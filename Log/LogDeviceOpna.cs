using System;
using System.Linq;

namespace LambdaMusic {
    class LogDeviceOpna {
        LogOpna Log;
        LogChannel[] Channel;

        public LogDeviceOpna(SoundLog LogData, int DeviceNo) {
            Log = new LogOpna(LogData, DeviceNo);
            // FM:6 SSG:3 ADPCM:1 Rhythm:1
            Channel = Enumerable.Range(0, 11).Select(x => new LogChannel(x, Log)).ToArray();
        }

        public LogChannel GetChannel(int ch) {
            if (ch < 0 || ch >= Channel.Length) return null;
            return Channel[ch];
        }
    }

    class LogChannel {
        private int Channel;
        private ILogChip Log;
        private bool KeyOnFlag;

        public LogChannel(int channel, ILogChip log) {
            this.Channel = channel;
            this.Log = log;
        }

        public void SetTone(FMTone tone) {
            Log.SetTone(Channel, tone);
        }

        public bool IsKeyOn() {
            return KeyOnFlag;
        }

        public void KeyOn() {
            KeyOnFlag = true;
            Log.KeyOn(Channel);
        }

        public void KeyOff() {
            KeyOnFlag = false;
            Log.KeyOff(Channel);
        }

        public void SetNoteNumber(int NoteNumber) {
            Log.SetNoteNumber(Channel, NoteNumber);
        }

        public void SetVolume(int vol) {
            Log.SetVolume(Channel, vol);
        }

        // リズム系
        public void SetMultiKeySet(int KeyOn) {
            Log.SetMultiKeySet(Channel, KeyOn);
        }

        public void SetToneValue(int v) {
            Log.SetToneValue(Channel, v);
        }
    }

    class LogOpna : ILogChip {
        int DeviceNo;
        SoundLog Log;
        FMTone Tone;
        int Octave;
        int Fnum;
        byte SsgMixer = 0;

        // (144 *　周波数(ノート) * (2^20) / マスタークロック) / 2^(オクターブ(4)-1)
        // 例:A4 : (144 *　440.00 * (2^20) / 7987200) / 2^(4-1)
        // C4 to C5
        int[] FnumberTable = {
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
        int[] SsgTp = {
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

        public LogOpna(SoundLog LogData, int DeviceNo) {
            this.Log = LogData;
            this.DeviceNo = DeviceNo;

            // OPNA mode / IRQ off
            Write(false, 0x29, 0x80);
        }

        public void SetTone(int channel, FMTone tone) {
            Tone = tone;
            Tone?.WriteTone(Log, DeviceNo, channel);
        }

        private void SetFreqFm(int channel, int block, int fnum) {
            bool ext = channel >= 3;
            int ch = (channel % 3);
            byte v = (byte)(Bits.Masked(block, 3, 3) | Bits.Masked(fnum >> 8, 3, 0));

            Write(ext, 0xa4 + ch, v);
            Write(ext, 0xa0 + ch, Bits.Masked(fnum, 8, 0));
        }

        private void SetFreqSsg(int channel, int tp) {
            int ch = (channel % 3);
            byte v = (byte)(Bits.Masked(tp >> 8, 4, 0));

            Write(false, 0x01 + (ch*2), v);
            Write(false, 0x00 + (ch*2), Bits.Masked(tp, 8, 0));
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
            if (channel < 6) {
                if (Tone != null) { Tone.WriteVolume(Log, DeviceNo, channel, vol); }
                return;
            }

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

        public void SetNoteNumber(int channel, int noteNumber) {
            if (channel < 6) { SetNoteFm(channel, noteNumber); return; }
            if (channel < 9) { SetNoteSsg(channel-6, noteNumber); return; }
        }

        private void SetNoteSsg(int channel, int noteNumber) {
            Octave = noteNumber / 12;
            var Note = noteNumber % 12;
            var Shift = Octave - 1;
            // C1からシフト
            Fnum = SsgTp[Note] >> Shift;
            SetFreqSsg(channel, Fnum);
        }

        private void SetNoteFm(int channel, int noteNumber) {
            Octave = noteNumber / 12;
            var Note = noteNumber % 12;
            Fnum = FnumberTable[Note];
            SetFreqFm(channel, Octave, Fnum);
        }

        public void SetToneValue(int channel, int toneValue) {
            if (channel < 6 || 9 <= channel) return;
            channel -= 6;
        }
    }

}