using System;
using System.IO;

namespace Masterloop.Codecs
{
    public class BigEndianReader : IDisposable
    {
        private BinaryReader _baseReader;

        public BigEndianReader(BinaryReader baseReader)
        {
            _baseReader = baseReader;
        }

        #region Dispose
        // Flag: Has Dispose already been called? 
        private bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Close();
            }

            disposed = true;
        }

        ~BigEndianReader()
        {
            Dispose(false);
        }
        #endregion

        public byte ReadByte()
        {
            return _baseReader.ReadByte();
        }

        public char ReadChar()
        {
            return _baseReader.ReadChar();
        }

        public byte[] ReadBytes(int count)
        {
            return _baseReader.ReadBytes(count);
        }

        public char[] ReadChars(int count)
        {
            return _baseReader.ReadChars(count);
        }

        public Int16 ReadInt16()
        {
            return BitConverter.ToInt16(ReadBigEndianBytes(2), 0);
        }

        public UInt16 ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBigEndianBytes(2), 0);
        }

        public Int32 ReadInt32()
        {
            return BitConverter.ToInt32(ReadBigEndianBytes(4), 0);
        }

        public UInt32 ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBigEndianBytes(4), 0);
        }

        public Int64 ReadInt64()
        {
            return BitConverter.ToInt64(ReadBigEndianBytes(8), 0);
        }

        public UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBigEndianBytes(8), 0);
        }

        public Double ReadSingle()
        {
            return BitConverter.ToSingle(ReadBigEndianBytes(4), 0);
        }

        public Double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBigEndianBytes(8), 0);
        }

        public byte[] ReadBigEndianBytes(int count)
        {
            byte[] bytes = _baseReader.ReadBytes(count);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public void Close()
        {
            _baseReader.Close();
        }

        public Stream BaseStream
        {
            get { return _baseReader.BaseStream; }
        }
    }
}