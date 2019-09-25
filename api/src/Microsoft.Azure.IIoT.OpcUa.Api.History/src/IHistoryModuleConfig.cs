// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History {

    /// <summary>
    /// Configuration for client
    /// </summary>
    public interface IHistoryModuleConfig {

        /// <summary>
        /// Edge device id
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Module id
        /// </summary>
        string ModuleId { get; }
    }
}
