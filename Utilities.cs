using System;
using System.IO;

namespace samptool
{
    static class Utilities
    {
        public static unsafe ushort Swap(ushort value)
        {
            ushort valueCopy = value;
            byte* valueCopyPtr = (byte*)&valueCopy + 1;
            byte* valuePtr = (byte*)&value;
            *valuePtr++ = *valueCopyPtr--;
            *valuePtr = *valueCopyPtr;
            return value;
        }

        public static unsafe uint Swap(uint value)
        {
            uint valueCopy = value;
            byte* valueCopyPtr = (byte*)&valueCopy + 3;
            byte* valuePtr = (byte*)&value;
            *valuePtr++ = *valueCopyPtr--;
            *valuePtr++ = *valueCopyPtr--;
            *valuePtr++ = *valueCopyPtr--;
            *valuePtr = *valueCopyPtr;
            return value;
        }

        public static uint SamplesToBytes(uint numSamples)
        {
            uint nibbles = SamplesToNibbles(numSamples);
            return (nibbles / 2) + (nibbles % 2);
        }

        public static uint BytesToSamples(uint numBytes)
        {
            uint nibbles = (numBytes * 2) - (1 - (numBytes % 2));
            return NibblesToSamples(nibbles);
        }

        public static uint SamplesToNibbles(uint numSamples)
        {
            uint wholeFrames = numSamples / 14;
            uint remainder = numSamples % 14;
            return (wholeFrames * 16) + (remainder > 0 ? (remainder + 2) : 0);
        }

        public static uint NibblesToSamples(uint numNibbles)
        {
            uint wholeFrames = numNibbles / 16;
            uint remainder = numNibbles % 16;
            return (wholeFrames * 14) + (remainder > 0 ? (remainder - 2) : 0);
        }

        public static void CreateNewFile(string path)
        {
            using (File.Create(path)) { }
        }

        public static void CreateNewFiles(params string[] paths)
        {
            foreach (string path in paths)
            {
                using (File.Create(path)) { }
            }
        }

        public static bool TryLocateFileByExtension(string[] paths, string extension, out string path)
        {
            // locate file
            var idx = Array.FindIndex(paths, f => Path.GetExtension(f) == extension);

            if (idx == -1)
            {
                // not found
                path = null;
                return false;
            }
            else
            {
                // found
                path = paths[idx];
                return true;
            }
        }

        public static string GetTopLevelDirectoryName(string path)
        {
            var dirs = path.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            if (dirs.Length == 0)
                return null;
            else
                return dirs[dirs.Length - 1];
        }
    }
}
