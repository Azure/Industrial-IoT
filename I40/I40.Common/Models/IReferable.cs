// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// An element that is referable by its id.
    /// This id is not globally unique.
    /// </summary>
    public interface IReferable {

        /// <summary>
        /// A self reference to this referable.
        ///
        /// The reference is a unique id that is serialized as $id.
        /// It is referred to using json pointers in the form of $ref
        /// from other items using referable.
        ///
        /// This is used particularly in property references.
        /// </summary>
        [Required]
        Reference Self { get; }

        /// <summary>
        /// The identification of an element.
        /// </summary>
        [Required]
        string Name { get; }

        /// <summary>
        /// Descriptions or comments on the element.
        /// </summary>
        LanguageString Description { get; }
    }
}