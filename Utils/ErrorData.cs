using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LambdaMusic.Compile;

namespace LambdaMusic {
    class ErrorData {
        List<string> Message = new List<string>();
        MmlFileReader Reader;

        public bool HasError { get { return Message.Count > 0; } }

        public void SetFileReader(MmlFileReader m) {
            Reader = m;
        }


        public enum Type {
            Unknown,
            FileNotFound,
            LineHeaderIsWrong,
            BlockStartNotFound,
            BlockEndNotFound,
            UnknownCharacterUsed,
            QuoteEndNotFound,
            UnknownHeaderName,
            FewParameter,
            DeviceNoIsOutOfRange,
            DeviceNameIsNotSupported,
            InvalidNumber,
            AlreadyBuiltTrack,
            TrackNameIsNotAssigned,
            ParameterIsWrong,
            UnknownCommandName,
        }

        Dictionary<Type, string> ErrorMessage = new Dictionary<Type, string>() {
            { Type.Unknown , "不明なエラー" },
            { Type.FileNotFound, "ファイルが見つかりません" },
            { Type.LineHeaderIsWrong , "行頭が異なります" },
            { Type.BlockStartNotFound , "ブロック開始記号が見つかりません" },
            { Type.BlockEndNotFound , "ブロック終了記号が見つかりません" },
            { Type.UnknownCharacterUsed , "不明な記号が使われています" },
            { Type.QuoteEndNotFound , "引用符終了記号が見つかりません" },
        };

        public void Add(Type ErrorType) {
            string Text = ErrorMessage.TryGetValue(ErrorType, out Text) ? Text : "不明なエラー : " + ErrorType.ToString();
            var s = $"{Reader.LineNoText()} : Error {Text}";
            Message.Add(s);
        }


        public void ShowMessage() {
            foreach (var t in Message) Console.WriteLine(t);
        }
    }


}
