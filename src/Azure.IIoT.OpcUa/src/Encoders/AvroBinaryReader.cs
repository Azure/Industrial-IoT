// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Reads from a Avro Binary encoded stream.
    /// </summary>
    public sealed class AvroBinaryReader : IDisposable
    {
        private readonly bool _leaveOpen;

        /// <summary>
        /// Underlying stream
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Max string
        /// </summary>
        public int MaxStringLength { get; set; } = 64 * 1024;

        /// <summary>
        /// Max bytes
        /// </summary>
        public int MaxBytesLength { get; set; } = 64 * 1024;

        /// <summary>
        /// Create avro reader
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="leaveOpen"></param>
        public AvroBinaryReader(Stream stream, bool leaveOpen = false)
        {
            Stream = stream;
            _leaveOpen = leaveOpen;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_leaveOpen)
            {
                Stream.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool ReadBoolean()
        {
            var b = ReadByte();
            switch (b)
            {
                case 0:
                    return false;
                case 1:
                    return true;
                default:
                    throw new DecodingException(
                        "Not a boolean value in the stream: " + b);
            }
        }

        /// <inheritdoc/>
        public long ReadInteger()
        {
            var b = ReadByte();
            var n = b & 0x7FUL;
            var shift = 7;
            while ((b & 0x80) != 0)
            {
                b = ReadByte();
                n |= (b & 0x7FUL) << shift;
                shift += 7;
            }
            var value = (long)n;
            return (-(value & 0x01L))
                ^ ((value >> 1) & 0x7fffffffffffffffL);
        }

        /// <inheritdoc/>
        public void ReadFixed(Span<byte> buffer)
        {
            while (!buffer.IsEmpty)
            {
                var n = Stream.Read(buffer);
                if (n <= 0)
                {
                    throw new DecodingException(StatusCodes.BadEndOfStream,
                        "End of stream reached.");
                }
                buffer = buffer[n..];
            }
        }

        /// <inheritdoc/>
        public float ReadFloat()
        {
            Span<byte> bytes = stackalloc byte[sizeof(float)];
            ReadFixed(bytes);
            return BinaryPrimitives.ReadSingleLittleEndian(bytes);
        }

        /// <inheritdoc/>
        public double ReadDouble()
        {
            Span<byte> bytes = stackalloc byte[sizeof(double)];
            ReadFixed(bytes);
            return BinaryPrimitives.ReadDoubleLittleEndian(bytes);
        }

        /// <inheritdoc/>
        public byte[] ReadBytes()
        {
            var length = ReadInteger();
            if (length == 0)
            {
                return [];
            }
            if (MaxBytesLength > 0 && MaxBytesLength < length)
            {
                throw new DecodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxByteStringLength {MaxBytesLength} < {length}.");
            }

            var buffer = new byte[(int)length];
            ReadFixed(buffer.AsSpan());
            return buffer;
        }

        /// <inheritdoc/>
        public string ReadString()
        {
            var length = ReadInteger();
            if (length == 0)
            {
                return string.Empty;
            }

            if (MaxStringLength > 0 && MaxStringLength < length)
            {
                throw new DecodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxStringLength {MaxStringLength} < {length}.");
            }

            if (length <= 256)
            {
                Span<byte> buffer = stackalloc byte[(int)length];
                ReadFixed(buffer);
                return GetString(buffer);
            }
            else if (length <= 4096)
            {
                var bufferArray = ArrayPool<byte>.Shared.Rent((int)length);
                try
                {
                    var buffer = bufferArray.AsSpan(0, (int)length);
                    ReadFixed(buffer);
                    return GetString(buffer);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferArray);
                }
            }
            else
            {
                using var binaryReader = new BinaryReader(Stream,
                    Encoding.UTF8, true);
                var bytes = binaryReader.ReadBytes((int)length);
                if (bytes.Length != length)
                {
                    throw new DecodingException(
                        "Could not read as many bytes from stream as expected!");
                }
                return GetString(bytes);
            }

            static string GetString(ReadOnlySpan<byte> bytes)
            {
                if (bytes[^1] == 0)
                {
                    // If 0 terminated, decrease length by one
                    // before converting to string
                    bytes = bytes[..^1];
                }
                return Encoding.UTF8.GetString(bytes);
            }
        }

        private byte ReadByte()
        {
            var n = Stream.ReadByte();
            if (n >= 0)
            {
                return (byte)n;
            }
            throw new DecodingException(StatusCodes.BadEndOfStream,
                "Stream reached its end.");
        }
    }
}
