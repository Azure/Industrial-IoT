// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Identifiers {
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Data identifier
    /// </summary>
    public sealed class Di {

        /// <summary>
        /// Code space identifier (optional)
        /// </summary>
        public Csi CSI { get; }

        /// <summary>
        /// Item code
        /// </summary>
        public string IC { get; }

        /// <summary>
        /// Create Data identifier
        /// </summary>
        public Di(Csi csi, string ic) {
            CSI = csi;
            IC = ic;
        }

        /// <summary>
        /// Create Data identifier
        /// </summary>
        public Di(string ic) {
            CSI = Csi.Undefined;
            IC = ic;
        }

        /// <summary>
        /// Parse Di
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Di Parse(string value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            var items = value.Split('-');
            if (items.Length == 2) {
                if (items[0].Length != 2) {
                    throw new FormatException($"Bad CSI '{value}'");
                }
                return new Di(items[0].ToCsi(), items[1]);
            }
            if (items.Length == 1) {
                return new Di(items[0]);
            }
            throw new FormatException($"Bad DI '{value}'");
        }

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            CSI == Csi.Undefined ? IC : $"{CSI.ToCode()}-{IC}";

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) =>
            obj is Di di && CSI == di.CSI && IC == di.IC;

        public static bool operator ==(Di di1, Di di2) =>
            EqualityComparer<Di>.Default.Equals(di1, di2);

        public static bool operator !=(Di di1, Di di2) =>
            !(di1 == di2);

        /// <summary>
        /// Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = -938466333;
            hashCode = hashCode * -1521134295 +
                CSI.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(IC);
            return hashCode;
        }
    }
}
