// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// A single valued property is an atomic data property that has
    /// a single value. A single valued property instance should be
    /// time stamped.
    /// </summary>
    public class PropertySingleValued<T> : AtomicDataProperty {

        /// <summary>
        /// The value of the property instance.
        /// Constraint AAS-011: A single valued property instance
        /// shall have a value.
        /// Constraint AAS-012: The value of the property instance
        /// shall be consistent to the semantic definition of the
        /// property (if there is a semantic definition available
        /// for the property).
        ///
        /// Note: If the semantic Datatype of the property as described
        /// in its semantic definition (id_semantics) is an enumeration,
        /// then the value of the property is a coded value, i.e.
        /// a standardized value with defined semantics (i.e.a static
        /// property itself).
        /// </summary>
        [JsonProperty(PropertyName = "value",
            NullValueHandling = NullValueHandling.Ignore)]
        public T Value { get; set; }

      //  /// <summary>
      //  /// Optional xsd, rdf, or other type info. to capture type
      //  /// information for json decoding/encoding.
      //  /// </summary>
      //  [JsonProperty(PropertyName = "dataType",
      //      NullValueHandling = NullValueHandling.Ignore)]
      //  public IDataType DataType { get; set; }
    }
}