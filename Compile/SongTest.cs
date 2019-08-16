using LambdaMusic.Log;

namespace LambdaMusic.Compile {
    class SongTest {

        public static SongData MakeTestSong() {
            var MasterTick = 128;
            var Song = new SongData();
            Song.SetMasterTick(MasterTick);

            Song.AddDevice(0, "OPNA");
            var Track = Song.GetTrack("A");

            FMTone PianoTone = ToneHelper.GetPianoTone();
            Track.AddToneCommand(PianoTone);

            int[] NoteNumber = { 0, 2, 4, 5, 7, 9, 11, 12 };
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

            foreach (var Note in NoteNumber) {
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
