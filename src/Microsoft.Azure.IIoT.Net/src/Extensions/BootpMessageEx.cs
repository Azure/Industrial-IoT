// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Bootp {
    using Microsoft.Azure.IIoT.Net;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;

    public static class BootpMessageEx {

        /// <summary>
        /// Read address
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IPAddress ReadAddress(this byte[] data, ref int offset,
            int? length = null) {
            var result = new IPAddress(data.Read(ref offset, 4, length));
            // Ipv4 = 4 bytes
            if (IPAddress.Any.Equals(result)) {
                return null;
            }
            return result;
        }

        /// <summary>
        /// Read hardware address 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        internal static PhysicalAddress ReadPhysical(this byte[] data,
            ref int offset, int size, int? length = null) {
            var result = new PhysicalAddress(data.Read(ref offset, size, length));
            if (PhysicalAddress.None.Equals(result)) {
                return null;
            }
            return result;
        }

        /// <summary>
        /// Convert from ushort to timespan using network byte order
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static TimeSpan ReadUInt16Seconds(this byte[] data,
            ref int offset) {
            return TimeSpan.FromSeconds(data.ReadUInt16(ref offset, true));
        }

        /// <summary>
        /// Write ushort seconds from timespan in network byte order
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        internal static void WriteUInt16Seconds(this Stream stream, 
            TimeSpan value, int? length = null) {
            stream.Write((ushort)value.TotalSeconds, true);
            var fill = length ?? 0;
            if (fill > 2) {
                stream.Fill(0, fill - 2);
            }
        }

        /// <summary>
        /// Write address
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        public static void Write(this Stream stream, IPAddress address,
            int? length = null) {
            if (address == null) {
                address = IPAddress.Any;
            }
            stream.Write(address?.GetAddressBytes(), length ?? 4); // Min 4
        }

        /// <summary>
        /// Write hardware address as bootp field which starts with 
        /// the hardware type byte and then is followed by 1-15 byte
        /// address.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="address"></param>
        /// <param name="length"></param>
        internal static void Write(this Stream stream, PhysicalAddress address,
            int? length = null) {
            if (address == null) {
                address = PhysicalAddress.None;
            }
            stream.Write(address.GetAddressBytes(), length);
        }
    }
}
