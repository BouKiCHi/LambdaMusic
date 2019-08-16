using LambdaMusic.Log;
using System;
using System.Collections.Generic;
using System.IO;

namespace LambdaMusic.Compile {
    class Compile {
        ErrorData Error = new ErrorData();

        SongData Song = null;
        public bool Verbose = true;


        public void CompileFile(string filename) {
            var BaseName = Path.GetFileNameWithoutExtension(filename);
            string OutputFilename = BaseName + ".s98";

            ParseMmlFile(filename);
            if (Error.HasError) {
                Error.ShowMessage();
                return;
            }

            // Song = SongTest.MakeTestSong();
            var Driver = new LogDriver();
            Driver.Make(OutputFilename, Song);
        }

        private void ParseMmlFile(string filename) {
            Song = new SongData();
            Song.Error = Error;

            var m = new MmlFileReader();
            Error.SetFileReader(m);
            if (!m.Load(filename)) Error.Add(ErrorData.Type.FileNotFound);

            while (!m.IsEof() && !Error.HasError) {
                // 行頭
                var ct = m.FetchType();

                // コメントブロック終了時の行は読み飛ばす
                if (ct == MmlCharactorType.CommentEnd) {
                    m.SkipType();
                    m.StepNextLine();
                }

                if (m.IsComment(ct)) {
                    m.SkipType();
                    continue;
                }

                if (ct == MmlCharactorType.NextLine || ct == MmlCharactorType.Eof) {
                    m.SkipType();
                    continue;
                }

                if (ct != MmlCharactorType.GeneralChanacter) {
                    Error.Add(ErrorData.Type.LineHeaderIsWrong);
                    continue;
                }

                ReadItem(m);
            }

        }

        private void ReadItem(MmlFileReader m) {
            char ch = m.FetchCharacter();
            switch (ch) {
                case '#': ReadHeader(m); break;
                case '%': ReadEffect(m); break;
                case '@': ReadTone(m); break;
                case '$': ReadMacro(m); break;
                default: ReadTrack(m); break;
            }
        }

        private void VerboseWriteLine(string Text) {
            if (!Verbose) return;
            Console.WriteLine(Text);
        }

        // ヘッダ
        private void ReadHeader(MmlFileReader m) {
            var Pos = m.GetPosition();
            m.StepNextCharacter();
            var Name = m.ReadName();
            VerboseWriteLine($"{Pos} Header: {Name}");

            List<string> Parameter = new List<string>();

            while(!m.IsEof()) {
                var ct = ReadNextType(m);
                if (m.IsLineEnd(ct)) break;
                if (m.IsSeparator(ct)) { ct = m.ReadNextType(); }
                if (m.IsSpace(ct)) { ct = m.ReadNextType(); }
                if (m.IsLineEnd(ct)) break;

                if (ct != MmlCharactorType.GeneralChanacter) {
                    Error.Add(ErrorData.Type.UnknownCharacterUsed);
                    return;
                }

                string t;
                if (m.IsQuote()) {
                    t = m.ReadQuote();
                    if (t == null) {
                        Error.Add(ErrorData.Type.QuoteEndNotFound);
                        return;
                    }
                } else {
                    t = m.ReadText();
                }

                Parameter.Add(t);
            }

            Song.SetHeader(Name, Parameter);
        }

        // スペースを飛ばして次のタイプを読む
        private static MmlCharactorType ReadNextType(MmlFileReader m) {
            m.SkipIfSpace();
            return m.FetchType();
        }

        // トラック
        private void ReadTrack(MmlFileReader m) {
            List<string> TrackName = new List<string>();

            while (true) {
                var Name = m.ReadName();

                TrackName.Add(Name);

                m.SkipIfSpace();
                var ct = m.FetchType();
                if (ct == MmlCharactorType.Separator) {
                    m.SkipType();
                    continue;
                }
                break;
            }

            var TrackPos = m.GetPosition();
            for(var i =0; i < TrackName.Count; i++) {
                if (i > 0) m.SetPosition(TrackPos);
                var Name = TrackName[i];
                VerboseWriteLine($"--- Track: {Name} ---");
                ReadTrackText(Name, m);
            }
        }

        private void ReadTrackText(string Name, MmlFileReader m) {

            var ct = ReadNextType(m);
            var TrackData = Song.GetTrack(Name);

            bool Track = false;
            bool Block = false;

            // トラック
            if (ct == MmlCharactorType.GeneralChanacter) Track = true;
            if (ct == MmlCharactorType.BlockStart) { Block = true; Track = true; }

            if (Track) {
                if (Block) m.SkipType();
                ReadUntilNextTrack(TrackData, m, Block);
                return;
            }

            Error.Add(ErrorData.Type.UnknownCharacterUsed);
        }

