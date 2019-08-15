namespace LambdaMusic.Compile {
    class ToneHelper {
        public static FMTone GetPianoTone() {
            // from VAL-SOUND Aco Piano2 (Attack)
            var PianoText = @"
4,5,
31, 5, 0, 0, 0,23, 1, 1, 3, 0
20,10, 3, 7, 8, 0, 1, 1, 3, 0
31, 3, 0, 0, 0,25, 1, 1, 7, 0
31,12, 3, 7,10, 2, 1, 1, 7, 0
";

            var PianoTone = new FMTone();
            PianoTone.SetToneFromText(ToneType.OPNA, PianoText);
            return PianoTone;
        }
    }
}
