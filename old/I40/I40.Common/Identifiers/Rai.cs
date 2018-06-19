// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Identifiers {
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Registration authority info based on ISO/IEC 6523 which defines a
    /// Structure for the identification of organizations and organization
    /// parts is an international standard that defines a structure for
    /// uniquely identifying organizations and parts thereof in computer
    /// data interchange and specifies the registration procedure to obtain
    /// an International Code Designator (ICD) value for an identification
    /// scheme. See <a href="http://www.cyber-identity.com/iso6523/"/>
    /// </summary>
    public sealed class Rai {

        /// <summary>
        /// The international code designator - up to 4 digits
        /// </summary>
        public ushort ICD => ushort.Parse(_icd);

        /// <summary>
        /// The organization identifier - up to maximum 35 chars.
        /// </summary>
        public string OI { get; }

        /// <summary>
        /// The optional organization part identifer (OPI) - up
        /// to 35 chars.
        /// </summary>
        public string OPI { get; }

        /// <summary>
        /// And optional OPI Source indictor (OPIS) exactly 1 digit
        /// </summary>
        public byte OPIS => byte.Parse(_opis);

        /// <summary>
        /// Additional optional information in RAI of up to 35
        /// chars
        /// </summary>
        public string AI { get; }

        /// <summary>
        /// Create Registry identifier
        /// </summary>
        public Rai(ushort icd, string oi,
            string opi = null, byte? opis = null, string ai = null) {
            _icd = icd.ToString("4");
            _opis = opis?.ToString("#");
            OPI = opi;
            OI = oi;
            AI = ai;
            _sep = '-';
        }

        /// <summary>
        /// Parse Registry identifier
        /// </summary>
        private Rai(string value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            var items = value.Split('-');
            if (items.Length < 2 || items.Length > 5) {
                // Try / used in CDD as encoding for -
                items = value.Split('/');
                if (items.Length < 2 || items.Length > 5) {
                    throw new FormatException($"Bad RAI '{value}'");
                }
                _sep = '/';
            }
            else {
                _sep = '-';
            }

            if (items[0].Length > 4 || string.IsNullOrEmpty(items[0]) ||
                items[1].Length > 35 || string.IsNullOrEmpty(items[1]) ||
                !ushort.TryParse(items[0], out var tmp)) {
                throw new FormatException($"Bad OI or ICD '{value}'");
            }

            _icd = items[0];
            OI = items[1];

            if (items.Length > 2 && !string.IsNullOrEmpty(items[2])) {
                if (items[2].Length > 35) {
                    throw new FormatException($"Bad OPI value '{value}'");
                }
                OPI = items[2].Trim();
            }
            if (items.Length > 3 && !string.IsNullOrEmpty(items[3])) {
                if (items[3].Length > 1 || !byte.TryParse(items[3], out var t)) {
                    throw new FormatException($"Bad OPIS value '{value}'");
                }
                _opis = items[3].Trim();
            }
            if (items.Length > 4 && !string.IsNullOrEmpty(items[3])) {
                if (items[4].Length > 35) {
                    throw new FormatException($"Bad AI value '{value}'");
                }
                AI = items[3].Trim();
            }
        }

        /// <summary>
        /// Parse Rai
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Rai Parse(string value) => new Rai(value);

        private readonly string _icd;
        private readonly string _opis;
        private readonly char _sep;

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"{_icd}{_sep}{OI}{_sep}{(OPI ?? "")}{_sep}{(_opis ?? "")}{_sep}{(AI ?? "")}".Trim(_sep);

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) =>
            obj is Rai rai && ICD == rai.ICD && AI == rai.AI && OI == rai.OI;

        public static bool operator ==(Rai rai1, Rai rai2) =>
            EqualityComparer<Rai>.Default.Equals(rai1, rai2);

        public static bool operator !=(Rai rai1, Rai rai2) =>
            !(rai1 == rai2);

        /// <summary>
        /// Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = -938466333;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(AI);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(OI);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<ushort>.Default.GetHashCode(ICD);
            return hashCode;
        }
    }
}
