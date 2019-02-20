// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Represents a range of ipv4 addresses
    /// </summary>
    public sealed class AddressRange {

        /// <summary>
        /// Name of the Network interface.
        /// </summary>
        public string Nic { get; }

        /// <summary>
        /// Lowest address in range in host order
        /// </summary>
        public uint Low { get; }

        /// <summary>
        /// Highest address in range in host order
        /// </summary>
        public uint High { get; }

        /// <summary>
        /// Number of addresses in range
        /// </summary>
        public int Count => (int)(High - Low) + 1;

        /// <summary>
        /// Create address range
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <param name="nic"></param>
        public AddressRange(uint low, uint high,
            string nic = "custom range") {
            Nic = nic;
            Low = _cur = low > high ? high : low;
            High = high < low ? low : high;
        }

        /// <summary>
        /// Create address range
        /// </summary>
        /// <param name="nic"></param>
        /// <param name="address"></param>
        /// <param name="suffix"></param>
        public AddressRange(IPAddress address, int suffix,
            string nic = "custom range") {
            if (address == null) {
                throw new ArgumentNullException(nameof(address));
            }
            if (suffix > 32) {
                throw new ArgumentException(nameof(suffix));
            }
            var curAddr = (uint)IPAddress.NetworkToHostOrder(
                (int)BitConverter.ToUInt32(
                    address.GetAddressBytes(), 0));
            var mask = 0xffffffff << (32 - suffix);
            High = curAddr | ~mask;
            Low = _cur = curAddr & mask;
            Nic = nic;

            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <summary>
        /// Create address range from unicast address uinfo
        /// </summary>
        /// <param name="itf"></param>
        /// <param name="localOnly"></param>
        /// <param name="suffix"></param>
        public AddressRange(NetInterface itf,
            bool localOnly = false, int? suffix = null) {

            if (itf == null) {
                throw new ArgumentNullException(nameof(itf));
            }

            var curAddr = (uint)new IPv4Address(itf.UnicastAddress);
            var mask = suffix == null ?
                (uint)new IPv4Address(itf.SubnetMask) :
                    0xffffffff << (32 - suffix.Value);

            Nic = itf.Name;
            if (localOnly) {
                // Add local address only
                High = curAddr;
                Low = _cur = curAddr;
            }
            else {
                // Add entire network
                High = curAddr | ~mask;
                Low = _cur = curAddr & mask;
            }
            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is AddressRange range)) {
                return false;
            }
            return Low == range.Low && High == range.High;
        }

        /// <inheritdoc/>
        public static bool operator ==(AddressRange range1, AddressRange range2) =>
            EqualityComparer<AddressRange>.Default.Equals(range1, range2);
        /// <inheritdoc/>
        public static bool operator !=(AddressRange range1, AddressRange range2) =>
            !(range1 == range2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 2082053542;
            hashCode = hashCode * -1521134295 + Low.GetHashCode();
            hashCode = hashCode * -1521134295 + High.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString() =>
            $"{(IPv4Address)Low}-{(IPv4Address)High} [{Nic}]";

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public AddressRange Copy() => new AddressRange(Low, High, Nic);

        /// <summary>
        /// Parses a series of address ranges
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static bool TryParse(string value,
            out IEnumerable<AddressRange> ranges) {
            try {
                ranges = Parse(value);
                return true;
            }
            catch {
                ranges = null;
                return false;
            }
        }

        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IEnumerable<AddressRange> Parse(string value) {
            if (string.IsNullOrEmpty(value)) {
                throw new ArgumentNullException(nameof(value));
            }
            var split = value.Split(new char[] { ';', ',' },
                StringSplitOptions.RemoveEmptyEntries);
            return split
                .SelectMany(s => {
                    var x = s.Split('/');
                    if (x.Length != 2) {
                        throw new FormatException("Bad suffix format");
                    }
                    var suffix = int.Parse(x[1]);
                    if (suffix == 0 || suffix > 32) {
                        throw new FormatException("Bad suffix value");
                    }
                    if (x[0] == "*") {
                        return NetworkInformationEx.GetAllNetInterfaces(
                            NetworkClass.Wired)
                        .Select(t => new AddressRange(t, false, suffix));
                    }
                    return new AddressRange(IPAddress.Parse(x[0]), suffix)
                        .YieldReturn();
                }).Distinct().ToList();
        }

        /// <summary>
        /// Fills next batch of addresses
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="count"></param>
        public void FillNextBatch(IList<uint> batch, int count) {
            for (var i = 0; _cur <= High && i < count; i++) {
                batch.Add(_cur++);
            }
        }

        /// <summary>
        /// Reset range
        /// </summary>
        public void Reset() => _cur = 0;
        private uint _cur;
    }
}
