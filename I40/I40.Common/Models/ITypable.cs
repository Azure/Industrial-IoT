// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Either instance or type
    /// </summary>
    public interface ITypable {

        /// <summary>
        /// Kind_asset denotes whether the AAS represents
        /// an asset type (kind_asset=Type) or an asset
        /// instance (kind_asset=Instance).
        /// </summary>
        [Required]
        Kind Kind { get; }
    }
}