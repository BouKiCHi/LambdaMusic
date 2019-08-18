using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LambdaMusic {
    class LogS98 : IDisposable, SoundLog {
        double BaseTick;
        double SyncBufferTicks = 0;
        int LoopPoint;

        // 分子 / 分母
        int Numerator = 0;
        int Denominator = 0;

        bool Loop = false;
        bool Tag = false;

        List<SoundDevice> DeviceList;
        BinaryWriter LogData;

        LogDeviceOpna[] LogDeviceData;

        
        public LogS98() {
            Tag = Loop = false;

            BaseTick = (double)10 / 1000;
            SyncBufferTicks = 0;
            LoopPoint = 0;
            DeviceList = new List<SoundDevice>();

            LogData = new BinaryWriter(new MemoryStream());
        }

        public LogDeviceOpna GetDevice(int DeviceNo) {
            return LogDeviceData[DeviceNo];
        }

        public void SetDeviceMap() {
            LogDeviceData = DeviceList.Select((x,i) => new LogDeviceOpna(this, i)).ToArray();
        }


        public void WriteToFile(string FilePath) {
            // データ長さの取得
            int DataLength = GetDataLength();

            var bs = new BinaryWriter(new FileStream(FilePath, FileMode.Create));
            bs.Write("S98".ToCharArray());
            bs.Write('3');

            // Numerator(10 if 0) / Denominator(1000 if 0)
            bs.Write(Numerator);
            bs.Write(Denominator);

            // 0x0c COMPRESSING(obsolute)
            bs.Write(0);

            int OffsetToDump = 0x20 + (DeviceList.Count * 0x10);
            int OffsetToDumpEnd = OffsetToDump + DataLength;

            // 0x10 Offset to Tag
            bs.Write(Tag ? OffsetToDumpEnd : 0);

            // 0x14 Offset to Dump
            bs.Write(OffsetToDump);

            // 0x18 Offset to Loop Point
            bs.Write(Loop ? OffsetToDump + LoopPoint : 0);

            // 0x1C Device Count
            bs.Write(DeviceList.Count);

            // 0x20- Device 
            foreach (var o in DeviceList) {
                bs.Write((int)o.Device);
                bs.Write((int)o.Clock);
                bs.Write(o.Pan);
                bs.Write(0);
            }

            var a = new byte[DataLength];
            LogData.BaseStream.Position = 0;
            LogData.BaseStream.Read(a, 0, DataLength);

            bs.Write(a);

            bs.Close();

            bs.Dispose();
        }

        private int GetDataLength() {
            LogData.Flush();
            return (int)LogData.BaseStream.Length;
        }

        public void Close() {
            LogData.Dispose();
        }

        public void AddDevice(SoundDevice Device) {
            DeviceList.Add(Device);
        }

        public void AddDeviceRange(IEnumerable<SoundDevice> deviceList) {
            DeviceList.AddRange(deviceList);
        }


        private void WriteValue(byte v) {
            LogData.Write(v);
        }

        public void SetLoopPoint() {
            Loop = true;
            LoopPoint = GetDataLength();
        }

        public void WriteData(int Device, int Address, int Value) {
            byte d = (byte)(Device * 2 + (Address >= 0x100 ? 1 : 0));
            byte a = (byte)(Address & 0xff);
            byte v = (byte)(Value & 0xff);
            WriteValue(d);
            WriteValue(a);
            WriteValue(v);
        }


        private void WriteSync() {
            if (SyncBufferTicks < BaseTick) return;

            var SyncCount = (int)(SyncBufferTicks / BaseTick);
            SyncBufferTicks -= BaseTick * SyncCount;
            if (SyncCount > 1) {
                WriteSync2(SyncCount);
                return;
            }
            WriteSync1();
        }

        private void WriteSync1() {
            WriteValue(0xff);
        }

        private void WriteSync2(int Count) {
            Count -= 2;
            WriteValue(0xfe);
            while (true) {
                var v = (Count & 0x7f);
                Count = Count >> 7;
                var Next = Count > 0;
                v = v | (Next ? 0x80 : 0x00);

                WriteValue((byte)v);
                if (!Next) break;
            }
        }

        public void WriteEnd() {
            WriteValue(0xfd);
        }

        public void Wait(double Seconds) {
            SyncBufferTicks += Seconds;
            WriteSync();
        }

        public void Dispose() {
            Close();
        }

    }
}
