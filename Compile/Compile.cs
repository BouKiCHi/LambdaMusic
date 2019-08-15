using LambdaMusic.Log;
using System;
using System.IO;

namespace LambdaMusic.Compile {
    class Compile {
        public void CompileFile(string filename) {
            var BaseName = Path.GetFileNameWithoutExtension(filename);
            string OutputFilename = BaseName + ".s98";

            MmlTest(filename);

            SongData Song = MakeSong();

            var Driver = new LogDriver();
            Driver.Make(OutputFilename, Song);
        }

        private static void MmlTest(string filename) {
            var m = new MmlFileReader();
            m.Load(filename);

            while(!m.IsEof()) {
                var ct = m.GetCharacterType();
                var Text = $"LineNo:{m.LineNo} Column:{m.Column} Type:{ct.ToString()}";
                if (ct == MmlCharactorType.Generic) Text += $" {m.FetchCharacter()}";
                Console.WriteLine(Text);

                m.SkipType();
            }

        }

        private static SongData MakeSong() {
            var MasterTick = 128;
            var Song = new SongData();
            Song.SetMasterTick(MasterTick);

            var DeviceNo = Song.AddDevice(SoundDevice.DeviceType.OPNA);
            var Track = Song.AddTrack();
            Track.SetOutput(DeviceNo, 0);

            FMTone PianoTone = ToneHelper.GetPianoTone();
            Track.AddToneCommand(PianoTone);

            int[] NoteData2 = { 0, 2, 4, 5, 7, 9, 11, 12 };
            CommandData cmd;

            cmd = Track.AddCommand(CommandType.Tempo);
            cmd.SetTempo(120);

            cmd = Track.AddCommand(CommandType.Octave);
            cmd.SetOctave(4);

            for (var vol = 15; vol >= 0; vol--) {
                cmd = Track.AddCommand(CommandType.Volume);
                cmd.SetVolume(vol);

                cmd = Track.AddCommand(CommandType.Note);
                cmd.SetNote(0);
                cmd.SetTick(MasterTick / 4);
            }

            cmd = Track.AddCommand(CommandType.Volume);
            cmd.SetVolume(15);

            cmd = Track.AddCommand(CommandType.RepeatStart);

            foreach (var Note in NoteData2) {
                cmd = Track.AddCommand(CommandType.Note);
                cmd.SetNote(Note);
                cmd.SetTick(MasterTick / 4);
            }

            cmd = Track.AddCommand(CommandType.RepeatEnd);
            cmd.SetValue(2);


            cmd = Track.AddCommand(CommandType.Rest);
            cmd.SetTick(MasterTick / 4);
            return Song;
        }
    }
}
