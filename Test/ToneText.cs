using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LambdaMusic.Compile {
    class ToneText {
        public static List<int> ToList(string Text) {
            List<int> Tone = new List<int>();
            var a = Regex.Split(Text, @"([\r\n]+|,)");
            foreach (var NumberText in a) {
                var t = NumberText.Trim();
                if (!string.IsNullOrWhiteSpace(t) && int.TryParse(t, out int x)) {
                    Tone.Add(x);
                }
            }

            return Tone;
        }
    }
}
