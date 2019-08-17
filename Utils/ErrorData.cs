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
            CountNumberIsWrong,
            LastNoteNotFound,
            NoteLengthIsWrong,
        }

        Dictionary<Type, string> ErrorMessage = new Dictionary<Type, string>() {
            { Type.Unknown , "不明なエラー" },
            { Type.FileNotFound, "ファイルが見つかりません" },
            { Type.LineHeaderIsWrong , "行頭が異なります" },
            { Type.BlockStartNotFound , "ブロック開始記号が見つかりません" },
            { Type.BlockEndNotFound , "ブロック終了記号が見つかりません" },
            { Type.UnknownCharacterUsed , "不明な記号が使われています" },
            { Type.QuoteEndNotFound , "引用符終了記号が見つかりません" },

            { Type.UnknownHeaderName , "不明なヘッダ名です" },
            { Type.FewParameter , "パラメータが足りません" },
            { Type.DeviceNoIsOutOfRange , "デバイス番号が範囲外です" },
            { Type.DeviceNameIsNotSupported , "指定されたデバイスはサポートされていません" },
            { Type.InvalidNumber , "数値が無効です" },
            { Type.AlreadyBuiltTrack , "すでにトラックが構築されています" },
            { Type.TrackNameIsNotAssigned , "トラック名はアサインされていません" },

            { Type.ParameterIsWrong , "パラメータが正しくありません" },

            { Type.UnknownCommandName , "不明なコマンド名です" },
            { Type.CountNumberIsWrong , "指定されたカウントは正しくありません" },
            { Type.LastNoteNotFound , "一つ前の音符が見つかりません" },
            { Type.NoteLengthIsWrong , "音長の指定が正しくありません" },
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
