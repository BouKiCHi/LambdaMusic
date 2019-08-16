using LambdaMusic.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LambdaMusic.Compile {
    /// <summary>
    /// ヘッダ設定
    /// </summary>

    class CompileHeader {
        List<Header> HeaderList = null;
        ErrorData Error = null;

        public CompileHeader(ErrorData Error) {
            this.Error = Error;
            MakeHeaderList();
        }

        private void MakeHeaderList() {
            HeaderList = (new List<Header>() {
                new Header { Name = "OCTREV", Length = 0, Execute = SetOctaveReverse },
                new Header { Name = "BASETICK", Length = 1, Execute = SetBaseTick },
                new Header { Name = "TITLE", Length = 1, Execute = SetTitle },
                new Header { Name = "DEVICE", Length = 2, Execute = SetDevice },
                new Header { Name = "TRACK", Length = 3, Execute = SetTrack },
            }).OrderBy(x => x.Name).ThenByDescending(x => x.Name.Length).ToList();
        }

        class Header {
            public string Name;
            public int Length;
            public ExecuteDelegate Execute;

            public delegate void ExecuteDelegate(SongData Song, List<string> Parameter);
        }


        public void Set(SongData Song, string Name, List<string> Parameter) {
            var o = HeaderList.FirstOrDefault(x => x.Name == Name);
            if (o == null) { Error.Add(ErrorData.Type.UnknownHeaderName); return; }
            if (!Requred(Parameter, o.Length)) return;

            o.Execute(Song, Parameter);
        }


        private void SetOctaveReverse(SongData Song, List<string> Parameter) {
            Song.OctaveReverse = true;
        }


        private void SetBaseTick(SongData Song, List<string> Parameter) {
            Song.SetMasterTick(ReadNumber(Parameter[0]));
        }

        private void SetTitle(SongData Song, List<string> Parameter) {
            if (!Requred(Parameter, 1)) return;
            Song.Title = Parameter[0];
        }

        private void SetDevice(SongData Song, List<string> Parameter) {
            int DeviceNo = ReadNumber(Parameter[0]);
            string DeviceName = Parameter[1];
            Song.AddDevice(DeviceNo, DeviceName);
        }

        private void SetTrack(SongData Song, List<string> Parameter) {
            string TrackName = Parameter[0];
            int DeviceNo = ReadNumber(Parameter[1]);
            int ChannelNo = ReadNumber(Parameter[2]);
            Song.AddTrack(TrackName, DeviceNo, ChannelNo);
        }

        private int ReadNumber(string Text) {
            int result = 0;
            if (int.TryParse(Text, out result)) return result;
            Error.Add(ErrorData.Type.InvalidNumber);
            return 0;
        }

        private bool Requred(List<string> Parameter, int RequiredLength) {
            if (Parameter.Count < RequiredLength) {
                Error.Add(ErrorData.Type.FewParameter);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// コマンド
    /// </summary>
    class CompileCommand {
        List<Command> ObjectList = null;
        ErrorData Error = null;

        Dictionary<string, int> NoteToNumber = new Dictionary<string, int> {
            {"c",0 },
            {"d",2 },
            {"e",4 },
            {"f",5 },
            {"g",7 },
            {"a",9 },
            {"b",11 },
        };

        public CompileCommand(ErrorData Error) {
            this.Error = Error;
            MakeList();
        }

        private void MakeList() {
            ObjectList = (new List<Command>() {
                new Command { Name = "@e", Execute = Effect },
                new Command { Name = "@", Execute = Tone },
                new Command { Name = "$", Execute = Macro },
                new Command { Name = "v", Execute = Volume },
                new Command { Name = "t", Execute = Tempo },
                new Command { Name = "l", Execute = Length },
                new Command { Name = "c", Execute = Note },
                new Command { Name = "d", Execute = Note },
                new Command { Name = "e", Execute = Note },
                new Command { Name = "f", Execute = Note },
                new Command { Name = "g", Execute = Note },
                new Command { Name = "a", Execute = Note },
                new Command { Name = "b", Execute = Note },
                new Command { Name = "r", Execute = Rest },
                new Command { Name = "o", Execute = Octave },
                new Command { Name = ">", Execute = RelativeOctave },
                new Command { Name = "<", Execute = RelativeOctave },
                new Command { Name = "&", Execute = Slar },
                new Command { Name = "^", Execute = Tie },
                new Command { Name = "L", Execute = Loop },
                new Command { Name = "[", Execute = RepeatStart },
                new Command { Name = "]", Execute = RepeatEnd },
                new Command { Name = "/", Execute = RepeatEscape },
                new Command { Name = "q", Execute = Staccato },
            }).OrderBy(x => x.Name).ThenByDescending(x => x.Name.Length).ToList();
        }

        class Command {
            public string Name;
            public ExecuteDelegate Execute;

            public delegate void ExecuteDelegate(SongData Song, TrackData Track, Command Command, MmlFileReader m);
        }


        private void Staccato(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.Staccato);
        }

        private void RepeatEscape(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.RepeatEscape);
        }

        private void RepeatEnd(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.RepeatEnd);
        }

        private void RepeatStart(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.RepeatStart);
        }

        private void Loop(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.Loop);
        }

        private void Tie(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.Tie);
        }

        private void RelativeOctave(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(Command.Name == ">" ? CommandType.OctaveGt : CommandType.OctaveLt);
        }

        private void Rest(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = Track.AddCommand(CommandType.Rest);
            cmd.SetValue(ReadLength(m));
        }

        private int ReadLength(MmlFileReader m) {
            return 0;
        }

        private void Octave(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadValue(Track, m, CommandType.Octave);
        }

        private void Slar(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.Slar);
        }

        private void Note(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var Num = NoteToNumber[Command.Name];
            var cmd = Track.AddCommand(CommandType.None);
           
        }

        private void Effect(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadName(Track, m, CommandType.Effect);
        }

        private void Tone(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadName(Track, m, CommandType.Tone);
        }

        private void Macro(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadName(Track, m, CommandType.Macro);
        }



        private void Volume(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadValue(Track, m, CommandType.Volume);
        }

        private void Tempo(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadValue(Track, m, CommandType.Tempo);
        }

        private void Length(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadValue(Track, m, CommandType.Length);
        }

        private void ReadName(TrackData Track, MmlFileReader m, CommandType Type) {
            var cmd = Track.AddCommand(Type);
            var t = m.ReadName();
            cmd.SetText(t);
        }

        private void ReadValue(TrackData Track, MmlFileReader m, CommandType Type) {
            var cmd = Track.AddCommand(Type);
            var t = m.ReadNumber();
            cmd.SetValue(ReadNumber(t));
        }



        public void Make(SongData Song, TrackData Track, MmlFileReader m) {
            var fl = m.FetchLine();
            var Command = ObjectList.FirstOrDefault(x => fl.StartsWith(x.Name));
            if (Command == null) { Error.Add(ErrorData.Type.UnknownCommandName); return; }
            m.StepCount(Command.Name.Length);

            Command.Execute(Song, Track, Command, m);
        }

        private int ReadNumber(string Text) {
            int result = 0;
            if (Text != null && int.TryParse(Text, out result)) return result;
            Error.Add(ErrorData.Type.InvalidNumber);
            return 0;
        }

    }


}
