// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.IO {
    using System.IO.Compression;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Stream extensions
    /// </summary>
    public static class StreamEx {

        /// <summary>
        /// Zip string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] Zip(this Stream input) {
            using (var result = new MemoryStream()) {
                using (var gs = new GZipStream(result, CompressionMode.Compress)) {
                    input.CopyTo(gs);
                }
                return result.ToArray();
            }
        }

        /// <summary>
        /// Unzip byte array
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] Unzip(this Stream input) {
            using (var output = new MemoryStream()) {
                using (var gs = new GZipStream(input, CompressionMode.Decompress)) {
                    gs.CopyTo(output);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// Load type from xml stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(this Stream stream) {
            var reader = new StreamReader(stream);
            try {
                var serializer = new Xml.Serialization.XmlSerializer(typeof(T));
                var xmlReader = new XmlTextReader(reader) {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };
                return (T)serializer.Deserialize(xmlReader);
            }
            finally {
                reader.Close();
            }
        }

        /// <summary>
        /// Helper extension to convert an entire stream into a string...
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoder"></param>
        /// <returns></returns>
        public static string ReadAsString(this Stream stream, Encoding encoder) {
            // Try to read as much as possible
            var buffer = stream.ReadAsBuffer();
            return encoder.GetString(buffer.Array, 0, buffer.Count);
        }

        /// <summary>
        /// Helper extension to convert an entire stream into a buffer...
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ArraySegment<byte> ReadAsBuffer(this Stream stream) {
            // Try to read as much as possible
            var body = new byte[1024];
            var offset = 0;
            try {
                while (true) {
                    var read = stream.Read(body, offset, body.Length - offset);
                    if (read <= 0) {
                        break;
                    }

                    offset += read;
                    if (offset == body.Length) {
                        // Grow
                        var newbuf = new byte[body.Length * 2];
                        Buffer.BlockCopy(body, 0, newbuf, 0, body.Length);
                        body = newbuf;
                    }
                }
            }
            catch (IOException) { }
            return new ArraySegment<byte>(body, 0, offset);
        }

        /// <summary>
        /// Helper extension to convert an entire stream into readonly memory async.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static async Task<ReadOnlyMemory<byte>> ReadAsMemoryAsync(this Stream stream,
            int bufferSize = 1024) {
            // Try to read as much as possible
            var body = new byte[bufferSize];
            var offset = 0;
            try {
                while (true) {
                    var read = await stream.ReadAsync(body, offset, body.Length - offset);
                    if (read <= 0) {
                        break;
                    }

                    offset += read;
                    if (offset == body.Length) {
                        // Grow
                        var newbuf = new byte[body.Length * 2];
                        Buffer.BlockCopy(body, 0, newbuf, 0, body.Length);
                        body = newbuf;
                    }
                }
            }
            catch (IOException) { }
            return body;
        }

        /// <summary>
        /// Write remaining buffer from offset
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static void Write(this Stream stream, byte[] buffer, int offset) {
            stream.Write(buffer, offset, buffer.Length - offset);
        }

        /// <summary>
        /// Write entire buffer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static void Write(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Write remaining buffer from offset
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset) {
            return stream.WriteAsync(buffer, offset, buffer.Length - offset);
        }

        /// <summary>
        /// Write entire buffer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Task WriteAsync(this Stream stream, byte[] buffer) {
            return stream.WriteAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Write remaining buffer from offset
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task WriteAsync(this Stream stream, byte[] buffer, int offset,
            CancellationToken ct) {
            return stream.WriteAsync(buffer, offset, buffer.Length - offset, ct);
        }

        /// <summary>
        /// Write entire buffer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task WriteAsync(this Stream stream, byte[] buffer,
            CancellationToken ct) {
            return stream.WriteAsync(buffer, 0, buffer.Length, ct);
        }

        /// <summary>
        /// Read remaining buffer from offset
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int Read(this Stream stream, byte[] buffer, int offset) {
            return stream.Read(buffer, offset, buffer.Length - offset);
        }

        /// <summary>
        /// Read entire buffer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int Read(this Stream stream, byte[] buffer) {
            return stream.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Read remaining buffer from offset
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset) {
            return stream.ReadAsync(buffer, offset, buffer.Length - offset);
        }

        /// <summary>
        /// Read entire buffer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer) {
            return stream.ReadAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Read remaining buffer from offset
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer, int offset,
            CancellationToken ct) {
            return stream.ReadAsync(buffer, offset, buffer.Length - offset, ct);
        }

        /// <summary>
        /// Read entire buffer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<int> ReadAsync(this Stream stream, byte[] buffer,
            CancellationToken ct) {
            return stream.ReadAsync(buffer, 0, buffer.Length, ct);
        }
    }
}
