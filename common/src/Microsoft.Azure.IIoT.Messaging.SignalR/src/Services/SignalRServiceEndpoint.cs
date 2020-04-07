// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR.Services {
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.SignalR.Management;
    using Microsoft.AspNetCore.SignalR;
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;

    /// <summary>
    /// Publish subscriber service built using signalr
    /// </summary>
    public class SignalRServiceEndpoint<THub> : IEndpoint<THub>
        where THub : Hub {

        /// <inheritdoc/>
        public string Resource { get; }

        /// <inheritdoc/>
        public Uri EndpointUrl => _serviceManager == null ? null :
            new Uri(_serviceManager.GetClientEndpoint(Resource));

        /// <summary>
        /// Create signalR event bus
        /// </summary>
        /// <param name="config"></param>
        public SignalRServiceEndpoint(ISignalRServiceConfig config) {
            Resource = NameAttribute.GetName(typeof(THub));
            if (!string.IsNullOrEmpty(config?.SignalRConnString)) {
                _serviceManager = new ServiceManagerBuilder().WithOptions(option => {
                    option.ConnectionString = config.SignalRConnString;
                    option.ServiceTransportType = ServiceTransportType.Persistent;
                }).Build();
            }
        }


        /// <inheritdoc/>
        public IdentityTokenModel GenerateIdentityToken(string userId,
            IList<Claim> claims, TimeSpan? lifeTime) {
            if (_serviceManager == null) {
                return null;
            }
            if (lifeTime == null) {
                lifeTime = TimeSpan.FromMinutes(5);
            }
            return new IdentityTokenModel {
                Identity = userId,
                Key = _serviceManager.GenerateClientAccessToken(
                    Resource, userId, claims, lifeTime),
                Expires = DateTime.UtcNow + lifeTime.Value
            };
        }

        /// <summary> Service manager </summary>
        protected readonly IServiceManager _serviceManager;
    }
}