// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Represents a range of ipv4 addresses
    /// </summary>
    public sealed class AddressRange
    {
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
            string? nic = null)
        {
            Nic = string.IsNullOrEmpty(nic) ? kNullNicName : nic;
            Low = _cur = low > high ? high : low;
            High = high < low ? low : high;
        }

        /// <summary>
        /// Create address range
        /// </summary>
        /// <param name="address"></param>
        /// <param name="suffix"></param>
        /// <param name="nic"></param>
        public AddressRange(IPAddress address, int suffix,
            string? nic = null)
        {
            ArgumentNullException.ThrowIfNull(address);
            if (suffix > 32)
            {
                throw new ArgumentException("Suffix too large", nameof(suffix));
            }
            var curAddr = (uint)IPAddress.NetworkToHostOrder(
                (int)BitConverter.ToUInt32(
                    address.GetAddressBytes(), 0));
            var mask = 0xffffffff << (32 - suffix);
            High = curAddr | ~mask;
            Low = _cur = curAddr & mask;
            Nic = string.IsNullOrEmpty(nic) ? kNullNicName : nic;

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
            bool localOnly = false, int? suffix = null)
        {
            ArgumentNullException.ThrowIfNull(itf);
            var curAddr = (uint)new IPv4Address(itf.UnicastAddress);
            Nic = string.IsNullOrEmpty(itf.Name) ? kNullNicName : itf.Name;
            if (localOnly)
            {
                // Add local address only
                High = curAddr;
                Low = _cur = curAddr;
            }
            else
            {
                var mask = suffix == null ?
                    (uint)new IPv4Address(itf.SubnetMask) :
                        0xffffffff << (32 - suffix.Value);

                High = curAddr | ~mask;
                Low = _cur = curAddr & mask;
            }
            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <summary>
        /// Create address range from address and subnet mask
        /// </summary>
        /// <param name="address"></param>
        /// <param name="subnet"></param>
        /// <param name="suffix"></param>
        /// <param name="nic"></param>
        public AddressRange(IPAddress address, IPAddress subnet,
            int? suffix = null, string? nic = null)
        {
            ArgumentNullException.ThrowIfNull(address);
            ArgumentNullException.ThrowIfNull(subnet);

            var mask = suffix == null ?
                (uint)new IPv4Address(subnet) :
                    0xffffffff << (32 - suffix.Value);

            var curAddr = new IPv4Address(address);
            Nic = string.IsNullOrEmpty(nic) ? kNullNicName : nic;
            High = curAddr | ~mask;
            Low = _cur = curAddr & mask;

            System.Diagnostics.Debug.Assert(Low <= High);
            System.Diagnostics.Debug.Assert(High != 0);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not AddressRange range)
            {
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
        public override int GetHashCode()
        {
            return HashCode.Combine(Low, High);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public AddressRange Copy()
        {
            return new AddressRange(Low, High, Nic);
        }

        /// <summary>
        /// Parses a series of address ranges
        /// </summary>
        /// <param name="value"></param>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static bool TryParse(string value,
            [NotNullWhen(true)] out IEnumerable<AddressRange>? ranges)
        {
            try
            {
                ranges = Parse(value);
                return true;
            }
            catch
            {
                ranges = null;
                return false;
            }
        }

        /// <summary>
        /// Format a series of address ranges
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        public static string Format(IEnumerable<AddressRange> ranges)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var range in Merge(ranges))
            {
                if (!first)
                {
                    sb.Append(';');
                }
                first = false;
                range.AppendTo(sb);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<AddressRange> Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            var split = value.Split([';', ','],
                StringSplitOptions.RemoveEmptyEntries);
            var unmerged = split
                .SelectMany(s =>
                {
                    var nic = string.Empty;
                    var x = s.Split('[', StringSplitOptions.RemoveEmptyEntries);
                    if (x.Length > 1)
                    {
                        var postFix = x[1].Split(']');
                        if (postFix.Length > 1)
                        {
                            nic = postFix[0];
                        }
                        s = x[0].Trim();
                    }
                    x = s.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (x.Length != 2)
                    {
                        x = s.Split('-', StringSplitOptions.RemoveEmptyEntries);
                        if (x.Length != 2)
                        {
                            throw new FormatException("Bad suffix format");
                        }
                        // Combine into cidr ranges and parse so we get distinct ranges
                        return Parse(new AddressRange(
                            new IPv4Address(IPAddress.Parse(x[0])),
                            new IPv4Address(IPAddress.Parse(x[1])), nic).ToString());
                    }
                    var suffix = int.Parse(x[1], CultureInfo.InvariantCulture);
                    if (suffix == 0 || suffix > 32)
                    {
                        throw new FormatException("Bad suffix value");
                    }
                    if (x[0] == "*")
                    {
                        return NetworkInformationEx.GetAllNetInterfaces(
                            NetworkClass.Wired)
                        .Select(t => new AddressRange(t, false, suffix));
                    }
                    return new AddressRange(IPAddress.Parse(x[0]), suffix, nic)
                        .YieldReturn();
                })
                .Distinct()
                .ToList();
            return Merge(unmerged);
        }

        /// <summary>
        /// Fills next batch of addresses
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="count"></param>
        public void FillNextBatch(IList<uint> batch, int count)
        {
            for (var i = 0; _cur <= High && i < count; i++)
            {
                batch.Add(_cur++);
            }
        }

        /// <summary>
        /// Reset range
        /// </summary>
        public void Reset()
        {
            _cur = Low;
        }

        /// <summary>
        /// Tests contains address
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(IPv4Address value)
        {
            return value >= Low && value <= High;
        }

        /// <summary>
        /// Whether it overlaps with another address range
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Overlaps(AddressRange other)
        {
            return
                Contains(other.Low) ||
                Contains(other.High) ||
                other.Contains(Low) ||
                other.Contains(High);
        }

        /// <summary>
        /// Merge overlapping ranges
        /// </summary>
        /// <param name="ranges"></param>
        /// <returns></returns>
        private static IEnumerable<AddressRange> Merge(IEnumerable<AddressRange> ranges)
        {
            var results = new Stack<AddressRange>();
            if (ranges != null)
            {
                foreach (var range in ranges.OrderBy(k => k.Low))
                {
                    if (results.Count == 0)
                    {
                        results.Push(range);
                    }
                    else
                    {
                        var top = results.Peek();
                        if (top.Overlaps(range))
                        {
                            var nic = (top.Nic + range.Nic)
                                .Replace("localhost", "", StringComparison.InvariantCulture)
                                .Replace(kNullNicName, "", StringComparison.InvariantCulture);
                            var union = new AddressRange(
                                top.Low < range.Low ? top.Low : range.Low,
                                top.High > range.High ? top.High : range.High, nic);
                            results.Pop();
                            results.Push(union);
                        }
                        else
                        {
                            results.Push(range);
                        }
                    }
                }
            }
            return results.Reverse();
        }

        /// <summary>
        /// Convert address range to cidr formatted ip strings
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        private void AppendTo(StringBuilder sb)
        {
            long start = Low;
            long end = High;
            var first = true;
            while (end >= start)
            {
                byte subnetSize = 32;
                while (subnetSize > 0)
                {
                    var mask = (1L << 32) - (1L << (32 - (subnetSize - 1)));
                    if ((start & mask) != start)
                    {
                        break;
                    }
                    subnetSize--;
                }
                var x = Math.Floor(Math.Log(end - start + 1) / Math.Log(2));
                var maxDiff = (byte)(32 - x);
                if (subnetSize < maxDiff)
                {
                    subnetSize = maxDiff;
                }
                var ip = ((IPv4Address)start).ToString();
                if (!first)
                {
                    sb.Append(';');
                }
                first = false;
                sb.Append(ip).Append('/').Append(subnetSize);
                if (Nic != kNullNicName)
                {
                    sb.Append(" [").Append(Nic).Append(']');
                }
                start += 1L << (32 - subnetSize);
            }
        }

        private const string kNullNicName = "custom";
        private uint _cur;
    }
}
