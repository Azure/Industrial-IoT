// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.IO
{
    using System.Text;

    /// <summary>
    /// Stream extensions
    /// </summary>
    public static class StreamEx
    {
        /// <summary>
        /// Helper extension to convert an entire stream into a string...
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoder"></param>
        /// <returns></returns>
        public static string ReadAsString(this Stream stream, Encoding encoder)
        {
            // Try to read as much as possible
            var buffer = stream.ReadAsBuffer();
            if (buffer.Array == null)
            {
                return string.Empty;
            }
            return encoder.GetString(buffer.Array, 0, buffer.Count);
        }

        /// <summary>
        /// Helper extension to convert an entire stream into a buffer...
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ArraySegment<byte> ReadAsBuffer(this Stream stream)
        {
            // Try to read as much as possible
            var body = new byte[1024];
            var offset = 0;
            try
            {
                while (true)
                {
                    var read = stream.Read(body, offset, body.Length - offset);
                    if (read <= 0)
                    {
                        break;
                    }

                    offset += read;
                    if (offset == body.Length)
                    {
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
    }
}
