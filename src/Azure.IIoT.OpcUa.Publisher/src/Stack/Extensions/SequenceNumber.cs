// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// A sequence number
    /// </summary>
    public static class SequenceNumber
    {
        /// <summary>
        /// Increment sequence number
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static uint Increment32(ref uint seq)
        {
            while (true)
            {
                var result = Interlocked.Increment(ref seq);
                if (result != 0)
                {
                    return result;
                }
            }
        }

        /// <summary>
        /// Increment sequence number
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static ushort Increment16(ref uint seq)
        {
            while (true)
            {
                var result = (ushort)Interlocked.Increment(ref seq);
                if (result != 0)
                {
                    return result;
                }
            }
        }

        /// <summary>
        /// Create a range of values missing between 2 sequence numbers
        /// Considers wrap around
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="dropped"></param>
        /// <returns></returns>
        public static uint[] Missing(uint from, uint to, out bool dropped)
        {
            unchecked
            {
                var diff = Math.Abs((int)from - (int)to);
                if (diff > 1)
                {
                    var (startAt, endAt) = (((int)from + diff) == (int)to) ? (from, to) : (to, from);
                    dropped = from == startAt;
                    var missing = new List<uint>(diff - 1);
                    while (++startAt != endAt)
                    {
                        if (startAt != 0)
                        {
                            missing.Add(startAt);
                        }
                    }
                    if (missing.Count > 0)
                    {
                        return [.. missing];
                    }
                }
            }
            dropped = false;
            return [];
        }

        /// <summary>
        /// Convert missing sequence numbers to string
        /// </summary>
        /// <param name="missingSequenceNumbers"></param>
        /// <returns></returns>
        public static string ToString(uint[] missingSequenceNumbers)
        {
            switch (missingSequenceNumbers.Length)
            {
                case 0:
                    return "none";
                case 1:
                    return missingSequenceNumbers[0].ToString(CultureInfo.InvariantCulture);
                default:
                    var length = missingSequenceNumbers.Length;
                    if (length > 6)
                    {
                        var last = missingSequenceNumbers[length - 1];
                        return $"{missingSequenceNumbers[0]}...{last}";
                    }
                    return missingSequenceNumbers
                        .Select(a => a.ToString(CultureInfo.InvariantCulture))
                        .Aggregate((a, b) => $"{a}, {b}");
            }
        }

        /// <summary>
        /// Validate the sequence number and update the last to current
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="lastSequenceNumber"></param>
        /// <param name="missing"></param>
        /// <param name="dropped"></param>
        /// <returns></returns>
        public static bool Validate(uint sequenceNumber, ref uint lastSequenceNumber,
            out uint[] missing, out bool dropped)
        {
            try
            {
                // Allow duplicates as events and data changes can be in the same notification
                if (lastSequenceNumber != sequenceNumber)
                {
                    var expected = lastSequenceNumber + 1 == 0 ? 1 : lastSequenceNumber + 1;
                    var ok = sequenceNumber == expected;
                    if (!ok)
                    {
                        missing = [.. Missing(lastSequenceNumber, sequenceNumber, out dropped)];
                        return false;
                    }
                }
                missing = [];
                dropped = false;
                return true;
            }
            finally
            {
                lastSequenceNumber = sequenceNumber;
            }
        }
    }
}
