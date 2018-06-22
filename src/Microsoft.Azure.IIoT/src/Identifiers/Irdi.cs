// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Identifiers {
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// The International Registration Data Identifier (IRDI) is based on
    /// the international standards ISO/IEC 11179-6, ISO 29002-5 and
    /// ISO 6532.
    ///
    /// It consists of an International Code Designator (ICD) according to
    /// ISO 6523, followed by an Organization Identifier(OI) and Code Space
    /// Identifer (CSI) that identifies the type of object
    ///
    /// Examples:
    /// CDD:
    /// 0112/2///61360_4#AFA001#001
    /// 0112/2///61360_4#AAA233
    /// 0112/2///61360_4#AAA233#001
    /// @eClass:
    /// 0161-1#04-000001#1
    /// </summary>
    public sealed class Irdi {

        /// <summary>
        /// Registration authority
        /// </summary>
        public Rai RAI { get; }

        /// <summary>
        /// Data identifier
        /// </summary>
        public Di DI { get; }

        /// <summary>
        /// Version identifier
        /// </summary>
        short? VI => _vi == null ? (short?)null : short.Parse(_vi);

        /// <summary>
        /// Create Irdi
        /// </summary>
        public Irdi(Rai rai, Di di, string vi = null) {
            RAI = rai;
            DI = di;
            _vi = vi;
        }

        /// <summary>
        /// Parse Irdi
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Irdi Parse(string value, bool skipVersion = false) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            var items = value.Split('#');
            if (items.Length == 2) {
                return new Irdi(Rai.Parse(items[0]), Di.Parse(items[1]), null);
            }
            if (items.Length == 3 && short.TryParse(items[2], out var tmp)) {
                return new Irdi(Rai.Parse(items[0]), Di.Parse(items[1]),
                    skipVersion ? null : items[2]);
            }
            throw new FormatException($"Bad IRDI '{value}'");
        }

        /// <summary>
        /// Try parse Irdi
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryParse(string value, bool skipVersion, out Irdi irdi) {
            try {
                irdi = Parse(value, skipVersion);
                return true;
            }
            catch {
                irdi = null;
                return false;
            }
        }

        /// <summary>
        /// Try parse Irdi
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryParse(string value, out Irdi irdi) =>
            TryParse(value, false, out irdi);

        private readonly string _vi;

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) =>
            obj is Irdi irdi && VI == irdi.VI && DI == irdi.DI && RAI == irdi.RAI;

        public static bool operator ==(Irdi irdi1, Irdi irdi2) =>
            EqualityComparer<Irdi>.Default.Equals(irdi1, irdi2);

        public static bool operator !=(Irdi irdi1, Irdi irdi2) =>
            !(irdi1 == irdi2);

        /// <summary>
        /// Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = -938466333;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<Rai>.Default.GetHashCode(RAI);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<Di>.Default.GetHashCode(DI);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<short?>.Default.GetHashCode(VI);
            return hashCode;
        }

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            _vi == null ? $"{RAI}#{DI}" : $"{RAI}#{DI}#{_vi}";
    }
}