// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Encodes objects in a stream using Avro binary encoding.
    /// </summary>
    public sealed class AvroBinaryWriter : IDisposable
    {
        /// <summary>
        /// Underlying stream
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Creates a writer that writes avro binary to
        /// the stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="leaveOpen">If the stream should
        /// be left open on dispose.</param>
        public AvroBinaryWriter(Stream stream, bool leaveOpen = true)
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
        public void WriteBoolean(bool value)
        {
            Stream.WriteByte((byte)(value ? 0x1 : 0x0));
        }

        /// <inheritdoc/>
        public void WriteInteger(int value)
        {
            var encoded = (uint)((value << 1) ^ (value >> 31));
            do
            {
                var current = encoded & 0x7FU;
                encoded >>= 7;

                if (encoded != 0)
                {
                    current |= 0x80U;
                }

                Stream.WriteByte((byte)current);
            }
            while (encoded != 0U);
        }

        /// <inheritdoc/>
        public void WriteInteger(long value)
        {
            var encoded = (ulong)((value << 1) ^ (value >> 63));
            do
            {
                var current = encoded & 0x7FUL;
                encoded >>= 7;

                if (encoded != 0)
                {
                    current |= 0x80UL;
                }

                Stream.WriteByte((byte)current);
            }
            while (encoded != 0UL);
        }

        /// <inheritdoc/>
        public void WriteFloat(float value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(float)];
            BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
            WriteFixed(bytes);
        }

        /// <inheritdoc/>
        public void WriteDouble(double value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(double)];
            BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
            WriteFixed(bytes);
        }

        /// <inheritdoc/>
        public void WriteFixed(ReadOnlySpan<byte> buffer)
        {
            Stream.Write(buffer);
        }

        /// <inheritdoc/>
        internal void WriteBytes(ReadOnlySpan<byte> value)
        {
            WriteInteger(value.Length);
            WriteFixed(value);
        }

        /// <inheritdoc/>
        public void WriteString(string value)
        {
            WriteBytes(Encoding.UTF8.GetBytes(value));
        }

        /// <inheritdoc/>
        public bool WriteNullable<T>([NotNullWhen(false)] T? o) where T : class
        {
            WriteInteger(o == null ? 0 : 1); // Union index, first is "null"
            return o == null;
        }

        /// <inheritdoc/>
        public void WriteArray<T>(IList<T> values, Action<T> writer)
        {
            WriteInteger(values.Count);

            // write contents.
            foreach (var value in values)
            {
                writer(value);
            }
        }

        private readonly bool _leaveOpen;
    }
}
