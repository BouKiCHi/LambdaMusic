using System;
using System.Collections.Generic;
using System.Linq;

namespace LambdaMusic.Log {
    class SongData {
        public int Tempo = 120;
        public int MasterTick = 128;
        public double SecondsPerTick = 0.015625;
        public bool OctaveReverse = false;
        public string Title = "Unknown";
        public bool IsBuiltTrack = false;

        public ErrorData Error;

        public List<SoundDevice> DeviceList = new List<SoundDevice>();
        public List<TrackInfo> TrackWorkList = new List<TrackInfo>();

        public Dictionary<string, int[]> ToneData = new Dictionary<string, int[]>();
        Dictionary<string, FMTone> NameToTone = new Dictionary<string, FMTone>();

        Dictionary<string, TrackData> TrackKeyToData = null;


        public SongData() {
            SetTimingValue();
        }

        public void SetToneParameter(string name, int[] parameter) {
            ToneData[name] = parameter;
        }

        public int[] GetToneParameter(string name) {
            return ToneData.ContainsKey(name) ? ToneData[name] : null;
        }


        public FMTone GetTone(string DeviceTypeName, string Name) {
            if (NameToTone.ContainsKey(Name)) return NameToTone[Name];
            var tone = new FMTone();
            NameToTone[Name] = tone;
            var Parameter = GetToneParameter(Name);
            var ToneType = tone.GetToneType(DeviceTypeName);
            if (Parameter == null) { return null; }
            tone.SetToneParameter(ToneType, Parameter);
            return tone;
        }


        /// <summary>
        /// デバイスの追加
        /// </summary>
        public void AddDevice(int deviceNo, string deviceName) {
            if (IsBuiltTrack) { Error.Add(ErrorData.Type.AlreadyBuiltTrack); return; }
            if (deviceNo < 0) { Error.Add(ErrorData.Type.DeviceNoIsOutOfRange); return;  }
            if (DeviceList.Count <= deviceNo) {
                for (var i = DeviceList.Count; i < deviceNo; i++) DeviceList.Add(new SoundDevice());
            }

            var DeviceType = SoundDevice.DeviceNameToType(deviceName);
            if (DeviceType == SoundDevice.DeviceType.NONE) { Error.Add(ErrorData.Type.DeviceNameIsNotSupported); return; }
            DeviceList[deviceNo].Device = DeviceType;
        }


        /// <summary>
        /// トラックの追加
        /// </summary>
        public void AddTrack(string trackName, int deviceNo, int channelNo) {
            if (IsBuiltTrack) { Error.Add(ErrorData.Type.AlreadyBuiltTrack); return; }
            TrackWorkList.Add(MakeTrackInfo(trackName, deviceNo, channelNo));
        }


        /// <summary>
        /// トラック取得時にトラック名とデバイスの設定が行われる
        /// </summary>
        public TrackData GetTrack(string Key) {
            if (!IsBuiltTrack) BuildTrack();
            if (!TrackKeyToData.ContainsKey(Key)) {
                Error.Add(ErrorData.Type.TrackNameIsNotAssigned);
                return null;
            }
            return TrackKeyToData[Key];
        }

        /// <summary>
        /// トラック構築
        /// </summary>
        private void BuildTrack() {
            if (IsBuiltTrack) return;
            IsBuiltTrack = true;

            AddDeviceIfNone();
            AddTrackIfNone();
            AddTrackDataToWork();
            AssignTrackName();
        }


        // デバイス追加
        private void AddDeviceIfNone() {
            if (DeviceList.Count != 0) return;

            DeviceList.Add(new SoundDevice(SoundDevice.DeviceType.OPNA));
        }


        private void AddTrackIfNone() {
            if (TrackWorkList.Count != 0) return;

            TrackWorkList.AddRange(new List<TrackInfo>() {
                MakeTrackInfo("A",0,0),
                MakeTrackInfo("B",0,1),
                MakeTrackInfo("C",0,2),
                MakeTrackInfo("D",0,6),
                MakeTrackInfo("E",0,7),
                MakeTrackInfo("F",0,8),
                MakeTrackInfo("G",0,8),
                MakeTrackInfo("H",0,3),
                MakeTrackInfo("I",0,4),
                MakeTrackInfo("J",0,5),
                MakeTrackInfo("K",0,10),
            });
        }

        private TrackInfo MakeTrackInfo(string Name, int DeviceNo, int ChannelNo) {
            return new TrackInfo() {
                TrackName = Name,
                DeviceNo = DeviceNo,
                ChannelNo = ChannelNo,
            };
        }

        private void AddTrackDataToWork() {
            foreach (var t in TrackWorkList) {
                var Track = new TrackData(MasterTick);
                Track.SetOutput(t.DeviceNo, t.ChannelNo);
                t.Track = Track;
                var dev = DeviceList[t.DeviceNo];
                Track.DeviceTypeName = dev.GetDeviceName();
                Track.ChannelInfo = dev.ChannelInfo(t.ChannelNo);
                Track.TrackName = t.TrackName;
            }
        }

        private void AssignTrackName() {
            TrackKeyToData = TrackWorkList.ToDictionary(x => x.TrackName, x => x.Track);
        }

        public void SetTimingValue() {
            SecondsPerTick = GetSecondsPerTick();
        }

