// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deploy {

    /// <summary>
    /// Azure Log Analytics Workspace configuration
    /// </summary>
    public interface ILogWorkspaceConfig {

        /// <summary>
        /// Log Analytics Workspace Id
        /// </summary>
        string LogWorkspaceId { get; }

        /// <summary>
        /// Log Analytics Workspace Key
        /// </summary>
        string LogWorkspaceKey { get; }
    }
}