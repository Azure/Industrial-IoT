// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics
{
    using Furly.Extensions.Hosting;

    /// <summary>
    /// Process info
    /// </summary>
    public interface IProcessInfo : IProcessIdentity
    {
        /// <summary>
        /// Site id
        /// </summary>
        string SiteId { get; }
    }
}
