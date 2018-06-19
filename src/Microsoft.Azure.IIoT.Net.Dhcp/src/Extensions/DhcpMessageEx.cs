// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using Microsoft.Azure.IIoT.Net.Bootp;
    using Microsoft.Azure.IIoT.Net;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    internal static class DhcpMessageEx {

        /// <summary>
        /// Read v4 option
        /// </summary>
        /// <param name="data"></param>
        /// <param name="option"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal static byte[] ReadOption(this byte[] data, out v4.DhcpOption option,
            ref int offset) {
            option = (v4.DhcpOption)data.ReadUInt8(ref offset);
            if (option == v4.DhcpOption.Pad) {
                // Skip
                offset += data.ReadUInt8(ref offset);
                return null;
            }
            if (option == v4.DhcpOption.End) {
                // Done
                return null;
            }
            var optionLen = data.ReadUInt8(ref offset);
            if (optionLen == 0) {
                return new byte[0];
            }
            return data.Read(ref offset, optionLen);
        }

        /// <summary>
        /// Write dhcp v4 option
        /// </summary>
        /// <param name="data"></param>
        /// <param name="option"></param>
        /// <param name="value"></param>
        internal static void Write(this Stream data, v4.DhcpOption option,
            byte[] value, int? length = null) {
            data.Write((byte)option);
            var len = value?.Length ?? 0;
            if (length.HasValue && length.Value > len) {
                len = length.Value;
            }
            if (option != v4.DhcpOption.End &&
                option != v4.DhcpOption.Pad) {
                // As per RFC 1497 any other option has variable data
                data.Write((byte)len);
            }
            if (len > 0) {
                data.Write(value, len);
            }
        }

        /// <summary>
        /// Convert from data to timespan
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static TimeSpan? ToSecondsDuration(this byte[] data,
            bool netOrder = false, int offset = 0) {
            var value = data;
            if (value != null) {
                return TimeSpan.FromSeconds(value.ToUInt32(netOrder, offset));
            }
            return null;
        }

        /// <summary>
        /// Convert bytes to list of addresses
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static List<IPAddress> ToIPAddresses(this byte[] data) {
            var list = new List<IPAddress>();
            if (data != null) {
                if ((data.Length % 4) != 0) {
                    throw new FormatException("Bad length of addresses");
                }
                var buffer = new byte[4];
                for (var offset = 0; offset < data.Length; offset += 4) {
                    Array.Copy(data, offset, buffer, 0, buffer.Length);
                    list.Add(buffer.ToIPAddress());
                }
            }
            return list;
        }

        /// <summary>
        /// Convert to address
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        internal static IPAddress ToIPAddress(this byte[] data,
            IPAddress fallback = null) {
            var address = fallback;
            if (data != null) {
                if (data.Length != 4 && data.Length != 16) {
                    throw new FormatException("Bad length of address");
                }
                address = new IPAddress(data);
            }
            if (IPAddress.Any.Equals(address)) {
                return null;
            }
            return address;
        }

        /// <summary>
        /// Convert address to bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static byte[] ToBytes(this IPAddress value) {
            if (value != null && !IPAddress.Any.Equals(value)) {
                return value.GetAddressBytes();
            }
            return null;
        }

        /// <summary>
        /// Convert list of addresses to bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static byte[] ToBytes(this IEnumerable<IPAddress> value) {
            if (value != null) {
                using (var stream = new MemoryStream()) {
                    foreach (var address in value) {
                        stream.Write(address);
                    }
                    return stream.ToArray();
                }
            }
            return null;
        }

        /// <summary>
        /// Convert timespan to buffer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static byte[] ToBytes(this TimeSpan? value, bool netOrder = false) {
            if (value.HasValue) {
                return value.Value.ToBytes(netOrder);
            }
            return null;
        }

        /// <summary>
        /// Convert timespan to buffer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static byte[] ToBytes(this TimeSpan value, bool netOrder = false) {
            return ((uint)value.TotalSeconds).ToBytes(netOrder);
        }
    }
}
