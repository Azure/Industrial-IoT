// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// A record element is an element within a record.
    /// </summary>
    public class RecordElement : ComplexDataProperty {

      //  /// <summary>
      //  /// Name of the record element within the record.
      //  /// </summary>
      //  [JsonProperty(PropertyName = "name")]
      //  public string RecordName { get; set; }

        // TODO: Obsolete!!

        /// <summary>
        /// Record element
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public Property Value { get; set; }
    }
}