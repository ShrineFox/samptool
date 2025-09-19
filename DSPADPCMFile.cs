using System.IO;
using System.IO.MemoryMappedFiles;

namespace samptool
{
    sealed class DSPADPCMFile
    {
        /*****************/
        /* Class members */
        /*****************/
        internal DSPADPCMHeader m_header;
        internal byte[] m_data;

        /**************/
        /* Properties */
        /**************/
        public uint SampleCount
        {
            get { return m_header.numSamples; }
        }

        public bool IsLooped
        {
            get { return m_header.loopFlag == 1; }
        }

        public uint LoopStart
        {
            get { return Utilities.NibblesToSamples(m_header.loopStartAddress); }
        }

        public uint LoopEnd
        {
            get { return Utilities.NibblesToSamples(m_header.loopEndAddress); }
        }

        public ushort[] Coefficients
        {
            get { return m_header.coefficients; }
        }

        public ushort PredictorScale
        {
            get { return m_header.predictorScale; }
        }

        public ushort SampleHistory1
        {
            get { return m_header.sampleHistory1; }
        }

        public ushort SampleHistory2
        {
            get { return m_header.sampleHistory2; }
        }

        public ushort LoopPredictorScale
        {
            get { return m_header.loopPredictorScale; }
        }

        public ushort LoopSampleHistory1
        {
            get { return m_header.loopSampleHistory1; }
        }

        public ushort LoopSampleHistory2
        {
            get { return m_header.loopSampleHistory2; }
        }

        /***********/
        /* Methods */
        /***********/
        public void Save(string path)
        {
            Utilities.CreateNewFile(path);

            using (var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, nameof(Save), DSPADPCMHeader.SIZE + m_data.Length))
            using (var access = mmf.CreateViewAccessor())
            {
                InternalWriteData(access);
            }
        }

        public void Load(string path)
        {
            using (var mmf = MemoryMappedFile.CreateFromFile(path))
            using (var access = mmf.CreateViewAccessor())
            {
                InternalReadData(access);
            }
        }

        // read the data from memory
        internal void InternalReadData(MemoryMappedViewAccessor access)
        {
            long position = 0;
            access.ReadUInt32BE(ref position, out m_header.numSamples);
            access.ReadUInt32BE(ref position, out m_header.numAdpcmNibbles);
            access.ReadUInt32BE(ref position, out m_header.sampleRate);
            access.ReadUInt16BE(ref position, out m_header.loopFlag);
            access.ReadUInt16BE(ref position, out m_header.format);
            access.ReadUInt32BE(ref position, out m_header.loopStartAddress);
            access.ReadUInt32BE(ref position, out m_header.loopEndAddress);
            access.ReadUInt32BE(ref position, out m_header.initialOffset);

            m_header.coefficients = new ushort[16];
            for (int i = 0; i < m_header.coefficients.Length; i++)
                 access.ReadUInt16BE(ref position, out m_header.coefficients[i]);

            access.ReadUInt16BE(ref position, out m_header.gain);
            access.ReadUInt16BE(ref position, out m_header.predictorScale);
            access.ReadUInt16BE(ref position, out m_header.sampleHistory1);
            access.ReadUInt16BE(ref position, out m_header.sampleHistory2);
            access.ReadUInt16BE(ref position, out m_header.loopPredictorScale);
            access.ReadUInt16BE(ref position, out m_header.loopSampleHistory1);
            access.ReadUInt16BE(ref position, out m_header.loopSampleHistory2);

            m_header.pad = new ushort[11];
            for (int i = 0; i < m_header.pad.Length; i++)
                access.ReadUInt16BE(ref position, out m_header.pad[i]);

            access.CopyFromMemory(ref position, (int)Utilities.SamplesToBytes(m_header.numSamples), out m_data);
        }

        // write the data to memory
        internal void InternalWriteData(MemoryMappedViewAccessor access)
        {
            long position = 0;
            access.WriteBE(ref position, m_header.numSamples);
            access.WriteBE(ref position, m_header.numAdpcmNibbles);
            access.WriteBE(ref position, m_header.sampleRate);
            access.WriteBE(ref position, m_header.loopFlag);
            access.WriteBE(ref position, m_header.format);
            access.WriteBE(ref position, m_header.loopStartAddress);
            access.WriteBE(ref position, m_header.loopEndAddress);
            access.WriteBE(ref position, m_header.initialOffset);

            for (int i = 0; i < m_header.coefficients.Length; i++)
                access.WriteBE(ref position, m_header.coefficients[i]);

            access.WriteBE(ref position, m_header.gain);
            access.WriteBE(ref position, m_header.predictorScale);
            access.WriteBE(ref position, m_header.sampleHistory1);
            access.WriteBE(ref position, m_header.sampleHistory2);
            access.WriteBE(ref position, m_header.loopPredictorScale);
            access.WriteBE(ref position, m_header.loopSampleHistory1);
            access.WriteBE(ref position, m_header.loopSampleHistory2);

            for (int i = 0; i < m_header.pad.Length; i++)
                access.WriteBE(ref position, m_header.pad[i]);

            access.CopyToMemory(ref position, m_data);
        }

        // dsp header struct, contains most of the data in this class
        internal struct DSPADPCMHeader
        {
            public static int SIZE = 96;

            public uint numSamples;
            public uint numAdpcmNibbles;
            public uint sampleRate;
            public ushort loopFlag; // bool
            public ushort format; // always 0
            public uint loopStartAddress; // in nibbles
            public uint loopEndAddress; // in nibbles
            public uint initialOffset; // in nibbles
            public ushort[] coefficients; // 8 pairs of 16-bit words
            public ushort gain;
            public ushort predictorScale;
            public ushort sampleHistory1;
            public ushort sampleHistory2;
            public ushort loopPredictorScale;
            public ushort loopSampleHistory1;
            public ushort loopSampleHistory2;
            public ushort[] pad; // 11
        }
    }
}
