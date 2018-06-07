// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {

    /// <summary>
    /// Qualifiable
    /// </summary>
    public interface IQualifiable {

        /// <summary>
        /// [0..1]  Qualifier of qualifable
        /// </summary>
        Qualifier Qualifier { get; }
    }
}