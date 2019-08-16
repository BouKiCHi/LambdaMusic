using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LambdaMusic.Compile {
    class MmlFileReader {
        Position CurrentPosition = new Position();
        string[] TextData;

        public bool Load(string MmlFileName) {
            if (!File.Exists(MmlFileName)) return false;
            TextData = File.ReadAllLines(MmlFileName);
            return true;
        }

        public MmlCharactorType FetchType() {
            if (IsEof()) return MmlCharactorType.Eof;
            if (IsNextLine()) return MmlCharactorType.NextLine;

            var ch = FetchCharacter();
            if (ch == ',') return MmlCharactorType.Separator;
            if (ch == '{') return MmlCharactorType.BlockStart;
            if (ch == '}') return MmlCharactorType.BlockEnd;
            if (ch == ' ' || ch == '\t') return MmlCharactorType.Space;

            var line = FetchLine();
            if (line.StartsWith("//")) return MmlCharactorType.CommentLine;
            if (line.StartsWith("/*")) return MmlCharactorType.CommentStart;
            if (line.StartsWith("*/")) return MmlCharactorType.CommentEnd;

            return MmlCharactorType.GeneralChanacter;
        }

        public void StepNextLine() {
            CurrentPosition.LineNo++;
            CurrentPosition.ColumnNo = 0;
        }

        public void StepNextCharacter() {
            CurrentPosition.ColumnNo++;
        }

        public void StepCount(int Skip) {
            for (var i = 0; i < Skip; i++) StepNextCharacter();
        }

        public void SkipType() {
            var nt = FetchType();
            switch (nt) {
                case MmlCharactorType.Eof:
                    break;
                case MmlCharactorType.NextLine:
                    StepNextLine();
                    break;
                case MmlCharactorType.BlockStart:
                case MmlCharactorType.BlockEnd:
                case MmlCharactorType.Separator:
                    StepNextCharacter();
                    break;
                case MmlCharactorType.Space:
                    SkipSpace();
                    break;
                case MmlCharactorType.CommentLine:
                    StepNextLine();
                    break;
                case MmlCharactorType.CommentStart:
                    SkipCommentBlock();
                    break;
                case MmlCharactorType.CommentEnd:
                    StepCount(2);
                    break;
                case MmlCharactorType.GeneralChanacter:
                    StepNextCharacter();
                    break;
            }
        }

        public MmlCharactorType ReadNextType() {
            SkipType();
            return FetchType();
        }

        public Position GetPosition() {
            return new Position(CurrentPosition);
        }

        public string LineNoText() {
            return CurrentPosition.ToString();
        }

        public bool IsSeparator(MmlCharactorType ct) {
            return ct == MmlCharactorType.Separator;
        }

        public bool IsSpace(MmlCharactorType ct) {
            return ct == MmlCharactorType.Space;
        }


        public bool IsComment(MmlCharactorType ct) {
            return (
                ct == MmlCharactorType.CommentStart ||
                ct == MmlCharactorType.CommentEnd ||
                ct == MmlCharactorType.CommentLine
            );
        }

        public bool IsLineEnd(MmlCharactorType ct) {
            return (
                ct == MmlCharactorType.NextLine ||
                ct == MmlCharactorType.Eof
            );
        }

        public bool IsNextSkipable() {
            var ct = FetchType();
            return (
                ct == MmlCharactorType.NextLine || 
                IsSpace(ct) || IsComment(ct)
            );
        }

        public string ReadName() {
            var fl = FetchLine();
            var m = Regex.Match(fl, @"[a-zA-Z0-9_]+");
            StepCount(m.Length);
            return m.Value;
        }

        public void SetPosition(Position o) {
            CurrentPosition.SetPosition(o);
        }

        public bool IsQuote() {
            var ch = FetchCharacter();
            return ch == '\"';
        }

        public string ReadQuote() {
            var fl = FetchLine();
            var m = Regex.Match(fl, @"""(\\""|[^""])+""");
            if (!m.Success) return null;
            StepCount(m.Length);
            return TrimedQuoteText(m.Value);
        }

        public string TrimedQuoteText(string Text) {
            return Text.Substring(1, Text.Length - 2);
        }

        private void SkipCommentBlock() {
            StepCount(2);
            while(!IsEof()) {
                var fl = FetchLine();
                var Index = fl.IndexOf("*/");
                if (Index < 0) { StepNextLine(); continue; }
                StepCount(Index);
                break;
            }
        }

        public void SkipSpace() {
            var fl = FetchLine();

            var m = Regex.Match(fl,@"\s+");
            StepCount(m.Length);
        }

        public void SkipIfSpace() {
            var ct = FetchType();
            if (ct == MmlCharactorType.Space) SkipSpace();
        }

        public void SkipSkipable() {
            while (IsNextSkipable()) SkipType();
        }

        public string ReadText() {
            var fl = FetchLine();

            var m = Regex.Match(fl, @"\S+");
            StepCount(m.Length);
            return m.Value;
        }

        public string ReadNumber() {
            var fl = FetchLine();

            var m = Regex.Match(fl, @"\d+");
            StepCount(m.Length);
            return m.Value;
        }


        public bool IsEof() {
            return (TextData.Length <= CurrentPosition.LineNo);
        }

        public bool IsNextLine() {
            return (TextData[CurrentPosition.LineNo].Length <= CurrentPosition.ColumnNo);
        }

        public char FetchCharacter() {
            return TextData[CurrentPosition.LineNo][CurrentPosition.ColumnNo];
        }

        public string FetchLine() {
            return TextData[CurrentPosition.LineNo].Substring(CurrentPosition.ColumnNo);
        }


    }
    class Position {
        public int LineNo = 0;
        public int ColumnNo = 0;

        public Position() { }

        public Position(Position o) {
            SetPosition(o);
        }

        public override string ToString() {
            return $"Line {LineNo + 1}:{ColumnNo + 1}";
        }

        public void SetPosition(Position o) {
            LineNo = o.LineNo;
            ColumnNo = o.ColumnNo;
        }
    }
}
