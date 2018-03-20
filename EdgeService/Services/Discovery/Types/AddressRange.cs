// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;

    public class AddressRange {

        /// <summary>
        /// Lowest address in range in host order
        /// </summary>
        public uint Low { get; }

        /// <summary>
        /// Highest address in range in host order
        /// </summary>
        public uint High { get; }

        /// <summary>
        /// Create address range
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public AddressRange(uint low, uint high) {
            Low = _cur = low;
            High = high;

            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(Low != 0);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <summary>
        /// Create address range
        /// </summary>
        /// <param name="address"></param>
        /// <param name="scope"></param>
        public AddressRange(IPAddress address, int scope) {
            var curAddr = (uint)IPAddress.NetworkToHostOrder(
                (int)BitConverter.ToUInt32(
                    address.GetAddressBytes(), 0));
            var mask = 0xffffffff >> scope;
            High = curAddr | ~mask;
            Low = _cur = (curAddr & mask);

            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(Low != 0);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <summary>
        /// Create address range from unicast address uinfo
        /// </summary>
        /// <param name="address"></param>
        /// <param name="localOnly"></param>
        public AddressRange(UnicastIPAddressInformation address,
            bool localOnly = false) {
            var curAddr = (uint)IPAddress.NetworkToHostOrder(
                (int)BitConverter.ToUInt32(
                address.Address.GetAddressBytes(), 0));
            var mask = (uint)IPAddress.NetworkToHostOrder(
                (int)BitConverter.ToUInt32(
                address.IPv4Mask.GetAddressBytes(), 0));

            if (localOnly) {
                // Add local address only
                High = curAddr;
                Low = _cur = curAddr;
            }
            else {
                // Add entire network
                High = curAddr | ~mask;
                Low = _cur = (curAddr & mask);
            }
            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(Low != 0);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (!(obj is AddressRange range)) {
                return false;
            }
            return Low == range.Low && High == range.High;
        }

        public static bool operator ==(AddressRange range1, AddressRange range2) =>
            EqualityComparer<AddressRange>.Default.Equals(range1, range2);
        public static bool operator !=(AddressRange range1, AddressRange range2) =>
            !(range1 == range2);

        /// <summary>
        /// Hash
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = 2082053542;
            hashCode = hashCode * -1521134295 + Low.GetHashCode();
            hashCode = hashCode * -1521134295 + High.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"{ToAddress(Low)}-{ToAddress(High)}";

        /// <summary>
        /// Parses a series of address ranges
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static bool TryParse(string value, out IEnumerable<AddressRange> ranges) {
            ranges = null;
            try {
                ranges = value.Split(new char[] { ';', ',' }, 
                    StringSplitOptions.RemoveEmptyEntries).Select(s => {
                        var x = s.Split('/');
                        if (x.Length != 2) {
                            throw new FormatException("Bad scope format");
                        }
                        return new AddressRange(IPAddress.Parse(x[0]),
                            int.Parse(x[1]));
                    });
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Helper to convert 32 bit host order address to address object
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static IPAddress ToAddress(uint addr) =>
            new IPAddress((uint)IPAddress.HostToNetworkOrder((int)addr));

        /// <summary>
        /// Fills next batch of addresses
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="count"></param>
        public void FillNextBatch(IList<uint> batch, int count) {
            for (var i = 0; _cur <= High && i < count; i++) {
                batch.Add(_cur++);
            }
            batch.Shuffle();
        }
        private uint _cur;
    }
}
