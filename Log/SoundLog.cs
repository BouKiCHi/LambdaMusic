using System.Collections.Generic;

namespace LambdaMusic {
    interface SoundLog {
        void AddDevice(SoundDevice Device);
        void Close();
        void Dispose();
        void SetLoopPoint();
        void Wait(double Seconds);
        void WriteData(int Device, int Address, int Value);
        void WriteEnd();
        void AddDeviceRange(IEnumerable<SoundDevice> deviceList);

        void SetDeviceMap();

        void WriteToFile(string FilePath);
    }
}