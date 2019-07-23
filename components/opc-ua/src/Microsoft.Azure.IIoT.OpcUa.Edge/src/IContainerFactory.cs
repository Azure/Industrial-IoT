// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge {
    using Autofac;
    using System;

    /// <summary>
    /// Create Autofac container
    /// </summary>
    public interface IContainerFactory {

        /// <summary>
        /// Create container for twin
        /// </summary>
        /// <returns></returns>
        IContainer Create(Action<ContainerBuilder> configure);
    }
}

