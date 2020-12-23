// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using System;
    using Autofac;

    /// <summary>
    /// Lifetime scope factory for processing engines
    /// </summary>
    public interface IProcessingEngineContainerFactory {

        /// <summary>
        /// Create a factory to create individual scopes engines.
        /// </summary>
        /// <returns></returns>
        Action<ContainerBuilder> GetJobContainerScope(
            string agentId, string jobId);
    }
}