        public double GetSecondsPerTick() {
            if (Tempo == 0 || MasterTick == 0) return 0.01;
            return ((double)60 * 4 / Tempo) / MasterTick;
        }

        public void SetTempo(int v) {
            Tempo = v;
            SetTimingValue();
        }

        public void SetMasterTick(int v) {
            MasterTick = v;
            SetTimingValue();
        }



        public class TrackInfo {
            public string TrackName;
            public TrackData Track;
            public int DeviceNo;
            public int ChannelNo;
        }
    }

    class TrackData {
        public bool TrackEnd = false;
        public string DeviceTypeName = null;
        public string TrackName = null;
        public int DeviceNo = 0;
        public int ChannelNo = 0;

        public int NoteTick = 0;
        public int StaccatoTick = 0;
        public int EffectTick = 0;

        public int TotalTick = 0;
        public int LoopTick = 0;

        public int Octave = 4;
        public int DefaultTick = 0;
        public int Staccato = 0;

        public int CommandIndex = 0;

        public bool EffectOn = false;
        public bool StaccatoOn = false;

        public ChannelInfoOpna ChannelInfo;


        /// <summary>
        /// 最小のティックを得る
        /// </summary>
        public int GetTick() {
            var Tick = NoteTick;
            if (StaccatoOn && StaccatoTick < Tick) Tick = StaccatoTick;
            if (EffectOn && EffectTick < Tick) Tick = EffectTick;
            return Tick;
        }

        public List<CommandData> CommandList = new List<CommandData>();
        public Stack<RepeatData> RepeatStack = new Stack<RepeatData>();

        CommandData LastNoteCommand = null;

        public bool Slur = false;
        public bool KeyOn = false;

        public TrackData(int MasterTick) {
            DefaultTick = MasterTick / 4;
        }

        public int LastCommandIndex = 0;


        public CommandData GetLastCommand() {
            if (CommandList.Count == LastCommandIndex) return null;
            LastCommandIndex = CommandList.Count;
            return CommandList[CommandList.Count - 1];
        }

        public CommandData GetNextCommand() {
            if (IsEnd()) return null;
            return CommandList[CommandIndex++];
        }


        public CommandData AddCommand(CommandType Type) {
            var Command = new CommandData(Type);
            CommandList.Add(Command);
            return Command;
        }

        public void AddWaitTrackTick(int Tick) {
            TotalTick += Tick;
            NoteTick += Tick;
        }

        public void SetLoopTick() {
            LoopTick = TotalTick;
        }

        public void SubTick(int Tick) {
            NoteTick -= Tick;
            if (StaccatoOn) StaccatoTick -= Tick;
            if (EffectOn) EffectTick -= Tick;
        }

        public void AddStaccato(int Tick) {
            if (Staccato == 0) return;
            StaccatoOn = true;
            StaccatoTick += (Tick * Staccato) / 8;
        }

        public CommandData AddCommand() {
            var Command = new CommandData(CommandType.NoOperation);
            CommandList.Add(Command);
            return Command;
        }

        public void SetOutput(int deviceNo, int channelNo) {
            DeviceNo = deviceNo;
            ChannelNo = channelNo;
        }

        public CommandData GetLastNote() {
            return LastNoteCommand;
        }

        public void SetLastNote(CommandData cmd) {
            LastNoteCommand = cmd;
        }

        public void RepeatStart(int Value) {
            RepeatStack.Push(new RepeatData() { StartIndex = CommandIndex });
        }

        public void RepeatEscape() {
            var o = RepeatStack.Peek();
            if (o.EndIndex == 0) return;
            if (o.Count <= 1) {
                CommandIndex = o.EndIndex;
                RepeatStack.Pop();
                return;
            }
        }

        public void RepeatEnd(int Value) {
            var o = RepeatStack.Peek();
            if (o.EndIndex == 0) {
                o.EndIndex = CommandIndex;
                CommandIndex = o.StartIndex;
                if (Value == 0) Value = 2;
                o.Count = Value - 1;
                return;
            }
            if (o.Count <= 1) {
                RepeatStack.Pop();
                return;
            }
            o.Count--;
        }

        public bool IsEnd() {
            return CommandIndex < 0 || CommandList.Count <= CommandIndex;
        }

        public override string ToString() {
            return $"Device: {DeviceTypeName} Track:{TrackName}";
        }
    }

    class RepeatData {
        public int StartIndex = 0;
        public int EndIndex = 0;
        public int Count = 0;
    }

    class CommandData {
        public int LineNo = 0;
        public int ColumnNo = 0;
        public CommandType Type;
        public int Value = 0;
        public int[] Param = null;
        public int Tick = 0;
        public string Text = null;
        public bool Slur = false;

        public CommandData(CommandType type) {
            Type = type;
        }

        public bool IsNote { get { return Type == CommandType.Note || Type == CommandType.DirectNote; } }

        public override string ToString() {
            return $"{Type} Value:{Value} Tick:{Tick}";
        }
    }

    enum CommandType {
        NoOperation,
        DirectNote,
        Rest,
        Tone,
        Octave,
        Note,
        Tempo,
        Volume,
        RepeatStart,
        RepeatEnd,
        Loop,
        Length,
        Macro,
        Effect,
        Tie,

        // オクターブダウン [<]
        OctaveLt,

        // オクターブアップ [>]
        OctaveGt,

        RepeatEscape,
        Staccato,
        Slur,
    }

}