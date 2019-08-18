namespace LambdaMusic {
    interface ILogChip {
        void KeyOff(int channel);
        void KeyOn(int channel);
        void SetSlot(int channel, bool SlotOn);
        void SetTone(int channel, FMTone tone);
        void SetVolume(int channel, int vol);
        void SetMultiKeySet(int channel, int keyOn);
        void SetNoteNumber(int channel, int noteNumber);
        void SetToneValue(int channel, int toneValue);
    }
}