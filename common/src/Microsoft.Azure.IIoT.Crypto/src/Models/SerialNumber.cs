// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Security.Cryptography;

    /// <summary>
    /// Represents a serial number
    /// </summary>
    public class SerialNumber {

        /// <summary>
        /// Serial number in big endian
        /// </summary>
        public byte[] Value { get; }

        /// <summary>
        /// Create random serial number
        /// </summary>
        public SerialNumber(int size = 20) {
            if (size <= 0) {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
            var serialNumber = new byte[size];
            RandomNumberGenerator.Fill(serialNumber);
            Value = NormalizeSerialNumber(serialNumber, true);
        }

        /// <summary>
        /// Create serial number
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="isBigEndian"></param>
        public SerialNumber(byte[] serialNumber, bool isBigEndian = true) {
            Value = NormalizeSerialNumber(serialNumber, isBigEndian);
        }

        /// <summary>
        /// Create serial number
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="isBigEndian"></param>
        public SerialNumber(BigInteger serialNumber, bool isBigEndian = true) :
            this(serialNumber.ToByteArray(), isBigEndian) {
        }

        /// <summary>
        /// Convert from serial number to big-endian bytes
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="asBigEndian"></param>
        /// <returns></returns>
        public static SerialNumber Parse(string serialNumber,
            bool asBigEndian = true) {
            var serial = serialNumber.DecodeAsBase16();
            return new SerialNumber(serial, asBigEndian);
        }

        /// <summary>
        /// Return as big integer
        /// </summary>
        /// <returns></returns>
        public BigInteger ToBigInteger() {
            return new BigInteger(Value, false, true);
        }

        /// <summary>
        /// Convert from serial number to serial number string
        /// </summary>
        /// <param name="asBigEndian"></param>
        /// <returns></returns>
        public string ToString(bool asBigEndian) {
            var serialNumber = Value;
            if (!asBigEndian) {
                serialNumber = Value.Reverse().ToArray();
            }
            return serialNumber.ToBase16String();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is SerialNumber number &&
                 Value.SequenceEqualsSafe(number.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return HashCode.Combine(Value);
        }

        /// <inheritdoc/>
        public static bool operator ==(SerialNumber left, SerialNumber right) =>
            EqualityComparer<SerialNumber>.Default.Equals(left, right);
        /// <inheritdoc/>
        public static bool operator !=(SerialNumber left, SerialNumber right) =>
            !(left == right);

        /// <inheritdoc/>
        public override string ToString() {
            return ToString(true);
        }

        /// <summary>
        /// Normalize serial number
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="isBigEndian"></param>
        /// <returns></returns>
        private static byte[] NormalizeSerialNumber(byte[] serialNumber,
            bool isBigEndian) {
            if (serialNumber == null) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            if (serialNumber.Length < 1) {
                throw new ArgumentException(nameof(serialNumber));
            }
            if (!isBigEndian) {
                serialNumber = serialNumber.ToArray();
                Array.Reverse(serialNumber);
            }
            if (serialNumber[0] >= 0x80) {
                // Keep the serial number unsigned by prepending a zero.
                var newSerialNumber = new byte[serialNumber.Length + 1];
                newSerialNumber[0] = 0;
                serialNumber.CopyTo(newSerialNumber, 1);
                return newSerialNumber;
            }
            // Strip any unnecessary zeros from the beginning.
            var leadingZeros = 0;
            while (leadingZeros < serialNumber.Length - 1 &&
                serialNumber[leadingZeros] == 0 &&
                serialNumber[leadingZeros + 1] < 0x80) {
                leadingZeros++;
            }
            if (leadingZeros != 0) {
                var newSerialNumber = new byte[serialNumber.Length - leadingZeros];
                Array.ConstrainedCopy(serialNumber, leadingZeros,
                    newSerialNumber, 0, newSerialNumber.Length);
                return newSerialNumber;
            }
            if (isBigEndian) {
                return serialNumber.ToArray();
            }
            return serialNumber;
        }
    }
}
