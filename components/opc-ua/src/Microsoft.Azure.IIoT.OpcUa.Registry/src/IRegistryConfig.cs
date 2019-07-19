// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry {

    /// <summary>
    /// Registry configuration
    /// </summary>
    public interface IRegistryConfig {

        /// <summary>
        /// Auto approve applications
        /// </summary>
        bool ApplicationsAutoApprove { get; }
    }
}
