using System;
using System.Linq;

namespace LambdaMusic {
    class LogDeviceOpna {
        LogOpna Log;
        LogChannel[] Channel;

        public LogDeviceOpna(SoundLog LogData, int DeviceNo) {
            Log = new LogOpna(LogData, DeviceNo);
            // FM:6 SSG:3 ADPCM:1 Rhythm:1
            Channel = Enumerable.Range(0, 11).Select(x => new LogChannel(DeviceNo, x, Log)).ToArray();
        }

        public LogChannel GetChannel(int ch) {
            if (ch < 0 || ch >= Channel.Length) return null;
            return Channel[ch];
        }
    }
    
    // OPNAのチャンネル単位
    class LogChannel {
        private int DeviceNo;
        private int Channel;
        private ILogChip Chip;
        private bool KeyOnFlag;

        FMTone Tone = null;
        int Octave;
        int NoteFine;
        int PitchValue;


        public LogChannel(int device, int channel, ILogChip chip) {
            this.DeviceNo = device;
            this.Channel = channel;
            this.Chip = chip;
        }

        public void SetTone(FMTone tone) {
            Tone = tone;
            Chip.SetTone(Channel, tone);
        }

        public bool IsKeyOn() {
            return KeyOnFlag;
        }

        public void KeyOn() {
            KeyOnFlag = true;
            Chip.KeyOn(Channel);
        }

        public void KeyOff() {
            KeyOnFlag = false;
            Chip.KeyOff(Channel);
        }

        public void SetNoteNumber(int NoteNumber) {
            Octave = NoteNumber / 12;
            NoteFine = NoteNumber % 12;

            if (Channel < 6) { SetNoteNumberFm(); return;  }
            if (Channel < 9) { SetNoteNumberSsg(); return; }
        }

        private void SetNoteNumberFm() {
            PitchValue = OpnaTable.GetFmPitch(Octave, NoteFine);
            Chip.SetPitchValue(Channel, Octave, PitchValue);
        }

        private void SetNoteNumberSsg() {
            PitchValue = OpnaTable.GetSsgPitch(Octave, NoteFine);
            Chip.SetPitchValue(Channel, Octave, PitchValue);
        }        

        public void SetVolume(int vol) {
            if (Tone != null) {
                Chip.WriteVolume(Channel, Tone, vol);
                return;
            }
            Chip.SetVolume(Channel, vol);
        }

        // リズム系
        public void SetMultiKeySet(int KeyOn) {
            Chip.SetMultiKeySet(Channel, KeyOn);
        }
    }

    class OpnaTable {
        // (144 *　周波数(ノート) * (2^20) / マスタークロック) / 2^(オクターブ(4)-1)
        // 例:A4 : (144 *　440.00 * (2^20) / 7987200) / 2^(4-1)
        // C4 to C5
        public static int[] FnumberTable = {
            618,
            655,
            694,
            735,
            779,
            825,
            874,
            926,
            981,
            1040,
            1102,
            1167,
            1236,
        };


        // 例:A1 (7987200 / 440.00 / (2^3)) / 64
        // C1 to C2
        public static int[] SsgTp = {
            3816,
            3602,
            3400,
            3209,
            3029,
            2859,
            2698,
            2547,
            2404,
            2269,
            2142,
            2022,
            1908,
        };

        public static int GetFmPitch(int Octave, int NoteFine) {
            return FnumberTable[NoteFine];
        }

        public static int GetSsgPitch(int Octave, int NoteFine) {
            // C1からシフト
            var Shift = Octave - 1;
            return SsgTp[NoteFine] >> Shift;
        }
    }

    class LogOpna : ILogChip {
        /// チップ単位で共有
        int DeviceNo;
        SoundLog Log;
        byte SsgMixer = 0;



        public LogOpna(SoundLog LogData, int DeviceNo) {
            this.Log = LogData;
            this.DeviceNo = DeviceNo;

            // OPNA mode / IRQ off
            Write(false, 0x29, 0x80);
        }

        public void SetTone(int channel, FMTone tone) {
            tone.WriteTone(Log, DeviceNo, channel);
        }

        public void WriteVolume(int channel, FMTone tone, int vol) {
            tone.WriteVolume(Log, DeviceNo, channel, vol);
        }

        public void SetPitchValue(int channel, int octave, int pitch) {
            if (channel < 6) { SetPitchValueFm(channel, octave, pitch); return; }
            if (channel < 9) { SetPitchValueSsg(channel - 6, octave, pitch); return; }
        }

        private void SetPitchValueFm(int channel, int block, int value) {
            bool ext = channel >= 3;
            int ch = (channel % 3);
            byte v = (byte)(Bits.Masked(block, 3, 3) | Bits.Masked(value >> 8, 3, 0));

            Write(ext, 0xa4 + ch, v);
            Write(ext, 0xa0 + ch, Bits.Masked(value, 8, 0));
        }

        private void SetPitchValueSsg(int channel, int block, int value) {
            int ch = (channel % 3);
            byte v = (byte)(Bits.Masked(value >> 8, 4, 0));

            Write(false, 0x01 + (ch*2), v);
            Write(false, 0x00 + (ch*2), Bits.Masked(value, 8, 0));
        }

        public void SetSlot(int channel, bool SlotOn) {
            if (channel < 6) { SetSlotFm(channel, SlotOn); return; }
            if (channel < 9) { SetSlotSsg(channel-6, SlotOn); return; }
        }

        private void SetSlotFm(int channel, bool SlotOn) {
            bool ext = channel >= 3;
            int ch = (channel % 3);
            var Slot = SlotOn ? 0x0f : 0x00;
            var ExtraChannel = ext ? 4 : 0;

            byte v = (byte)(Bits.Masked(Slot, 4, 4) | ExtraChannel | Bits.Masked(ch, 2, 0));
            Write(false, 0x28, v);
        }

        private void SetSlotSsg(int channel, bool SlotOn) {
            int ch = (channel % 3);
            byte Shift = (byte)(1 << ch);
            byte Mask = (byte)(0xff ^ Shift);
            SsgMixer |= 0x38;
            SsgMixer &= Mask;
            // 0で出力
            if (!SlotOn) SsgMixer |= Shift;
            Write(false, 0x07, SsgMixer);
        }

        public void RhythmOn(int value) {

        }

        public void KeyOn(int channel) {
            SetSlot(channel, true);
        }

        public void KeyOff(int channel) {
            SetSlot(channel, false);
        }

        private void Write(bool ext, int adr, int val) {
            Log.WriteData(DeviceNo, adr + (ext ? 0x100 : 0), val);
        }

        public void SetVolume(int channel, int vol) {
            if (channel < 6) return;
            if (channel < 9) {
                SetVolumeSsg(channel - 6, vol);
                return;
            }
        }

        private void SetVolumeSsg(int channel, int vol) {
            int ch = (channel % 3);
            byte v = (byte)(vol & 0x0f);

            Write(false, 0x08 + ch, v);
        }

        public void SetOpLevel(int channel, int op, int level) {

        }

        public void SetMultiKeySet(int channel, int keyOn) {

        }


    }

}