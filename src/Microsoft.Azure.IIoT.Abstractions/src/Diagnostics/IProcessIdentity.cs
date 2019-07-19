// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {

    /// <summary>
    /// Process info
    /// </summary>
    public interface IProcessIdentity {

        /// <summary>
        /// Process identity
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Service identity
        /// </summary>
        string ServiceId { get; }

        /// <summary>
        /// Service name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description
        /// </summary>
        string Description { get; }
    }
}
