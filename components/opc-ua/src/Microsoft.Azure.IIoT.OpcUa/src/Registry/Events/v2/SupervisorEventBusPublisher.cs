// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2 {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor registry event supervisor
    /// </summary>
    public class SupervisorEventBusPublisher : ISupervisorRegistryListener {

        /// <summary>
        /// Create event supervisor
        /// </summary>
        /// <param name="bus"></param>
        public SupervisorEventBusPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnSupervisorDeletedAsync(RegistryOperationContextModel context,
            string supervisorId) {
            return _bus.PublishAsync(Wrap(SupervisorEventType.Deleted, context,
                supervisorId, null));
        }

        /// <inheritdoc/>
        public Task OnSupervisorNewAsync(RegistryOperationContextModel context,
            SupervisorModel supervisor) {
            return _bus.PublishAsync(Wrap(SupervisorEventType.New, context,
                supervisor.Id, supervisor));
        }

        /// <inheritdoc/>
        public Task OnSupervisorUpdatedAsync(RegistryOperationContextModel context,
            SupervisorModel supervisor) {
            return _bus.PublishAsync(Wrap(SupervisorEventType.Updated, context,
                supervisor.Id, supervisor));
        }

        /// <summary>
        /// Create supervisor event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="supervisorId"></param>
        /// <param name="supervisor"></param>
        /// <returns></returns>
        private static SupervisorEventModel Wrap(SupervisorEventType type,
            RegistryOperationContextModel context, string supervisorId,
            SupervisorModel supervisor) {
            return new SupervisorEventModel {
                EventType = type,
                Context = context,
                Id = supervisorId,
                Supervisor = supervisor
            };
        }

        private readonly IEventBus _bus;
    }
}
