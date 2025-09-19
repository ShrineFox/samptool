using System.IO.MemoryMappedFiles;

namespace samptool
{
    static class MemoryMappedViewAccessorExtensions
    {
        #region Big endian reading extensions 

        public static void ReadUInt32BE(this MemoryMappedViewAccessor accessor, long position, out uint value)
        {
            value = Utilities.Swap(accessor.ReadUInt32(position));
        }

        public static void ReadUInt32BE(this MemoryMappedViewAccessor accessor, ref long position, out uint value)
        {
            value = Utilities.Swap(accessor.ReadUInt32(position));
            position += sizeof(uint);
        }

        public static void ReadUInt16BE(this MemoryMappedViewAccessor accessor, long position, out ushort value)
        {
            value = Utilities.Swap(accessor.ReadUInt16(position));
        }

        public static void ReadUInt16BE(this MemoryMappedViewAccessor accessor, ref long position, out ushort value)
        {
            value = Utilities.Swap(accessor.ReadUInt16(position));
            position += sizeof(ushort);
        }

        public static void ReadByte(this MemoryMappedViewAccessor accessor, ref long position, out byte value)
        {
            value = accessor.ReadByte(position);
            position += sizeof(byte);
        }

        #endregion

        #region Big endian writing extension

        public static void WriteBE(this MemoryMappedViewAccessor accessor, long position, uint value)
        {
            accessor.Write(position, Utilities.Swap(value));
        }

        public static void WriteBE(this MemoryMappedViewAccessor accessor, ref long position, uint value)
        {
            accessor.Write(position, Utilities.Swap(value));
            position += sizeof(uint);
        }

        public static void WriteBE(this MemoryMappedViewAccessor accessor, long position, ushort value)
        {
            accessor.Write(position, Utilities.Swap(value));
        }

        public static void WriteBE(this MemoryMappedViewAccessor accessor, ref long position, ushort value)
        {
            accessor.Write(position, Utilities.Swap(value));
            position += sizeof(ushort);
        }

        #endregion

        #region Reading extensions

        public unsafe static void CopyFromMemory(this MemoryMappedViewAccessor accessor, long position, int count, out byte[] array)
        {
            // alloc array
            array = new byte[count];

            // get pointer from mem
            byte* src = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref src);

            // move the pointer to the copy position
            src += position;

            // calculate counts
            int intCount = array.Length / 4;
            int rest = array.Length % 4;

            fixed (byte* dst = array)
            {
                int* srcInt = (int*)src;
                int* dstInt = (int*)dst;

                // copy 32 bit elements first
                for (int i = 0; i < intCount; i++, dstInt++, srcInt++)
                    *dstInt = *srcInt;

                if (rest != 0)
                {
                    byte* srcByte = (byte*)srcInt;
                    byte* dstByte = (byte*)dstInt;

                    // copy remaining single bytes last
                    for (int i = 0; i < rest; i++, dstByte++, srcByte++)
                        *dstByte = *srcByte;
                }
            }

            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }

        public static void CopyFromMemory(this MemoryMappedViewAccessor accessor, ref long position, int count, out byte[] array)
        {
            CopyFromMemory(accessor, position, count, out array);
            position += count;
        }

        #endregion

        #region Writing extensions

        public static void Write(this MemoryMappedViewAccessor accessor, ref long position, byte value)
        {
            accessor.Write(position, value);
            position += 1;
        }

        public unsafe static void CopyToMemory(this MemoryMappedViewAccessor accessor, long position, byte[] array)
        {
            // get mem ptr
            byte* dst = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref dst);

            // move the pointer to the copy position
            dst += position;

            // calculate counts
            int numInts = array.Length / 4;
            int numRestBytes = array.Length % 4;

            fixed (byte* src = array)
            {
                int* srcInt = (int*)src;
                int* dstInt = (int*)dst;

                // copy 32 bit elements first
                for (int i = 0; i < numInts; i++, dstInt++, srcInt++)
                    *dstInt = *srcInt;

                if (numRestBytes != 0)
                {
                    byte* srcByte = (byte*)srcInt;
                    byte* dstByte = (byte*)dstInt;

                    // copy remaining single bytes last
                    for (int i = 0; i < numRestBytes; i++, dstByte++, srcByte++)
                        *dstByte = *srcByte;
                }
            }

            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }

        public static void CopyToMemory(this MemoryMappedViewAccessor accessor, ref long position, byte[] array)
        {
            accessor.CopyToMemory(position, array);
            position += array.Length;
        }

        #endregion
    }
}
