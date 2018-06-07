// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System;

    /// <summary>
    /// A reference to an identifiable target item
    /// </summary>
    [JsonConverter(typeof(Encoding.ReferenceConverter))]
    public class Reference {

        /// <summary>
        /// Reference target identifiable
        /// </summary>
        [Required]
        public Identification Target { get; set; }

        /// <summary>
        /// [0...*] Path to the refereable child from
        /// the identifiable target (ordered)
        /// </summary>
        [DefaultValue(null)]
        public List<string> SubPath { get; set; }

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
            obj is Reference r && r.IsEqual(this);

        public static bool operator ==(Reference r1,
            Reference r2) => r1.IsEqual(r2);
        public static bool operator !=(Reference r1,
            Reference r2) => !(r1 == r2);
        public static bool operator ==(Reference r1,
            IReferable r2) => r1.IsEqual(r2);
        public static bool operator !=(Reference r1,
            IReferable r2) => !(r1 == r2);
        public static bool operator ==(IReferable r1,
            Reference r2) => r1.IsEqual(r2);
        public static bool operator !=(IReferable r1,
            Reference r2) => !(r1 == r2);

        /// <summary>
        /// Cast explicit, and if subpaths, throws
        /// cast exception.
        /// </summary>
        /// <param name="r"></param>
        public static explicit operator Identification(
            Reference r) => (r.SubPath?.Any() ?? false) ?
                throw new InvalidCastException() : r.Target;
    }
}