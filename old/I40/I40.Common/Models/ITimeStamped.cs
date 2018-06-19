// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using System;

    /// <summary>
    /// Timestamped item
    /// </summary>
    public interface ITimeStamped {

        /// <summary>
        /// [0..1] Timestamp
        /// </summary>
        DateTime TimeStamp { get; }
    }
}