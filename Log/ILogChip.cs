namespace LambdaMusic {
    interface ILogChip {
        void KeyOff(int channel);
        void KeyOn(int channel);
        void SetSlot(int channel, bool SlotOn);
        void SetVolume(int channel, int vol);
        void SetMultiKeySet(int channel, int keyOn);
        void WriteVolume(int channel, FMTone tone, int vol);
        void SetPitchValue(int channel, int octave, int pitch);
        void SetTone(int channel, FMTone tone);
    }
}