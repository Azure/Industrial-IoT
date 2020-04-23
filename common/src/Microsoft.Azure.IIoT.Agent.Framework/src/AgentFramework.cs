// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Agent;
    using Autofac;

    /// <summary>
    /// Agent framework module
    /// </summary>
    public sealed class AgentFramework : Module {

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {

            builder.RegisterType<WorkerSupervisor>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Worker>()
                .AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
