// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using I40.Common.Encoding;
    using I40.Common.Identifiers;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A resolvable identification
    /// </summary>
    [JsonConverter(typeof(IdentifierConverter))]
    public class Identification {

        /// <summary>
        /// Identifier of the element
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Type of the identifier, e.g. URI, IRDI, local
        /// </summary>
        [Required]
        public IdentificationKind Kind { get; set; }

        /// <summary>
        /// Hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() =>
            this.AsString().GetHashCode();

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) =>
            obj is Identification r && r.IsEqual(this);

        public static bool operator ==(Identification id1,
            Identification id2) => id1.IsEqual(id2);
        public static bool operator !=(Identification id1,
            Identification id2) => !(id1 == id2);

        public static bool operator ==(Identification id1,
            Uri id2) => id1.IsEqual(id2);
        public static bool operator !=(Identification id1,
            Uri id2) => !(id1 == id2);

        public static bool operator ==(Identification id1,
            Irdi id2) => id1.IsEqual(id2);
        public static bool operator !=(Identification id1,
            Irdi id2) => !(id1 == id2);

        /// <summary>
        /// Implicit convert to reference
        /// </summary>
        /// <param name="t"></param>
        public static implicit operator Reference(
            Identification t) => new Reference {
                Target = t
            };

        /// <summary>
        /// Implicit convert from identifier type
        /// </summary>
        /// <param name="t"></param>
        public static implicit operator Identification(Uri t) =>
            new Identification {
                Id = t.ToString(), Kind = IdentificationKind.Uri
            };
        public static implicit operator Identification(Irdi t) =>
            new Identification {
                Id = t.ToString(), Kind = IdentificationKind.Irdi
            };
    }
}