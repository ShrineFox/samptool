using System.IO.MemoryMappedFiles;

namespace samptool
{
    sealed class Sample
    {
        /*****************/
        /* Class members */
        /*****************/
        internal SampleInfoHeader m_header;
        internal SampleDecodingMetadata m_metadata;
        internal byte[] m_data;

        /**************/
        /* Properties */
        /**************/
        public ushort ID
        {
            get { return m_header.id; }
        }

        public ushort SampleRate
        {
            get { return m_header.sampleRate; }
        }

        public uint SampleCount
        {
            get { return m_header.numSamples; }
        }

        public uint LoopStart
        {
            get { return m_header.loopStart; }
        }

        public uint LoopEnd
        {
            get { return m_header.loopLength; }
        }

        public byte Predictor
        {
            get { return m_metadata.predictorScale; }
        }

        public byte LoopPredictor
        {
            get { return m_metadata.loopPredictorScale; }
        }

        public ushort LoopSampleHistory2
        {
            get { return m_metadata.loopSampleHistory2; }
        }

        public ushort LoopSampleHistory1
        {
            get { return m_metadata.loopSampleHistory1; }
        }

        public ushort[] Coefficients
        {
            get { return m_metadata.coefficients; }
        }

        public byte[] SampleData
        {
            get { return m_data; }
        }

        /***********/
        /* Methods */
        /***********/
        internal DSPADPCMFile ConvertToDSPADPCMFile()
        {
            var dspHdr = new DSPADPCMFile.DSPADPCMHeader();
            dspHdr.numSamples = m_header.numSamples;
            dspHdr.numAdpcmNibbles = Utilities.SamplesToNibbles(m_header.numSamples);
            dspHdr.sampleRate = m_header.sampleRate;

            bool isLooped = m_header.loopLength > 0;
            if (isLooped)
            {
                dspHdr.loopFlag = 1;
                dspHdr.loopStartAddress = Utilities.SamplesToNibbles(m_header.loopStart);
                dspHdr.loopEndAddress = Utilities.SamplesToNibbles(m_header.loopStart + m_header.loopLength) - 1;
            }
            else
            {
                dspHdr.loopFlag = 0;
                dspHdr.loopStartAddress = 2;
                dspHdr.loopEndAddress = 0;
            }

            dspHdr.format = 0;
            dspHdr.initialOffset = 0;
            dspHdr.coefficients = m_metadata.coefficients;
            dspHdr.gain = 0;
            dspHdr.predictorScale = m_metadata.predictorScale;
            dspHdr.sampleHistory1 = 0;
            dspHdr.sampleHistory2 = 2;
            dspHdr.loopPredictorScale = m_metadata.loopPredictorScale;
            dspHdr.loopSampleHistory1 = m_metadata.loopSampleHistory1;
            dspHdr.loopSampleHistory2 = m_metadata.loopSampleHistory2;
            dspHdr.pad = new ushort[11];

            var dsp = new DSPADPCMFile();
            dsp.m_header = dspHdr;
            dsp.m_data = m_data;

            return dsp;
        }

        // update header values with the header values from the dsp, but we still have to update the data offsets
        internal void Update(DSPADPCMFile dsp)
        {
            // we're only updating header values at the moment
            var dspHdr = dsp.m_header;

            // update info header
            m_header.dataOffset = 0xDEADBEEF; // invalidate offset
            m_header.sampleRate = (ushort)dspHdr.sampleRate;

            if (dspHdr.loopFlag == 1)
            {
                m_header.loopStart = Utilities.NibblesToSamples(dspHdr.loopStartAddress);
                m_header.loopLength = Utilities.NibblesToSamples(dspHdr.loopEndAddress - dspHdr.loopStartAddress) + 1;
            }
            else
            {
                m_header.loopStart = 0;
                m_header.loopLength = 0;
            }

            m_header.metadataOffset = 0xDEADBEEF; // invalidate offset

            // update decoding header
            m_metadata.predictorScale = (byte)dspHdr.predictorScale;
            m_metadata.loopPredictorScale = (byte)dspHdr.loopPredictorScale;
            m_metadata.loopSampleHistory1 = dspHdr.loopSampleHistory1;
            m_metadata.loopSampleHistory2 = dspHdr.loopSampleHistory2;
            m_metadata.coefficients = dspHdr.coefficients;

            // update the data
            m_data = dsp.m_data;
        }

