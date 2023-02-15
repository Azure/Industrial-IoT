// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Buffer extensions
    /// </summary>
    public static class ByteBufferEx {

        /// <summary>
        /// Create subset
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] Subset(this byte[] buffer, int offset, int length) {
            var buf = new byte[length];
            Array.Copy(buffer, offset, buf, 0, length);
            return buf;
        }

        /// <summary>
        /// Read uint 8
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static byte ReadUInt8(this byte[] data, ref int offset) {
            return data[offset++];
        }

        /// <summary>
        /// Read uint 16
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="netOrder"></param>
        /// <returns></returns>
        public static ushort ReadUInt16(this byte[] data, ref int offset,
            bool netOrder = false) {
            var value = data.ToUInt16(netOrder, offset);
            offset += 2;
            return value;
        }

        /// <summary>
        /// Read uint 32
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="netOrder"></param>
        /// <returns></returns>
        public static uint ReadUInt32(this byte[] data, ref int offset,
            bool netOrder = false) {
            var value = data.ToUInt32(netOrder, offset);
            offset += 4;
            return value;
        }

        /// <summary>
        /// Read buffer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="copy"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] ReadByteArray(this byte[] data, ref int offset, int copy,
            int? length = null) {

            var padding = (length ?? copy) - copy;
            if (padding < 0) {
                throw new ArgumentException(nameof(copy));
            }

            var buf = data.Subset(offset, copy);
            offset += copy;

            if (padding > 0) {
                offset += padding;
            }
            return buf;
        }

        /// <summary>
        /// Read uint 32
        /// </summary>
        /// <param name="data"></param>
        /// <param name="netOrder"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static uint ToUInt32(this byte[] data, bool netOrder = false,
            int offset = 0) {
            if (data == null) {
                return 0;
            }
            var value = BitConverter.ToUInt32(data, offset);
            if (netOrder) {
                value = (uint)IPAddress.NetworkToHostOrder((int)value);
            }
            return value;
        }

        /// <summary>
        /// Read uint 16
        /// </summary>
        /// <param name="data"></param>
        /// <param name="netOrder"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static ushort ToUInt16(this byte[] data, bool netOrder = false,
            int offset = 0) {
            if (data == null) {
                return 0;
            }
            var value = BitConverter.ToUInt16(data, offset);
            if (netOrder) {
                value = (ushort)IPAddress.NetworkToHostOrder((short)value);
            }
            return value;
        }

        /// <summary>
        /// Convert to buffer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string ToEncodedString(this byte[] data,
            Encoding encoding = null, int offset = 0) {
            if (data == null) {
                return string.Empty;
            }
            try {
                var str = (encoding ?? Encoding.ASCII).GetString(data, offset,
                    data.Length - offset);
                return str.TrimEnd('\0');
            }
            catch {
                return string.Empty;
            }
        }

        /// <summary>
        /// Convert uint to buffer
        /// </summary>
        /// <param name="value"></param>
        /// <param name="netOrder"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this uint value, bool netOrder = false) {
            if (netOrder) {
                value = (uint)IPAddress.HostToNetworkOrder((int)value);
            }
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert ushort to buffer
        /// </summary>
        /// <param name="value"></param>
        /// <param name="netOrder"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this ushort value, bool netOrder = false) {
            if (netOrder) {
                value = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert string to buffer
        /// </summary>
        /// <param name="value"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this string value, Encoding encoding = null) {
            if (string.IsNullOrEmpty(value)) {
                return null;
            }
            return (encoding ?? Encoding.ASCII).GetBytes(value);
        }
    }
}
