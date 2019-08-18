using LambdaMusic.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LambdaMusic.Compile {
    /// <summary>
    /// ヘッダ設定
    /// </summary>

    class CompileHeader {
        List<Header> HeaderList = null;
        ErrorData Error = null;

        public CompileHeader(ErrorData Error) {
            this.Error = Error;
            MakeHeaderList();
        }

        private void MakeHeaderList() {
            HeaderList = (new List<Header>() {
                new Header { Name = "OCTREV",   Length = 0, Execute = SetOctaveReverse },
                new Header { Name = "BASETICK", Length = 1, Execute = SetBaseTick },
                new Header { Name = "TITLE",    Length = 1, Execute = SetTitle },
                new Header { Name = "DEVICE",   Length = 2, Execute = SetDevice },
                new Header { Name = "TRACK",    Length = 3, Execute = SetTrack },
            }).OrderBy(x => x.Name).ThenByDescending(x => x.Name.Length).ToList();
        }

        class Header {
            public string Name;
            public int Length;
            public ExecuteDelegate Execute;

            public delegate void ExecuteDelegate(SongData Song, List<string> Parameter);
        }


        public void Set(SongData Song, string Name, List<string> Parameter) {
            var o = HeaderList.FirstOrDefault(x => x.Name == Name);
            if (o == null) { Error.Add(ErrorData.Type.UnknownHeaderName); return; }
            if (!Requred(Parameter, o.Length)) return;

            o.Execute(Song, Parameter);
        }


        private void SetOctaveReverse(SongData Song, List<string> Parameter) {
            Song.OctaveReverse = true;
        }


        private void SetBaseTick(SongData Song, List<string> Parameter) {
            Song.SetMasterTick(ParseNumber(Parameter[0]));
        }

        private void SetTitle(SongData Song, List<string> Parameter) {
            if (!Requred(Parameter, 1)) return;
            Song.Title = Parameter[0];
        }

        private void SetDevice(SongData Song, List<string> Parameter) {
            int DeviceNo = ParseNumber(Parameter[0]);
            string DeviceName = Parameter[1];
            Song.AddDevice(DeviceNo, DeviceName);
        }

        private void SetTrack(SongData Song, List<string> Parameter) {
            string TrackName = Parameter[0];
            int DeviceNo = ParseNumber(Parameter[1]);
            int ChannelNo = ParseNumber(Parameter[2]);
            Song.AddTrack(TrackName, DeviceNo, ChannelNo);
        }

        private int ParseNumber(string Text) {
            int result = 0;
            if (int.TryParse(Text, out result)) return result;
            Error.Add(ErrorData.Type.InvalidNumber);
            return 0;
        }

        private bool Requred(List<string> Parameter, int RequiredLength) {
            if (Parameter.Count < RequiredLength) {
                Error.Add(ErrorData.Type.FewParameter);
                return false;
            }
            return true;
        }
    }

}
