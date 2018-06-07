// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace I40.DINPVSxx.Models {
    using I40.Common.Models;

    /// <summary>
    /// An atomic data property is a property that is not further
    /// composed out of other properties.  An atomic data property
    /// is a property that has a value.The type of value differs
    /// for different subtypes of atomic data properties.
    /// </summary>
    public abstract class AtomicDataProperty : Property, IAtomic {
    }
}