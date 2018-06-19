// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using I40.Common.Identifiers;
    using System;

    /// <summary>
    /// Identifier extensions
    /// </summary>
    public static class IdentifierEx {

        /// <summary>
        /// Parse as identifer. Internal ids start with $, IRDI
        /// and Uri are parsed using the respective parsers.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Identification AsIdentifier(this string id) {
            if (id.StartsWith("$", StringComparison.Ordinal)) {
                return new Identification { Id = id.Substring(1), Kind = IdentificationKind.Internal };
            }
            if (Irdi.TryParse(id, out var irdi)) {
                return new Identification { Id = irdi.ToString(), Kind = IdentificationKind.Irdi };
            }
            if (Uri.TryCreate(id, UriKind.RelativeOrAbsolute, out var uri)) {
                return new Identification { Id = uri.ToString(), Kind = IdentificationKind.Uri };
            }
            //  try {
            //      return new Identifier { Id = new Uri(id).ToString(), Kind = IdentifierKind.Uri };
            //  }
            //  catch {
            //      // Return as internal identifier since it is neither one we understand
            //      return new Identifier { Id = id, Kind = IdentifierKind.Internal };
            //  }
            throw new FormatException($"Bad identifier {id}");
        }

        /// <summary>
        /// Identifer to string. Internal ids are prefixed with $.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsString(this Identification value) {
            switch (value.Kind) {
                case IdentificationKind.Irdi:
                case IdentificationKind.Uri:
                   return value.Id;
                case IdentificationKind.Internal:
                    return $"${value.Id}";
                default:
                    throw new FormatException("Bad identifier kind");
            }
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this Identification value, Identification that) =>
            value?.AsString() == that?.AsString();

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this Identification value, Uri that) =>
            value.Kind == IdentificationKind.Uri && value?.AsString() == that?.ToString();

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this Identification value, Irdi that) =>
            value.Kind == IdentificationKind.Irdi && value?.AsString() == that?.ToString();
    }
}