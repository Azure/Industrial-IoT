// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Enum field
    /// </summary>
    public class EnumFieldModel {

        /// <summary>
        /// Name of the field
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the field.
        /// </summary>
        public long Value { get; set; }

        /// <summary>
        /// Human readable name for the value.
        /// </summary>
        public LocalizedTextModel DisplayName { get; set; }

        /// <summary>
        /// A description of the value.
        /// </summary>
        public LocalizedTextModel Description { get; set; }
    }
}
