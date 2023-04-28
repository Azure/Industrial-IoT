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
    /// Application registry event publisher
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public class ApplicationEventPublisher<THub> : IApplicationRegistryListener
    {
        /// <inheritdoc/>
        public ApplicationEventPublisher(ICallbackInvoker<THub> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <inheritdoc/>
        public Task OnApplicationDeletedAsync(OperationContextModel? context,
            string applicationId, ApplicationInfoModel application)
        {
            return PublishAsync(ApplicationEventType.Deleted, context,
                applicationId, application);
        }

        /// <inheritdoc/>
        public Task HandleApplicationDisabledAsync(
            OperationContextModel? context, ApplicationInfoModel application)
        {
            return PublishAsync(ApplicationEventType.Disabled, context,
                application.ApplicationId, application);
        }

        /// <inheritdoc/>
        public Task OnApplicationEnabledAsync(
            OperationContextModel? context, ApplicationInfoModel application)
        {
            return PublishAsync(ApplicationEventType.Enabled, context,
                application.ApplicationId, application);
        }

        /// <inheritdoc/>
        public Task OnApplicationNewAsync(
            OperationContextModel? context, ApplicationInfoModel application)
        {
            return PublishAsync(ApplicationEventType.New, context,
                application.ApplicationId, application);
        }

        /// <inheritdoc/>
        public Task OnApplicationUpdatedAsync(OperationContextModel? context,
            ApplicationInfoModel application)
        {
            return PublishAsync(ApplicationEventType.Updated, context,
                application.ApplicationId, application);
        }

        /// <summary>
        /// Publish application event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="applicationId"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        public Task PublishAsync(ApplicationEventType type,
            OperationContextModel? context, string applicationId,
            ApplicationInfoModel application)
        {
            var arguments = new object[] {
                new ApplicationEventModel {
                    EventType = type,
                    Context = context,
                    Id = applicationId,
                    Application = application
                }
            };
            return _callback.BroadcastAsync(
                EventTargets.ApplicationEventTarget, arguments);
        }

        private readonly ICallbackInvoker<THub> _callback;
    }
}