        // マクロ
        private void ReadMacro(MmlFileReader m) {
            // 位置を固定
            var Pos = m.GetPosition();
            var Name = "$";
            m.StepNextCharacter();
            Name += m.ReadName();
            VerboseWriteLine($"--- Macro: {Name} {Pos} ---");
            ReadTrackText(Name, m);
        }

        // 音色
        private void ReadTone(MmlFileReader m) {
            // 位置を固定
            var Pos = m.GetPosition();
            var Name = "@";
            m.StepNextCharacter();
            Name += m.ReadName();
            VerboseWriteLine($"--- Tone: {Name} {Pos} ---");

            if (!SkipUntilBlockStart(m)) return;
            var Parameter = ReadToneParameter(m);
            Song.SetTone(Name, Parameter);
        }

        // エフェクト
        private void ReadEffect(MmlFileReader m) {
            // 位置を固定
            var Pos = m.GetPosition();
            m.StepNextCharacter();
            var Name = m.ReadName();
            VerboseWriteLine($"{Pos} Effect: {Name}");
            if (!SkipUntilBlockStart(m)) return;
            SkipUntilBlockEnd(m);
        }

        private void ReadUntilNextTrack(TrackData Track, MmlFileReader m, bool Block = false) {
            while (true) {
                var ct = m.FetchType();
                // EOF
                if (ct == MmlCharactorType.Eof) {
                    if (Block) Error.Add(ErrorData.Type.BlockEndNotFound);
                    return;
                }

                // 改行
                if (ct == MmlCharactorType.NextLine) {
                    m.SkipType();
                    if (Block) continue;
                    return;
                }

                // ブロック終了
                if (ct == MmlCharactorType.BlockEnd) {
                    if (!Block) { Error.Add(ErrorData.Type.UnknownCharacterUsed); return; }
                    m.SkipType();
                    m.StepNextLine();
                    return;
                }

                var Text = $"{m.GetPosition()} {ct}";
                if (ct == MmlCharactorType.GeneralChanacter) Text += $" [{m.FetchCharacter()}]";
                VerboseWriteLine(Text);
                m.SkipType();
            }
        }

        // 音色を読み出す
        private int[] ReadToneParameter(MmlFileReader m) {
            List<int> Parameter = new List<int>();
            bool SkipSeparator = false;
            while (true) {
                m.SkipSkipable();
                var ct = m.FetchType();
                // EOF
                if (ct == MmlCharactorType.Eof) {
                    Error.Add(ErrorData.Type.BlockEndNotFound);
                    return null;
                }

                // Separator
                if (ct == MmlCharactorType.Separator) {
                    VerboseWriteLine($"{m.GetPosition()} {ct}");
                    if (!SkipSeparator) { SkipSeparator = false; Parameter.Add(0); }
                    m.SkipType();
                    continue;
                }

                if (m.IsLineEnd(ct) || m.IsComment(ct)) { m.SkipType(); continue; }

                if (ct == MmlCharactorType.GeneralChanacter) {
                    SkipSeparator = true;
                    VerboseWriteLine($"{m.GetPosition()} {ct}");
                    Parameter.Add(ReadParameter(m));
                    continue;
                }


                if (ct == MmlCharactorType.BlockEnd) {
                    m.SkipType();
                    m.StepNextLine();
                    return Parameter.ToArray();
                }

                Error.Add(ErrorData.Type.UnknownCharacterUsed);
                return null;
            }
        }

        private int ReadParameter(MmlFileReader m) {
            var t = m.ReadNumber();
            int result;
            if (!int.TryParse(t, out result)) {
                Error.Add(ErrorData.Type.ParameterIsWrong);
            }
            return result;
        }


        // ブロック終了まで進める
        private void SkipUntilBlockEnd(MmlFileReader m) {
            while (true) {
                m.SkipSkipable();
                var ct = m.FetchType();
                if (ct == MmlCharactorType.GeneralChanacter || ct == MmlCharactorType.Separator) {
                    VerboseWriteLine($"{m.GetPosition()} {ct}");
                    m.SkipType();
                    continue;
                }

                if (ct == MmlCharactorType.Eof) {
                    Error.Add(ErrorData.Type.BlockEndNotFound);
                    return;
                }

                if (ct == MmlCharactorType.BlockEnd) {
                    m.SkipType();
                    m.StepNextLine();
                    return;
                }

                Error.Add(ErrorData.Type.UnknownCharacterUsed);
                return;
            }
        }

        // ブロック開始まで進める

        private bool SkipUntilBlockStart(MmlFileReader m) {
            m.SkipSkipable();

            var ct = m.FetchType();
            if (ct != MmlCharactorType.BlockStart) {
                Error.Add(ErrorData.Type.BlockStartNotFound);
                return false;
            }
            m.SkipType();
            return true;
        }
    }
}
