using System.Collections.Generic;
using System.Linq;

namespace LambdaMusic {
    class SoundDevice {
        public DeviceType Device = DeviceType.NONE;
        public long Clock = 0;
        public int Pan = 0;

        public enum DeviceType {
            NONE = 0,
            PSG = 1,
            OPN = 2,
            OPN2 = 3,
            OPNA = 4,
            OPM = 5,
            OPLL = 6,
            OPL = 7,
            OPL2 = 8,
            OPL3 = 9,
            PSG8910 = 15,
            DCSG = 16
        }

        Dictionary<DeviceType, string> DeviceTypeToName = new Dictionary<DeviceType, string>() {
            { DeviceType.OPNA, "OPNA" },
            { DeviceType.OPN, "OPN" },
            { DeviceType.OPLL, "OPLL" },
        };

        public string GetDeviceName() {
            return DeviceTypeToName.ContainsKey(Device) ? DeviceTypeToName[Device] : "NONE";
        }

        public ChannelInfoOpna ChannelInfo(int ChannelNo) {
            return new ChannelInfoOpna(ChannelNo);
        }

        public static DeviceType[] DeviceList = { DeviceType.OPNA };

        public static DeviceType DeviceNameToType(string Name) {
            var v = DeviceList.FirstOrDefault(x => x.ToString() == Name);
            return v;
        }

        public SoundDevice() {
            Device = DeviceType.NONE;
            Clock = 0;
        }
        public SoundDevice(DeviceType type) {
            SetDevice(type);
        }

        public SoundDevice(DeviceType type, int clock) {
            Device = type;
            Clock = clock;
        }

        public void SetDevice(DeviceType type) {
            Device = type;
            Clock = GetDefaultClock(type);
        }

        private int GetDefaultClock(DeviceType type) {
            var d = new Dictionary<DeviceType, int>() {
                { DeviceType.NONE, 0},
                { DeviceType.OPLL, 3579545 },
                { DeviceType.OPM,  4000000 },
                { DeviceType.OPNA, 7987200 },
                { DeviceType.OPL3, 14318180 }
            };

            if (!d.TryGetValue(type, out int DeviceDefaultClock)) return 0;
            return DeviceDefaultClock;
        }
    }

    class ChannelInfoOpna {
        private int ChannelNo;

        public ChannelInfoOpna(int channelNo) {
            ChannelNo = channelNo;
        }

        public int MaxVolume() {
            return 15;
        }
    }

}
