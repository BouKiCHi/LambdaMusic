using LambdaMusic.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LambdaMusic.Compile {
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

                // 音符
                new Command { Name = "c", Execute = Note },
                new Command { Name = "d", Execute = Note },
                new Command { Name = "e", Execute = Note },
                new Command { Name = "f", Execute = Note },
                new Command { Name = "g", Execute = Note },
                new Command { Name = "a", Execute = Note },
                new Command { Name = "b", Execute = Note },
                // 休符
                new Command { Name = "r", Execute = Rest },

                new Command { Name = "@e", Execute = Effect },
                new Command { Name = "@", Execute = Tone },
                new Command { Name = "$", Execute = Macro },
                new Command { Name = "v", Execute = Volume },
                new Command { Name = "t", Execute = Tempo },
                new Command { Name = "l", Execute = Length },
                new Command { Name = "o", Execute = Octave },
                new Command { Name = ">", Execute = RelativeOctave },
                new Command { Name = "<", Execute = RelativeOctave },
                new Command { Name = "&", Execute = Slur },
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

        public void Make(SongData Song, TrackData Track, MmlFileReader m) {
            var fl = m.FetchLine();
            var Command = ObjectList.FirstOrDefault(x => fl.StartsWith(x.Name));
            if (Command == null) { Error.Add(ErrorData.Type.UnknownCommandName); return; }
            m.StepCount(Command.Name.Length);

            Command.Execute(Song, Track, Command, m);
        }

        private void RepeatEscape(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.RepeatEscape);
        }

        private void RepeatEnd(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = Track.AddCommand(CommandType.RepeatEnd);
            cmd.Value = ReadNumber(m, Required:false);
        }

        private void RepeatStart(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.RepeatStart);
        }

        private void Loop(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(CommandType.Loop);
        }

        private void RelativeOctave(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            Track.AddCommand(Command.Name == ">" ? CommandType.OctaveGt : CommandType.OctaveLt);
        }

        private void Rest(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = Track.AddCommand(CommandType.Rest);
            Track.SetLastNote(cmd);
            var Tick = ReadNoteLength(Track.DefaultTick, Song.MasterTick, m);
            cmd.Tick = Tick;
        }

        private void Note(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var NoteNumber = NoteToNumber[Command.Name];
            var cmd = Track.AddCommand(CommandType.Note);
            Track.SetLastNote(cmd);
            var ct = m.FetchType();
            if (m.IsNextLine()) {
                cmd.Value = NoteNumber;
                cmd.Tick = Track.DefaultTick;
                return;
            }

            var ch = m.FetchCharacter();
            bool Shift = false;
            if (ch == '-') { Shift = true; NoteNumber++; }
            if (ch == '+') { Shift = true; NoteNumber++; }
            if (Shift) m.StepNextCharacter();

            cmd.Value = NoteNumber;
            var Tick = ReadNoteLength(Track.DefaultTick, Song.MasterTick, m);
            cmd.Tick = Tick;
        }

        private int ReadNoteLength(int DefaultTick, int MasterTick, MmlFileReader m) {

            int Tick = DefaultTick;
            if (m.IsNextLine()) return Tick;
            var ch = m.FetchCharacter();
            if (ch == '%') {
                m.StepNextCharacter();
                Tick = ReadNumber(m);
            } else {
                if (char.IsDigit(ch)) {
                    int Len = ReadNumber(m, Default: -1);
                    if (Len <= 0) {
                        Error.Add(ErrorData.Type.NoteLengthIsWrong);
                    } else  Tick = MasterTick / Len;
                }
                ch = m.FetchCharacter();
                int AddTick = Tick;
                while(ch == '.') {
                    AddTick /= 2;
                    Tick += AddTick;
                    m.StepNextCharacter();
                    ch = m.FetchCharacter();
                }
            }
            return Tick;
        }

        private void Octave(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadValue(Track, m, CommandType.Octave);
        }

        private void Slur(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = Track.AddCommand(CommandType.Slur);
            cmd.Value = 1;
        }

        private void Tie(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = Track.GetLastNote();
            if (cmd == null) { Error.Add(ErrorData.Type.LastNoteNotFound); return; }

            var Tick = ReadNoteLength(Track.DefaultTick, Song.MasterTick, m);
            cmd.Tick += Tick;
        }

        private void Staccato(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = Track.AddCommand(CommandType.Staccato);
            cmd.Value = ReadNumber(m);
        }


        private void Volume(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = ReadValue(Track, m, CommandType.Volume);
            if (cmd.Value < 0 || Track.ChannelInfo.MaxVolume() < cmd.Value) Error.Add(ErrorData.Type.VolumeIsWrong);
        }

        private void Tempo(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadValue(Track, m, CommandType.Tempo);
        }

        private void Length(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var Tick = ReadNoteLength(Track.DefaultTick, Song.MasterTick, m);
            Track.DefaultTick = Tick;
        }


        private void Effect(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadName(Track, m, CommandType.Effect);
        }

        private void Tone(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            var cmd = ReadName(Track, m, CommandType.Tone);
            var tp = Song.GetToneParameter(cmd.Text);
            if (tp == null) {
                Error.Add(ErrorData.Type.ToneNameIsNotFound);
                cmd.Text = null;
            }
        }

        private bool IsNumber(string text) {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+$");
        }

        private void Macro(SongData Song, TrackData Track, Command Command, MmlFileReader m) {
            ReadName(Track, m, CommandType.Macro);
        }


        private CommandData ReadName(TrackData Track, MmlFileReader m, CommandType Type) {
            var cmd = Track.AddCommand(Type);
            var t = m.ReadName();
            cmd.Text = t;
            return cmd;
        }

        private CommandData ReadValue(TrackData Track, MmlFileReader m, CommandType Type) {
            var cmd = Track.AddCommand(Type);
            cmd.Value = ReadNumber(m);
            return cmd;
        }

        private int ReadNumber(MmlFileReader m, bool Required = true, int Default = 0) {
            var t = m.ReadNumber();
            if (!Required) return Default;
            int Result;
            if (t != null && int.TryParse(t, out Result)) return Result;
            Error.Add(ErrorData.Type.InvalidNumber);
            return Default;
        }

    }


}
