﻿namespace Heroes.ReplayParser.Streams
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A basic little-endian bitstream reader.
    /// </summary>
    public class BitReader
    {
        public Stream stream;

        private int currentByte;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitReader"/> class.
        /// </summary>
        /// <param name="stream"> The stream. </param>
        public BitReader(Stream stream)
        {
            this.stream = stream;
            this.Cursor = 0;
        }

        /// <summary>
        /// Gets the current cursor position.
        /// </summary>
        public int Cursor { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the end of stream has been reached.
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                return (this.Cursor >> 3) == this.stream.Length;
            }
        }

        /// <summary>
        /// Reads up to 32 bits from the stream, returning them as a uint.
        /// </summary>
        /// <param name="numBits">
        /// The number of bits to read.
        /// </param>
        /// <returns>
        /// The <see cref="uint"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if numBits is greater than 32.
        /// </exception>
        public uint Read(int numBits)
        {
            if (numBits > 32)
            {
                throw new ArgumentOutOfRangeException("numBits", "Number of bits must be less than 32.");
            }

            uint value = 0;

            while (numBits > 0)
            {
                var bytePos = this.Cursor & 7;
                int bitsLeftInByte = 8 - bytePos;
                if (bytePos == 0)
                {
                    this.currentByte = this.stream.ReadByte();
                }

                var bitsToRead = (bitsLeftInByte > numBits) ? numBits : bitsLeftInByte;

                value = (value << bitsToRead) | ((uint)this.currentByte >> bytePos) & ((1u << bitsToRead) - 1u);
                this.Cursor += bitsToRead;
                numBits -= bitsToRead;
            }

            return value;
        }

        /// <summary>
        /// If in the middle of a byte, moves to the start of the next byte.
        /// </summary>
        public void AlignToByte()
        {
            if ((this.Cursor & 7) > 0)
            {
                this.Cursor = (this.Cursor & 0x7ffffff8) + 8;
            }
        }

        /// <summary>
        /// Reads up to 32 bits from the stream
        /// </summary>
        /// <param name="numBits">Number of bits to read, up to 32.</param>
        /// <returns>Returns a uint containing the number of bits read.</returns>
        public uint Read(uint numBits)
        {
            return this.Read((int)numBits);
        }

        public bool[] ReadBitArray(uint numBits)
        {
            var bitArray = new bool[numBits];
            for (var i = 0; i < bitArray.Length; i++)
                bitArray[i] = ReadBoolean();

            return bitArray;
        }

        /// <summary>
        /// Reads 1 byte from the current stream position.
        /// </summary>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        public byte ReadByte()
        {
            return (byte)this.Read(8);
        }

        /// <summary>
        /// Reads a single bit from the stream as a boolean.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool ReadBoolean()
        {
            return this.Read(1) == 1;
        }

        /// <summary>
        /// Reads 2 bytes from the stream as a short.
        /// </summary>
        /// <returns>
        /// The <see cref="short"/>.
        /// </returns>
        public short ReadInt16()
        {
            return (short)this.Read(16);
        }

        /// <summary>
        /// Reads 4 bytes from the stream as an int.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int ReadInt32()
        {
            return (int)this.Read(32);
        }

        /// <summary>
        /// Reads an array of bytes from the stream.
        /// </summary>
        /// <param name="bytes">
        /// The number of bytes to read.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/> array.
        /// </returns>
        public byte[] ReadBytes(int bytes)
        {
            var buffer = new byte[bytes];
            for (int i = 0; i < bytes; i++)
            {
                buffer[i] = this.ReadByte();
            }

            return buffer;
        }

        /// <summary>
        /// Reads a given number of bytes, parsing them as a UTF8 string.
        /// </summary>
        /// <param name="length">
        /// The string length.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string ReadString(int length)
        {
            var buffer = this.ReadBytes(length);
            return Encoding.UTF8.GetString(buffer);
        }

        public byte[] ReadBlobPrecededWithLength(int numBitsForLength)
        {
            var stringLength = Read(numBitsForLength);
            AlignToByte();
            return ReadBytes((int)stringLength);
        }
    }
}
