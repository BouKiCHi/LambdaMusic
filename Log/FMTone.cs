using LambdaMusic.Compile;
using System.Collections.Generic;

namespace LambdaMusic {

    enum ToneType {
        Unknown,
        OPNA,
    }

    class FMTone {

        public FMToneData ToneData = new FMToneData();

        public ToneType GetToneType(string Text) {
            if (Text == "OPNA") return ToneType.OPNA;
            return ToneType.Unknown;
        }

        public void SetToneFromText(ToneType Type, string Text) {
            List<int> Tone = ToneText.ToList(Text);
            var Parameter = Tone.ToArray();
            SetToneParameter(Type, Parameter);
        }


        public void SetToneParameter(ToneType Type, int[] Parameter) {
            ToneData.SetData(Type, Parameter);
        }

        public void WriteTone(SoundLog Log, int Device, int Channel) {
            ToneData.WriteTone(Log, Device, Channel);
        }

        public void WriteVolume(SoundLog Log, int Device, int Channel, int Volume) {
            ToneData.WriteVolume(Log, Device, Channel, Volume);
        }

    }

    class FMToneData {
        public ToneType Type;
        public int Algorithm;
        public int Feedback;
        public FMOperator[] Operator = new FMOperator[4];

        public void SetData(ToneType Type, int[] Parameter) {
            this.Type = Type;
            switch (Type) {
                case ToneType.OPNA: SetDataOPNA(Parameter); break;
            }
        }

        private void SetDataOPNA(int[] ToneValue) {
            Algorithm = ToneValue[0];
            Feedback = ToneValue[1];

            for (var i = 0; i < 4; i++) {
                var o = new FMOperator();
                Operator[i] = o;
                var BaseIndex = (i * 10) + 2;

                o.AttackRate = ToneValue[BaseIndex + 0];
                o.DecayRate = ToneValue[BaseIndex + 1];
                o.SustainRate = ToneValue[BaseIndex + 2];
                o.ReleaseRate = ToneValue[BaseIndex + 3];
                o.SustainLevel = ToneValue[BaseIndex + 4];
                o.OutputLevel = ToneValue[BaseIndex + 5];
                o.KeyScale = ToneValue[BaseIndex + 6];
                o.Multiple = ToneValue[BaseIndex + 7];
                o.Detune = ToneValue[BaseIndex + 8];
                o.AmplitudeModulation = ToneValue[BaseIndex + 9];
            }
        }

        public void WriteVolume(SoundLog Log, int Device, int Channel,int Volume) {
            switch (Type) {
                case ToneType.OPNA: WriteVolumeOPNA(Log, Device, Channel, Volume); break;
            }
        }


        public void WriteTone(SoundLog Log,int Device, int Channel) {
            switch (Type) {
                case ToneType.OPNA: WriteOPNA(Log, Device, Channel); break;
            }
        }

        private void WriteOPNA(SoundLog Log, int Device, int Channel) {
            bool ext = Channel >= 3;
            int c = Channel % 3;
            int adr;
            byte v;
            for (var opn = 0; opn < Operator.Length; opn++) {
                var o = Operator[opn];

                // 0x30 Detune / Multiple
                adr = CalcAddress(ext, 0x30 + 4 * opn, c);
                v = (byte)(Bits.Masked(o.Detune, 3, 4) | Bits.Masked(o.Multiple, 4, 0));

                Log.WriteData(Device, adr, v);

                // 0x40    Output Level(TotalLevel)
                adr = CalcAddress(ext, 0x40 + 4 * opn, c);
                v = (byte)(Bits.Masked(o.OutputLevel, 7, 0));
                Log.WriteData(Device, adr, v);

                // 0x50    KeyScale / Attack Rate
                adr = CalcAddress(ext, 0x50 + 4 * opn, c);
                v = (byte)(Bits.Masked(o.KeyScale, 2, 6) | Bits.Masked(o.AttackRate, 5, 0));
                Log.WriteData(Device, adr, v);

                // 0x60    Decay Rate
                adr = CalcAddress(ext, 0x60 + 4 * opn, c);
                v = (byte)(Bits.Masked(o.DecayRate, 5, 0));
                Log.WriteData(Device, adr, v);

                // 0x70    Sustain Rate
                adr = CalcAddress(ext, 0x70 + 4 * opn, c);
                v = (byte)(Bits.Masked(o.SustainRate, 5, 0));
                Log.WriteData(Device, adr, v);

                // 0x80    Sustain Level / Release Rate
                adr = CalcAddress(ext, 0x80 + 4 * opn, c);
                v = (byte)(Bits.Masked(o.SustainLevel, 4, 4) | Bits.Masked(o.ReleaseRate, 4, 0));
                Log.WriteData(Device, adr, v);

                // 0x90    SSG-EG ?
            }

            // 0xB0 Feedback/ Connect
            adr = CalcAddress(ext, 0xb0, c);
            v = (byte)(Bits.Masked(Feedback, 3, 3) | Bits.Masked(Algorithm, 3, 0));
            Log.WriteData(Device, adr, v);
        }

        private void WriteVolumeOPNA(SoundLog Log, int Device, int Channel, int Volume) {
            bool ext = Channel >= 3;
            int c = Channel % 3;
            int adr;
            byte v;
            for (var opn = 0; opn < Operator.Length; opn++) {
                var o = Operator[opn];

                // 0x40    Output Level(TotalLevel)
                var diff = (127 - o.OutputLevel);
                var ol = 127 - ((diff * Volume) / 15);
                
                adr = CalcAddress(ext, 0x40 + 4 * opn, c);
                v = (byte)(Bits.Masked(ol, 7, 0));
                Log.WriteData(Device, adr, v);
            }
        }

        private  int CalcAddress(bool Extend,int Address, int Channel) {
            return (Extend ? 0x100 : 0) + Address + Channel;
        }
    }

    class Bits {
        public static int Masked(int Value, int Bits, int LeftShift) {
            int[] Mask = { 0, 0x1, 0x3, 0x7, 0xf, 0x1f, 0x3f, 0x7f, 0xff };
            return (Value & Mask[Bits]) << LeftShift;
        }
    }


    class FMOperator {
        public int AttackRate;
        public int DecayRate;
        public int SustainRate;
        public int ReleaseRate;
        public int SustainLevel;
        public int OutputLevel;
        public int KeyScale;
        public int Multiple;
        public int Detune;
        public int AmplitudeModulation;
    }
}
