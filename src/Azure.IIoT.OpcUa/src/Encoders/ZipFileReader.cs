// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    /// <summary>
    /// Read zip files
    /// </summary>
    public sealed class ZipFileReader : IDisposable
    {
        /// <summary>
        /// Read zip file
        /// </summary>
        /// <param name="fileName"></param>
        public ZipFileReader(string fileName)
        {
            if (!fileName.EndsWith(ZipFileWriter.FileSuffix,
                StringComparison.InvariantCulture))
            {
                fileName += ZipFileWriter.FileSuffix;
            }
            var stream = new FileStream(fileName, FileMode.Open);
            try
            {
                _zip = new ZipArchive(stream, ZipArchiveMode.Read, false);
            }
            catch (Exception ex)
            {
                stream.Dispose();
                throw new FormatException("Bad zip file", ex);
            }
            try
            {
                _schema = ReadHeader(out _contentType);
                _suffix = ZipFileWriter.Suffix(_contentType);
            }
            catch
            {
                _zip.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Read from stream
        /// </summary>
        /// <param name="stream"></param>
        public ZipFileReader(Stream stream)
        {
            try
            {
                _zip = new ZipArchive(stream, ZipArchiveMode.Read, false);
            }
            catch (Exception ex)
            {
                throw new FormatException("Bad zip file", ex);
            }
            try
            {
                _schema = ReadHeader(out _contentType);
                _suffix = ZipFileWriter.Suffix(_contentType);
            }
            catch
            {
                _zip.Dispose();
                throw;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<T> Stream<T>(Func<string?, Stream, T> reader)
        {
            while (HasMore())
            {
                yield return Read(reader);
            }
        }

        /// <inheritdoc/>
        public T Read<T>(Func<string?, Stream, T> reader)
        {
            var entry = GetEntry();
            if (entry == null)
            {
                throw new EndOfStreamException("No more entries in zip file");
            }
            using var stream = entry.Open();
            var result = reader(_schema, stream);
            _sequenceNumber++;
            return result;
        }

        private ZipArchiveEntry? GetEntry()
        {
            return _zip.GetEntry(_sequenceNumber + _suffix);
        }

        /// <inheritdoc/>
        public bool HasMore()
        {
            return GetEntry() != null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _zip.Dispose();
        }

        /// <summary>
        /// Writes the schema.
        /// </summary>
        /// <param name="contentType"></param>
        /// <exception cref="FormatException"></exception>
        private string? ReadHeader(out ZipFileWriter.ContentType contentType)
        {
            var entry = _zip.GetEntry(ZipFileWriter.ContentTypeFile);
            if (entry == null || entry.Length != 1)
            {
                throw new FormatException("Not a valid zip file with stream data");
            }
            using var contentTypeStream = entry.Open();
            contentType = (ZipFileWriter.ContentType)contentTypeStream.ReadByte();
            switch (contentType)
            {
                case ZipFileWriter.ContentType.Json:
                case ZipFileWriter.ContentType.JsonGzip:
                case ZipFileWriter.ContentType.Binary:
                    break;
                default:
                    throw new FormatException("Content type of zip file is invalid.");
            }
            entry = _zip.GetEntry(ZipFileWriter.MessageSchemaFile);
            if (entry != null)
            {
                using var schemaStream = entry.Open();
                return schemaStream.ReadAsString(Encoding.UTF8);
            }
            return null;
        }

        private int _sequenceNumber = 1;
        private readonly ZipArchive _zip;
        private readonly string? _schema;
        private readonly string _suffix;
        private readonly ZipFileWriter.ContentType _contentType;
    }
}
