using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LambdaMusic.Compile {
    class MmlFileReader {
        Position CurrentPosition;
        string[] TextData;

        public int LineNo { get { return CurrentPosition.LineNo; } }
        public int Column { get { return CurrentPosition.Column; } }

        public void Load(string MmlFileName) {
            TextData = File.ReadAllLines(MmlFileName);
            CurrentPosition = new Position();
        }

        public MmlCharactorType GetCharacterType() {
            if (IsEof()) return MmlCharactorType.Eof;
            if (IsNextLine()) return MmlCharactorType.NextLine;

            var ch = FetchCharacter();
            if (ch == ',') return MmlCharactorType.Seperator;
            if (ch == '{') return MmlCharactorType.BlockStart;
            if (ch == '}') return MmlCharactorType.BlockEnd;
            if (ch == ' ' || ch == '\t') return MmlCharactorType.Space;

            var line = FetchLine();
            if (line.StartsWith("//")) return MmlCharactorType.CommentLine;
            if (line.StartsWith("/*")) return MmlCharactorType.CommentStart;
            if (line.StartsWith("*/")) return MmlCharactorType.CommentEnd;

            return MmlCharactorType.Generic;
        }

        public void StepNextLine() {
            CurrentPosition.LineNo++;
            CurrentPosition.Column = 0;
        }

        public void StepNextCharacter() {
            CurrentPosition.Column++;
        }

        public void StepCount(int Skip) {
            for (var i = 0; i < Skip; i++) StepNextCharacter();
        }

        public void SkipType() {
            var nt = GetCharacterType();
            switch (nt) {
                case MmlCharactorType.Eof:
                    break;
                case MmlCharactorType.NextLine:
                    StepNextLine();
                    break;
                case MmlCharactorType.BlockStart:
                case MmlCharactorType.BlockEnd:
                case MmlCharactorType.Seperator:
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
                case MmlCharactorType.Generic:
                    StepNextCharacter();
                    break;
            }

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


        private void SkipSpace() {
            var fl = FetchLine();

            var m = Regex.Match(fl,@"\s+");
            StepCount(m.Length);
        }

        public bool IsEof() {
            return (TextData.Length <= CurrentPosition.LineNo);
        }

        public bool IsNextLine() {
            return (TextData[CurrentPosition.LineNo].Length <= CurrentPosition.Column);
        }

        public char FetchCharacter() {
            return TextData[CurrentPosition.LineNo][CurrentPosition.Column];
        }

        public string FetchLine() {
            return TextData[CurrentPosition.LineNo].Substring(CurrentPosition.Column);
        }

    }

    enum MmlCharactorType {
        Space,
        Seperator,
        NextLine,
        Eof,
        CommentLine,
        CommentStart,
        CommentEnd,
        Generic,
        BlockStart,
        BlockEnd
    }
    class Position {
        public bool Eof = false;
        public int LineNo = 0;
        public int Column = 0;
    }
}
