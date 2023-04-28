// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Events
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor registry event publisher
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class SupervisorEventPublisher<THub> : ISupervisorRegistryListener
    {
        /// <inheritdoc/>
        public SupervisorEventPublisher(ICallbackInvoker<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnSupervisorDeletedAsync(OperationContextModel? context,
            string supervisorId)
        {
            return PublishAsync(SupervisorEventType.Deleted, context,
                supervisorId, null);
        }

        /// <inheritdoc/>
        public Task OnSupervisorNewAsync(OperationContextModel? context,
            SupervisorModel supervisor)
        {
            return PublishAsync(SupervisorEventType.New, context,
                supervisor.Id, supervisor);
        }

        /// <inheritdoc/>
        public Task OnSupervisorUpdatedAsync(OperationContextModel? context,
            SupervisorModel supervisor)
        {
            return PublishAsync(SupervisorEventType.Updated, context,
                supervisor.Id, supervisor);
        }

        /// <summary>
        /// Publish supervisor event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="supervisorId"></param>
        /// <param name="supervisor"></param>
        /// <returns></returns>
        public Task PublishAsync(SupervisorEventType type,
            OperationContextModel? context, string? supervisorId,
            SupervisorModel? supervisor)
        {
            var arguments = new object[] {
                new SupervisorEventModel {
                    EventType = type,
                    Context = context,
                    Id = supervisorId,
                    Supervisor = supervisor
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.SupervisorEventTarget, arguments);
        }
        private readonly ICallbackInvoker<THub> _callback;
    }
}
