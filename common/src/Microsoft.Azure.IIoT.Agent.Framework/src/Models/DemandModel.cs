// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    /// <summary>
    /// Demand model
    /// </summary>
    public class DemandModel {

        /// <summary>
        /// Create
        /// </summary>
        public DemandModel() {
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public DemandModel(string key, string value) {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Match operator
        /// </summary>
        public DemandOperators? Operator { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }
    }
}