using LambdaMusic.Log;

namespace LambdaMusic.Compile {
    class SongTest {

        public static SongData MakeTestSong() {
            var MasterTick = 128;
            var Song = new SongData();
            Song.SetMasterTick(MasterTick);

            Song.AddDevice(0, "OPNA");
            var Track = Song.GetTrack("A");

            //FMTone PianoTone = ToneHelper.GetPianoTone();
            //Track.AddToneCommand(PianoTone);

            int[] NoteNumber = { 0, 2, 4, 5, 7, 9, 11, 12 };
            CommandData cmd;

            cmd = Track.AddCommand(CommandType.Tempo);
            cmd.Value = 120;

            cmd = Track.AddCommand(CommandType.Octave);
            cmd.Value = 4;

            for (var vol = 15; vol >= 0; vol--) {
                cmd = Track.AddCommand(CommandType.Volume);
                cmd.Value = vol;

                cmd = Track.AddCommand(CommandType.Note);
                cmd.Value = 0;
                cmd.Tick = MasterTick / 4;
            }

            cmd = Track.AddCommand(CommandType.Volume);
            cmd.Value = 15;

            cmd = Track.AddCommand(CommandType.RepeatStart);

            foreach (var Note in NoteNumber) {
                cmd = Track.AddCommand(CommandType.Note);
                cmd.Value = Note;
                cmd.Tick = MasterTick / 4;
            }

            cmd = Track.AddCommand(CommandType.RepeatEnd);
            cmd.Value = 2;

            cmd = Track.AddCommand(CommandType.Rest);
            cmd.Tick = MasterTick / 4;
            return Song;
        }
    }
}
