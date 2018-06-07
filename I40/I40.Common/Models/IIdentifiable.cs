// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// An element that is uniquly referable, packagable and
    /// has versioning and revision metadata information.
    /// </summary>
    public interface IIdentifiable : IReferable, IPackageable {

        /// <summary>
        /// The identification of the identifiable element.
        /// </summary>
        [Required]
        Identification Id { get; }

        /// <summary>
        /// [0..1] Administrative information of an element.
        /// </summary>
        AdministrativeInformation Administration { get; }
    }
}