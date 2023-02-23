// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics
{
    /// <summary>
    /// Process info
    /// </summary>
    public interface IProcessIdentity
    {
        /// <summary>
        /// Process identity
        /// </summary>
        string ProcessId { get; }

        /// <summary>
        /// Site id
        /// </summary>
        string SiteId { get; }

        /// <summary>
        /// Service identity
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Service name
        /// </summary>
        string Name { get; }
    }
}
