// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Hosting
{
    /// <summary>
    /// Process identity
    /// </summary>
    public interface IProcessIdentity
    {
        /// <summary>
        /// Process identity
        /// </summary>
        string Identity { get; }
    }
}
