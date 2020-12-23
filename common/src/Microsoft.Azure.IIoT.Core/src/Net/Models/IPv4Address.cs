// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Models {
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// IPV4 address
    /// </summary>
    public sealed class IPv4Address : IPAddress, IComparable<IPv4Address>,
        IComparable<IPAddress> {

        /// <summary>
        /// Create address
        /// </summary>
        /// <param name="address"></param>
        private IPv4Address(uint address) :
            base(address) {
        }

        /// <summary>
        /// Create address
        /// </summary>
        /// <param name="address"></param>
        public IPv4Address(IPAddress address) :
            this(address?.GetAddressBytes()) {
        }

        /// <summary>
        /// Create address
        /// </summary>
        /// <param name="address"></param>
        public IPv4Address(byte[] address) : base(address) {
            if (address.Length != 4) {
                throw new ArgumentException(nameof(address.Length));
            }
        }

        /// <summary>
        /// Convert to and from uint
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator uint(IPv4Address value) => (uint)
            NetworkToHostOrder((int)BitConverter.ToUInt32(value.GetAddressBytes(), 0));

        /// <summary>
        /// Convert back to address from uint
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator IPv4Address(uint value) =>
            new IPv4Address((uint)HostToNetworkOrder((int)value));

        /// <summary>
        /// Convert from long
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator long(IPv4Address value) =>
            NetworkToHostOrder((int)BitConverter.ToUInt32(value.GetAddressBytes(), 0));

        /// <summary>
        /// Convert back to address from long
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator IPv4Address(long value) =>
            new IPv4Address((uint)HostToNetworkOrder((int)value));

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="value"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IPv4Address operator +(IPv4Address value, int x) =>
            new IPv4Address((uint)((uint)value + x));

        /// <summary>
        /// Subtract
        /// </summary>
        /// <param name="value"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IPv4Address operator -(IPv4Address value, int x) =>
            new IPv4Address((uint)((uint)value - x));

        /// <summary>
        /// Increment by 1
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IPv4Address operator ++(IPv4Address value) => value + 1;

        /// <summary>
        /// Decrement by 1
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IPv4Address operator --(IPv4Address value) => value - 1;

        /// <inheritdoc/>
        public static bool operator ==(IPv4Address addr1, IPv4Address addr2) =>
            EqualityComparer<IPv4Address>.Default.Equals(addr1, addr2);

        /// <inheritdoc/>
        public static bool operator !=(IPv4Address addr1, IPv4Address addr2) =>
            !(addr1 == addr2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object comparand) {
            return base.Equals(comparand);
        }

        /// <inheritdoc/>
        public int CompareTo(IPv4Address other) {
            return (int)(this - other);
        }

        /// <inheritdoc/>
        public int CompareTo(IPAddress other) {
            return CompareTo(other.AsV4());
        }

        /// <inheritdoc/>
        public int Compare(IPv4Address x, IPv4Address y) {
            return x.CompareTo(y);
        }
    }
}
