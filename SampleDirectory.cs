using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace samptool
{
    sealed class SampleDirectory
    {
        /*****************/
        /* Class members */
        /*****************/
        private List<Sample> m_samples;
        private bool m_unordered = false;

        /**************/
        /* Properties */
        /**************/
        public int SampleCount
        {
            get { return m_samples.Count; }
        }

        public List<Sample> SampleInfoList
        {
            get { return m_samples; }
        }

        /****************/
        /* Constructors */
        /****************/
        public SampleDirectory(string pathToDirectory)
        {
            string[] files = Directory.GetFiles(pathToDirectory);

            // locate samp
            string sampPath;
            if (!Utilities.TryLocateFileByExtension(files, ".samp", out sampPath))
            {
                throw new ApplicationException("Can't find samp file in directory.");
            }

            // locate sdir
            string sdirPath;
            if (!Utilities.TryLocateFileByExtension(files, ".sdir", out sdirPath))
            {
                throw new ApplicationException("Can't find sdir file in directory.");
            }

            // sdir found, init list
            m_samples = new List<Sample>();

            // read sdir
            using (var sdirAccess = MemoryMappedFile.CreateFromFile(sdirPath).CreateViewAccessor())
            using (var sampAccess = MemoryMappedFile.CreateFromFile(sampPath).CreateViewAccessor())
            {
                InternalReadSamples(sdirAccess, sampAccess);
            }
        }

        /***********/
        /* Methods */
        /***********/     
        public void Save(string pathToDirectory)
        {
            Save(pathToDirectory, Utilities.GetTopLevelDirectoryName(pathToDirectory));
        }

        public void Save(string pathToDirectory, string baseName)
        {
            uint sdirSize;
            uint sampSize;
            InternalCalculateBufferSizes(out sdirSize, out sampSize);

            var sdirPath = Path.Combine(pathToDirectory, baseName + ".sdir");
            var sampPath = Path.Combine(pathToDirectory, baseName + ".samp");
            Utilities.CreateNewFiles(sdirPath, sampPath);

            using (var sdirMmf = MemoryMappedFile.CreateFromFile(sdirPath, FileMode.Open, "sdirMap", sdirSize))
            using (var sdirAccess = sdirMmf.CreateViewAccessor())
            using (var sampMmf = MemoryMappedFile.CreateFromFile(sampPath, FileMode.Open, "sampMap", sampSize))
            using (var sampAccess = sampMmf.CreateViewAccessor())
            {
                InternalWriteSamples(sdirAccess, sampAccess);
            }
        }

        public void ExtractSamples(string directory)
        {
            // just in case it doesn't exist..
            Directory.CreateDirectory(directory);

            Console.Write("Exporting samples");
            int sampleIdx = 0;
            foreach (Sample sample in m_samples)
            {
                string dspPath = Path.Combine(directory, sample.ID + ".dsp");

                var dsp = sample.ConvertToDSPADPCMFile();
                dsp.Save(dspPath);

                if (sampleIdx++ % ((m_samples.Count / 10) + m_samples.Count % 10) == 0)
                    Console.Write('.');
            }

            // close off current line
            Console.WriteLine();
        }

        public void UpdateSamples(string directory)
        {
            var dspFiles = Array.FindAll(Directory.GetFiles(directory), file => Path.GetExtension(file) == ".dsp");
            if (dspFiles.Length == 0)
                throw new ArgumentException("Directory contains no dsp files!");

            Console.Write("Updating samples");

            int dspIdx = 0;
            foreach (string dspPath in dspFiles)
            {
                ushort id;
                string name = Path.GetFileNameWithoutExtension(dspPath);
                if (!ushort.TryParse(name, out id))
                {
                    throw new ApplicationException("Can't extract sample id from " + dspPath + ".\nPlease make sure you keep the original extracted file name.");
                }

                int sampleIdx = id;
                string exceptionSampleIdNotExist = string.Format("Sample id {0} doesn't exist in this file. Can't import {1}.", sampleIdx, dspPath);

                if (m_unordered) // optimisation: only search for the index if it is unordered
                {
                    sampleIdx = m_samples.FindIndex(si => si.ID == id);
                    if (sampleIdx == -1)
                    {
                        throw new ApplicationException(exceptionSampleIdNotExist);
                    }
                }
                else if (sampleIdx + 1 > m_samples.Count)
                {
                    throw new ApplicationException(exceptionSampleIdNotExist);
                }

                // load dsp file
                var dsp = new DSPADPCMFile();
                dsp.Load(dspPath);

                // try to update sample in list
                m_samples[sampleIdx].Update(dsp);

                if (dspIdx++ % ((dspFiles.Length / 10) + dspFiles.Length % 10) == 0)
                    Console.Write('.');
            }

            // go to a new line
            Console.WriteLine();
        }

        // read sample data from mem
        internal void InternalReadSamples(MemoryMappedViewAccessor sdirAccess, MemoryMappedViewAccessor sampAccess)
        {
            Console.WriteLine("Reading sample list..");

            long position = 0;
     
            uint test = sdirAccess.ReadUInt32(position);
            int lastId = -1;
            while (test != uint.MaxValue)
            {
                // read sample info
                var sampleInfo = new Sample();
                sampleInfo.InternalReadData(sdirAccess, sampAccess, ref position);

                if (m_unordered == false)
                {
                    if (!(lastId + 1 == sampleInfo.ID))
                        m_unordered = true;
                    else
                        lastId = sampleInfo.ID;
                }

                // add to list
                m_samples.Add(sampleInfo);

                // read next test value
                test = sdirAccess.ReadUInt32(position);
            }
        }

        // write sample data to mem
        internal void InternalWriteSamples(MemoryMappedViewAccessor sdirAccess, MemoryMappedViewAccessor sampAccess)
        {
            // write sample data
            Console.WriteLine("Writing sample data..");
            long sampPosition = 0;
            for (int i = 0; i < m_samples.Count; i++)
            {
                m_samples[i].m_header.dataOffset = (uint)sampPosition;
                sampAccess.CopyToMemory(ref sampPosition, m_samples[i].m_data);
                sampPosition = (sampPosition + 31) & ~31;
            }

            // write sdir metadata
            Console.WriteLine("Writing sample metadata..");
            long sdirPosition = m_samples.Count * Sample.SampleInfoHeader.SIZE + /* terminator uint */ sizeof(uint);
            for (int i = 0; i < m_samples.Count; i++)
            {
                m_samples[i].InternalWriteMetadata(sdirAccess, ref sdirPosition);
            }

            // write sdir sample headers
            Console.WriteLine("Writing sample headers..");
            sdirPosition = 0;
            for (int i = 0; i < m_samples.Count; i++)
            {
                m_samples[i].InternalWriteHeader(sdirAccess, ref sdirPosition);
            }

            // list terminator
            sdirAccess.WriteBE(sdirPosition, uint.MaxValue);
        }

        // calculate space needed for mem alloc
        private void InternalCalculateBufferSizes(out uint sdirSize, out uint sampSize)
        {
            Console.WriteLine("Calculating buffer sizes..");
            sdirSize = 0;
            sampSize = 0;
            for (int i = 0; i < m_samples.Count; i++)
            {
                // add size for the sample info and metadata headers
                sdirSize += Sample.SampleInfoHeader.SIZE + Sample.SampleDecodingMetadata.SIZE;

                // add size for sample data
                sampSize = (uint)(((sampSize + m_samples[i].m_data.Length) + 31) & ~31);
            }

            // add size for list terminator
            sdirSize += 4;
        }
    }
}
