// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// A sequence number
    /// </summary>
    public static class SequenceNumber {

        /// <summary>
        /// Increment sequence number
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static uint Increment32(ref uint seq) {
            while (true) {
                var result = Interlocked.Increment(ref seq);
                if (result != 0) {
                    return result;
                }
            }
        }

        /// <summary>
        /// Increment sequence number
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public static ushort Increment16(ref uint seq) {
            while (true) {
                var result = (ushort)Interlocked.Increment(ref seq);
                if (result != 0) {
                    return result;
                }
            }
        }

        /// <summary>
        /// Create a range of values missing between 2 sequence numbers
        /// Considers wrap around
        /// </summary>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        public static IEnumerable<uint> Missing(uint seq1, uint seq2) {
            unchecked {
                var diff = Math.Abs((int)seq1 - (int)seq2);
                if (diff > 1) {
                    var (startAt, endAt) = (((int)seq1 + diff) == (int)seq2) ? (seq1, seq2) : (seq2, seq1);
                    while (++startAt != endAt) {
                        if (startAt != 0) {
                            yield return startAt;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create a range of values missing between 2 sequence numbers
        /// Considers wrap around
        /// </summary>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        public static IEnumerable<ushort> Missing(ushort seq1, ushort seq2) {
            unchecked {
                var diff = Math.Abs((short)seq1 - (short)seq2);
                if (diff > 1) {
                    var (startAt, endAt) = (((short)seq1 + diff) == (short)seq2) ? (seq1, seq2) : (seq2, seq1);
                    while (++startAt != endAt) {
                        if (startAt != 0) {
                            yield return startAt;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate the sequence number and update the last to current
        /// </summary>
        /// <param name="sequenceNumber"></param>
        /// <param name="lastSequenceNumber"></param>
        /// <param name="missing"></param>
        /// <returns></returns>
        public static bool Validate(uint sequenceNumber, ref uint lastSequenceNumber, out uint[] missing) {
            try {
                var expected = lastSequenceNumber + 1 == 0 ? 1 : lastSequenceNumber + 1;
                var ok = sequenceNumber == expected;
                if (!ok) {
                    missing = Missing(lastSequenceNumber, sequenceNumber).ToArray();
                    return false;
                }
                missing = Array.Empty<uint>();
                return true;
            }
            finally {
                lastSequenceNumber = sequenceNumber;
            }
        }
    }
}