        // read the data from memory
        internal void InternalDeserializeData(MemoryMappedViewAccessor sdirAccess, MemoryMappedViewAccessor sampAccess, ref long position)
        {
            // read header
            m_header.id = sdirAccess.ReadUInt16BE(ref position);
            m_header.field02 = sdirAccess.ReadUInt16BE(ref position);
            m_header.dataOffset = sdirAccess.ReadUInt32BE(ref position);
            m_header.field08 = sdirAccess.ReadUInt32BE(ref position);
            m_header.field0C = sdirAccess.ReadUInt16BE(ref position);
            m_header.sampleRate = sdirAccess.ReadUInt16BE(ref position);
            m_header.numSamples = sdirAccess.ReadUInt32BE(ref position);
            m_header.loopStart = sdirAccess.ReadUInt32BE(ref position);
            m_header.loopLength = sdirAccess.ReadUInt32BE(ref position);
            m_header.metadataOffset = sdirAccess.ReadUInt32BE(ref position);

            // read metadata
            m_metadata.field00 = sdirAccess.ReadUInt16BE(m_header.metadataOffset);
            m_metadata.predictorScale = sdirAccess.ReadByte(m_header.metadataOffset + 2);
            m_metadata.loopPredictorScale = sdirAccess.ReadByte(m_header.metadataOffset + 3);
            m_metadata.loopSampleHistory2 = sdirAccess.ReadUInt16BE(m_header.metadataOffset + 4);
            m_metadata.loopSampleHistory1 = sdirAccess.ReadUInt16BE(m_header.metadataOffset + 6);
            m_metadata.coefficients = new ushort[16];
            for (int i = 0; i < m_metadata.coefficients.Length; i++)
                m_metadata.coefficients[i] = sdirAccess.ReadUInt16BE(m_header.metadataOffset + 8 + i * sizeof(ushort));

            // read raw adpcm data
            sampAccess.CopyFromMemory(m_header.dataOffset, (int)Utilities.SamplesToBytes(m_header.numSamples), out m_data);
        }

        // write metadata header
        internal void InternalSerializeMetadata(MemoryMappedViewAccessor access, ref long position)
        {
            m_header.metadataOffset = (uint)position;
            access.WriteBE(ref position, m_metadata.field00);
            access.Write(ref position, m_metadata.predictorScale);
            access.Write(ref position, m_metadata.loopPredictorScale);
            access.WriteBE(ref position, m_metadata.loopSampleHistory2);
            access.WriteBE(ref position, m_metadata.loopSampleHistory1);
            for (int i = 0; i < m_metadata.coefficients.Length; i++)
                access.WriteBE(ref position, m_metadata.coefficients[i]);
        }

        // write info header
        internal void InternalSerializeHeader(MemoryMappedViewAccessor access, ref long position)
        {
            access.WriteBE(ref position, m_header.id);
            access.WriteBE(ref position, m_header.field02);
            access.WriteBE(ref position, m_header.dataOffset);
            access.WriteBE(ref position, m_header.field08);
            access.WriteBE(ref position, m_header.field0C);
            access.WriteBE(ref position, m_header.sampleRate);
            access.WriteBE(ref position, m_header.numSamples);
            access.WriteBE(ref position, m_header.loopStart);
            access.WriteBE(ref position, m_header.loopLength);
            access.WriteBE(ref position, m_header.metadataOffset);
        }

        // sample info header, essentially the internal structure of this class
        internal struct SampleInfoHeader
        {
            public static uint Size = 32;

            public ushort id;
            public ushort field02;
            public uint dataOffset; // offset in .samp
            public uint field08;
            public ushort field0C; // 0x3c or 0x48
            public ushort sampleRate;
            public uint numSamples; // in raw samples
            public uint loopStart; // in raw samples
            public uint loopLength; // in raw samples
            public uint metadataOffset; // decoding metadata
        }

        // struct containing data needed for decoding the sound, used for converting to standard dsp as well
        internal unsafe struct SampleDecodingMetadata
        {
            public static uint Size = 40;

            public ushort field00;
            public byte predictorScale;
            public byte loopPredictorScale;
            public ushort loopSampleHistory2;
            public ushort loopSampleHistory1;
            public ushort[] coefficients;
        }
    }
}
