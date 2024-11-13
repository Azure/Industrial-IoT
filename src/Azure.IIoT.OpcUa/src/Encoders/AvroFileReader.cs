// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro;
    using Avro.File;
    using Avro.IO;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Read avro files
    /// </summary>
    public sealed class AvroFileReader : IDisposable
    {
        /// <summary>
        /// Read avro file
        /// </summary>
        /// <param name="fileName"></param>
        public AvroFileReader(string fileName)
        {
            if (!fileName.EndsWith(".avro", StringComparison.InvariantCulture))
            {
                fileName += ".avro";
            }

            _leaveOpen = false;
            fileName = fileName.SanitizeFileName();
            _stream = new FileStream(fileName, FileMode.Open);
            try
            {
                _decoder = new BinaryDecoder(_stream);
                _codec = ReadHeader();
            }
            catch
            {
                _stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Read from stream
        /// </summary>
        /// <param name="stream"></param>
        public AvroFileReader(Stream stream)
        {
            _leaveOpen = true;
            _stream = stream;
            try
            {
                _decoder = new BinaryDecoder(_stream);
                _codec = ReadHeader();
            }
            catch
            {
                _stream.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<T> Stream<T>(Func<Schema, Stream, T> reader)
        {
            while (HasMore())
            {
                yield return Read(reader);
            }
        }

        /// <inheritdoc/>
        public T Read<T>(Func<Schema, Stream, T> reader)
        {
            if (!HasMore() || _currentBlockStream == null)
            {
                throw new EndOfStreamException("No more objects remaining!");
            }

            var result = reader(_header.Schema, _currentBlockStream);
            _blockRemaining--;
            if (_blockRemaining == 0)
            {
                _currentBlockStream.Dispose();
                _currentBlockStream = null;
            }
            return result;
        }

        /// <inheritdoc/>
        public bool HasMore()
        {
            return _blockRemaining != 0 || TryMoveToNextBlock();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _currentBlockStream?.Dispose();
            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }

        /// <summary>
        /// Try moving to next block
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private bool TryMoveToNextBlock()
        {
            // check to ensure still data to read
            var currentPosition = _stream.Position;
            if (_stream.ReadByte() == -1)
            {
                return false;
            }
            _stream.Position = currentPosition;

            // read block count
            _blockRemaining = _decoder.ReadLong();
            // read block size
            _blockSize = _decoder.ReadLong();
            if (_blockSize > int.MaxValue || _blockSize < 0)
            {
                throw new FormatException(
                    $"Block size {_blockSize} invalid or too large.");
            }

            // Read and decompress block
            var buffer = new byte[_blockSize];
            _decoder.ReadFixed(buffer, 0, buffer.Length);
            _currentBlockStream = AvroFileWriter.Streams.GetStream(
                _codec.Decompress(buffer, (int)_blockSize));

            // Read sync marker
            var marker = new byte[16];
            _decoder.ReadFixed(marker);
            if (!marker.SequenceEqual(_header.SyncData))
            {
                throw new FormatException("Invalid sync marker found!");
            }
            return true;
        }

        /// <summary>
        /// Read header
        /// </summary>
        /// <returns></returns>
        private Codec ReadHeader()
        {
            ReadMagicBytes();
            ReadMetaData();
            _decoder.ReadFixed(_header.SyncData);

            var schema = GetString(_header, DataFileConstants.MetaDataSchema);
            _header.Schema = Schema.Parse(schema);

            var codec = GetString(_header, DataFileConstants.MetaDataCodec);
            return codec == null ? Codec.CreateCodec(Codec.Type.Null) :
                Codec.CreateCodecFromString(codec);
        }

        /// <summary>
        /// Read magic
        /// </summary>
        /// <exception cref="FormatException"></exception>
        private void ReadMagicBytes()
        {
            var firstBytes = new byte[DataFileConstants.Magic.Length];
            try
            {
                _decoder.ReadFixed(firstBytes);
            }
            catch (Exception e)
            {
                throw new FormatException("Not a valid data file!", e);
            }
            if (!firstBytes.SequenceEqual(DataFileConstants.Magic))
            {
                throw new FormatException("Not a valid data file!");
            }
        }

        /// <summary>
        /// Read metadata
        /// </summary>
        private void ReadMetaData()
        {
            // read meta data
            var len = _decoder.ReadMapStart();
            if (len > 0)
            {
                do
                {
                    for (long i = 0; i < len; i++)
                    {
                        var key = _decoder.ReadString();
                        var val = _decoder.ReadBytes();
                        _header.MetaData.Add(key, val);
                    }
                }
                while ((len = _decoder.ReadMapNext()) != 0);
            }
        }

        /// <inheritdoc/>
        private static string? GetString(Header header, string key)
        {
            if (!header.MetaData.TryGetValue(key, out var value))
            {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(value);
        }

        private readonly Codec _codec;
        private readonly bool _leaveOpen;
        private readonly Stream _stream;
        private readonly Header _header = new();
        private Stream? _currentBlockStream;
        private long _blockRemaining;
        private long _blockSize;
        private readonly BinaryDecoder _decoder;
    }
}
