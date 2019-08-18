using System;
using System.Linq;

namespace LambdaMusic.Log {
    class LogDriver {

        bool Verbose = true;

        public void Make(string songFilename, SongData sd) {
            LogS98 Log = new LogS98();
            Log.AddDeviceRange(sd.DeviceList);
            Log.SetDeviceMap();

            sd.SetTimingValue();

            AdvanceSong(Log, sd);
            Log.WriteEnd();
            Log.WriteToFile(songFilename);
            Log.Close();
        }

        // 曲を進める
        private void AdvanceSong(LogS98 Log, SongData sd) {
            while(true) {
                int Tick = 0;
                int WorkChannel = 1;
                while (Tick == 0 && WorkChannel > 0) {
                    WorkChannel = 0;
                    foreach (var td in sd.TrackWorkList.Select(x => x.Track).Where(x => !x.IsEnd())) {
                        WorkChannel++;
                        var TrackTick = AdvanceTrack(Log, sd, td);
                        Tick = GetMinimunTick(Tick, TrackTick);
                    }
                }
                AddSync(Log, sd, Tick);
                if (Tick == 0) break;
            }
        }

        // 最小イベント
        private static int GetMinimunTick(int Tick, int TrackTick) {
            if (0 < TrackTick) {
                // 一番小さいイベント
                if (Tick == 0 || TrackTick < Tick) Tick = TrackTick;
            }

            return Tick;
        }

        // 同期
        private void AddSync(LogS98 Log, SongData sd, int Tick) {
            if (Tick == 0) return;
            Log.Wait(sd.SecondsPerTick * Tick);
            foreach (var t in sd.TrackWorkList.Where(x => !x.Track.IsEnd())) t.Track.SubTick(Tick);
        }

        // トラックを進める

        private int AdvanceTrack(LogS98 Log, SongData sd, TrackData td) {
            var Tick = td.GetTick();
            if (Tick > 0) return Tick;

            var lc = Log.GetDevice(td.DeviceNo).GetChannel(td.ChannelNo);

            if (td.StaccatoOn &&  td.StaccatoTick == 0) {
                td.StaccatoOn = false;
                NoteOff(lc);
            }            

            while (td.NoteTick == 0) {
                if (td.IsEnd()) break;
                var cmd = td.GetNextCommand();
                if (cmd != null) AdvanceCommand(Log, sd, td, cmd, lc);
                else { NoteOff(lc); }
            }
            return td.GetTick();
        }

        private void AdvanceCommand(LogS98 Log, SongData sd, TrackData td, CommandData cmd, LogChannel lc) {

            td.AddWaitTrackTick(cmd.Tick);

            if (cmd.IsNote) td.AddStaccato(cmd.Tick);
            
            switch (cmd.Type) {
                case CommandType.Slur:
                    td.Slur = cmd.Value != 0;
                    break;
                case CommandType.Octave:
                    td.Octave = cmd.Value;
                break;
                case CommandType.OctaveGt:
                    if (!sd.OctaveReverse) td.Octave++; else td.Octave--;
                break;
                case CommandType.OctaveLt:
                    if (!sd.OctaveReverse) td.Octave--; else td.Octave++;
                    break;
                case CommandType.Tempo:
                    sd.SetTempo(cmd.Value);
                    break;
                case CommandType.Tone:
                    SetTone(sd, td , cmd, lc);
                    break;
                case CommandType.Note:
                    NoteOn(td, lc, cmd.Value + (12 * td.Octave));
                    break;
                case CommandType.DirectNote:
                    NoteOn(td, lc, cmd.Value);
                    break;
                case CommandType.Rest:
                    NoteOff(lc);
                    break;
                case CommandType.Volume:
                    lc.SetVolume(cmd.Value);
                    break;
                case CommandType.RepeatStart:
                    td.RepeatStart(cmd.Value);
                    break;
                case CommandType.RepeatEnd:
                    td.RepeatEnd(cmd.Value);
                    break;
                case CommandType.Loop:
                    td.SetLoopTick();
                    Log.SetLoopPoint();
                    break;
                case CommandType.Staccato:
                    td.Staccato = (cmd.Value >= 8 ? 0 : cmd.Value);
                    break;
                default:
                    VerboseWriteLine($"未実装: {cmd}");
                    break;
            }

        }

        private void VerboseWriteLine(string v) {
            if (Verbose) Console.WriteLine(v);
        }

        private void SetTone(SongData sd, TrackData td, CommandData cmd, LogChannel lc) {
            if (cmd.Text == null) return;
            var tone = sd.GetTone(td.DeviceTypeName, cmd.Text);
            lc.SetTone(tone);
        }

        private void NoteOn(TrackData td, LogChannel lc, int NoteNumber) {
            lc.SetNoteNumber(NoteNumber);
            if (!td.Slur) {
                if (lc.IsKeyOn()) lc.KeyOff();
                lc.KeyOn();
            }
            td.Slur = false;
        }

        private void NoteOff(LogChannel lc) {
            if (lc.IsKeyOn()) lc.KeyOff();
        }

    }
}
