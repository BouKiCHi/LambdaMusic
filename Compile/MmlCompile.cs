using LambdaMusic.Log;
using System;
using System.Collections.Generic;
using System.IO;

namespace LambdaMusic.Compile {
    class MmlCompile {
        ErrorData Error = new ErrorData();

        SongData Song = null;
        public bool Verbose = true;
        public bool Play = false;
        CompileHeader Header = null;
        CompileCommand Command = null;

        public MmlCompile() {
            Command = new CompileCommand(Error);
            Header = new CompileHeader(Error);
        }


        public void CompileFile(string path) {
            var BaseName = Path.GetFileNameWithoutExtension(path);
            var SongFileDirectory = Path.GetDirectoryName(path);
            string OutputFilename = Path.Combine(SongFileDirectory, BaseName + ".s98");
            Console.WriteLine($"Input Filename:{path}");

            ParseMmlFile(path);
            if (Error.HasError) {
                Error.ShowMessage();
                return;
            }

            // Song = SongTest.MakeTestSong();
            var Driver = new LogDriver();
            Driver.Make(OutputFilename, Song);

            ShowResult();

            if (Play) PlaySong(OutputFilename);
        }

        private void PlaySong(string outputFilename) {
            System.Diagnostics.Process.Start(outputFilename);
        }

        private void ShowResult() {
            var tbl = new TextTable();
            var row = tbl.NewRow();

            foreach (var t in new string[] { "Track", "DeviceNo", "ChannelNo", "Tick", "Loop" }) row.AddCell(t);
            tbl.UserWidth[0] = 15;
            tbl.UserWidth[3] = 10;
            tbl.UserWidth[4] = 10;
            foreach (var trk in Song.TrackWorkList) {
                row = tbl.NewRow();
                row.AddCell(trk.TrackName);
                row.AddCell(trk.DeviceNo.ToString());
                row.AddCell(trk.ChannelNo.ToString());
                row.AddCell(trk.Track.TotalTick.ToString());
                row.AddCell(trk.Track.LoopTick.ToString());
            }

            tbl.ShowTable();
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
            VerboseWriteLine($"--- Header: {Name} {Pos} ---");

            List<string> Parameter = new List<string>();

            bool SkipSeparator = false;

            while(!m.IsEof()) {
                var ct = SkipSpaceAndFetch(m);
                if (m.IsLineEnd(ct)) break; // 改行は終了
                if (SkipSeparator && m.IsSeparator(ct)) { ct = m.ReadNextType(); }
                if (m.IsSpace(ct)) { ct = m.ReadNextType(); } 
                if (m.IsLineEnd(ct)) break; // 改行は終了

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
                SkipSeparator = true;
                Parameter.Add(t);
            }

            Header.Set(Song, Name, Parameter);
        }

        // 空白スキップ＆フェッチ
        private static MmlCharactorType SkipSpaceAndFetch(MmlFileReader m) {
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

        /// <summary>
        /// トラックコマンド読み出し
        /// </summary>
        private void ReadTrackText(string Name, MmlFileReader m) {

            var ct = SkipSpaceAndFetch(m);
            var TrackData = Song.GetTrack(Name);

            bool Track = false;
            bool Block = false;

            // トラック
            if (ct == MmlCharactorType.GeneralChanacter) Track = true;
            if (ct == MmlCharactorType.BlockStart) { Block = true; Track = true; }

            if (Track) {
                if (Block) m.SkipType();
                ReadCommandUntilNextTrack(TrackData, m, Block);
                return;
            }

            Error.Add(ErrorData.Type.UnknownCharacterUsed);
        }

        // マクロ
        private void ReadMacro(MmlFileReader m) {
            // 位置を固定
            var Pos = m.GetPosition();
            m.StepNextCharacter();
            var Name = m.ReadName();
            VerboseWriteLine($"--- Macro: {Name} {Pos} ---");
            ReadTrackText(Name, m);
        }

        // 音色
        private void ReadTone(MmlFileReader m) {
            // 位置を固定
            var Pos = m.GetPosition();
            m.StepNextCharacter();
            var Name = m.ReadName();
            VerboseWriteLine($"--- Tone: {Name} {Pos} ---");

            if (!SkipUntilBlockStart(m)) return;
            var Parameter = ReadToneParameter(m);
            Song.SetToneParameter(Name, Parameter);
        }

        // エフェクト
        private void ReadEffect(MmlFileReader m) {
            // 位置を固定
            var Pos = m.GetPosition();
            m.StepNextCharacter();
            var Name = m.ReadName();
            VerboseWriteLine($"--- Effect: {Name} {Pos} ---");
            if (!SkipUntilBlockStart(m)) return;
            SkipUntilBlockEnd(m);
        }

        /// <summary>
        /// トラック処理
        /// </summary>

        private void ReadCommandUntilNextTrack(TrackData Track, MmlFileReader m, bool Block = false) {
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

                // 改行
                if (m.IsSpace(ct) || m.IsComment(ct)) {
                    m.SkipType();
                    continue;
                }

                // ブロック終了
                if (ct == MmlCharactorType.BlockEnd) {
                    if (!Block) { Error.Add(ErrorData.Type.UnknownCharacterUsed); return; }
                    m.SkipType();
                    m.StepNextLine();
                    return;
                }

                // トラックコマンド
                if (ct == MmlCharactorType.GeneralChanacter) {
                    Command.Make(Song, Track, m);
                    var lc = Track.GetLastCommand();
                    if (lc != null) VerboseWriteLine(lc.ToString());
                } else {
                    Error.Add(ErrorData.Type.UnknownCharacterUsed); return;
                }
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
            int result = 0;
            if (t == null || !int.TryParse(t, out result)) {
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
