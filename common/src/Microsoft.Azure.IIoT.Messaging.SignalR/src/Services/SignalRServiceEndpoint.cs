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
    /// SignalR endpoint for serverless mode
    /// </summary>
    public class SignalRServiceEndpoint<THub> : IEndpoint<THub>
        where THub : Hub {

        /// <inheritdoc/>
        public string Resource { get; }

        /// <summary>
        /// Create signalR event bus
        /// </summary>
        /// <param name="config"></param>
        public SignalRServiceEndpoint(ISignalRServiceConfig config) {
            Resource = NameAttribute.GetName(typeof(THub));
            if (!string.IsNullOrEmpty(config?.SignalRConnString) && config.SignalRServerLess) {
                _serviceManager = new ServiceManagerBuilder()
                    .WithOptions(option => {
                        option.ConnectionString = config.SignalRConnString;
                        option.ServiceTransportType = ServiceTransportType.Persistent;
                    })
                    .BuildServiceManager();
            }
        }

        /// <inheritdoc/>
        public IdentityTokenModel GenerateIdentityToken(
            string userId,
            IList<Claim> claims,
            TimeSpan? lifeTime
        ) {
            if (_serviceManager == null) {
                return null;
            }
            if (lifeTime == null || !lifeTime.HasValue) {
                lifeTime = TimeSpan.FromMinutes(5);
            }

            var serviceHubContext = _serviceManager.CreateHubContextAsync(Resource, default)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            var negotiationResponse = serviceHubContext.NegotiateAsync(
                new NegotiationOptions() {
                    UserId = userId,
                    Claims = claims,
                    TokenLifetime = lifeTime.Value,
                })
                .ConfigureAwait(false).GetAwaiter().GetResult();

            return new IdentityTokenModel {
                Identity = userId,
                Key = negotiationResponse.AccessToken,
                Expires = DateTime.UtcNow + lifeTime.Value
            };
        }

        /// <summary> Service manager </summary>
        protected readonly ServiceManager _serviceManager;
    }
}