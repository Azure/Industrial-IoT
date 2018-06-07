// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Templated object
    /// </summary>
    public interface IHasTemplate {

        /// <summary>
        /// [0..*] Target (combines id_hasTemplate and uses)
        /// </summary>
        List<Reference> /* Template */ HasTemplates { get; set; }
    }
}