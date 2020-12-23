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
    /// Application registry event publisher
    /// </summary>
    public class ApplicationEventBusPublisher : IApplicationRegistryListener {

        /// <summary>
        /// Create event publisher
        /// </summary>
        /// <param name="bus"></param>
        public ApplicationEventBusPublisher(IEventBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <inheritdoc/>
        public Task OnApplicationDeletedAsync(RegistryOperationContextModel context,
            string applicationId, ApplicationInfoModel application) {
            return _bus.PublishAsync(Wrap(ApplicationEventType.Deleted, context,
                applicationId, application));
        }

        /// <inheritdoc/>
        public Task OnApplicationDisabledAsync(
            RegistryOperationContextModel context, ApplicationInfoModel application) {
            return _bus.PublishAsync(Wrap(ApplicationEventType.Disabled, context,
                application.ApplicationId, application));
        }

        /// <inheritdoc/>
        public Task OnApplicationEnabledAsync(
            RegistryOperationContextModel context, ApplicationInfoModel application) {
            return _bus.PublishAsync(Wrap(ApplicationEventType.Enabled, context,
                application.ApplicationId, application));
        }

        /// <inheritdoc/>
        public Task OnApplicationNewAsync(
            RegistryOperationContextModel context, ApplicationInfoModel application) {
            return _bus.PublishAsync(Wrap(ApplicationEventType.New, context,
                application.ApplicationId, application));
        }

        /// <inheritdoc/>
        public Task OnApplicationUpdatedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application) {
            return _bus.PublishAsync(Wrap(ApplicationEventType.Updated, context,
                application.ApplicationId, application));
        }

        /// <summary>
        /// Create application event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="applicationId"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        private static ApplicationEventModel Wrap(ApplicationEventType type,
            RegistryOperationContextModel context, string applicationId,
            ApplicationInfoModel application) {
            return new ApplicationEventModel {
                EventType = type,
                Context = context,
                Id = applicationId,
                Application = application
            };
        }

        private readonly IEventBus _bus;
    }
}
