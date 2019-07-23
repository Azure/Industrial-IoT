// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Operation consistency level
    /// </summary>
    public enum OperationConsistency {

        /// <summary>
        /// Strong consistency
        /// </summary>
        Strong,

        /// <summary>
        /// Bounded staleness
        /// </summary>
        Bounded,

        /// <summary>
        /// Session
        /// </summary>
        Session,

        /// <summary>
        /// Eventual
        /// </summary>
        Low
    }
}
