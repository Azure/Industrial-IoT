// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {

    /// <summary>
    /// A simple serializable index
    /// </summary>
    public class Bitmap {

        /// <summary>
        /// The serialized bits
        /// </summary>
        internal List<ulong> Bits { get; set; }

        /// <summary>
        /// Create bitmap
        /// </summary>
        public Bitmap() {
            Bits = new List<ulong>();
        }

        /// <summary>
        /// Create a clone
        /// </summary>
        /// <param name="map"></param>
        public Bitmap(Bitmap map) {
            Bits = map?.Bits ?? throw new ArgumentNullException(nameof(map));
        }

        /// <summary>
        /// Create a clone
        /// </summary>
        /// <param name="bits"></param>
        internal Bitmap(List<ulong> bits) {
            Bits = bits ?? throw new ArgumentNullException(nameof(bits));
        }

        /// <summary>
        /// Returns index back so it can be allocated again
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Whether the index was freed</returns>
        public bool Free(uint index) {
            var bit = (int)(index % 64);
            var blockIdx = (int)(index / 64);
            if (blockIdx < Bits.Count) {
                if (0 != (Bits[blockIdx] & (1ul << bit))) {
                    Bits[blockIdx] &= ~(1ul << bit); // Clear bit
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get first free unuseds index
        /// </summary>
        /// <returns>The next available unused index</returns>
        public uint Allocate() {
            for (var blockIdx = 0; blockIdx < Bits.Count; blockIdx++) {
                if (Bits[blockIdx] == ulong.MaxValue) {
                    continue; // Full - continue
                }
                // Grab from block
                var block = Bits[blockIdx];
                for (var bit = 0; bit < 64; bit++) {
                    if (0 == (block & (1ul << bit))) {
                        Bits[blockIdx] |= 1ul << bit;
                        return (uint)(((uint)blockIdx * 64) + bit);
                    }
                }
            }
            // Add new block
            Bits.Add(1);
            return (uint)(Bits.Count - 1) * 64;
        }
    }
}
