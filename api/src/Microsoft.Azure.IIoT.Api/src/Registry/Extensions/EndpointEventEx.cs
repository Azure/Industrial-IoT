// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Linq;

    /// <summary>
    /// Endpoint event extensions
    /// </summary>
    public static class EndpointEventEx {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointEventApiModel ToApiModel(
            this EndpointEventModel model) {
            return new EndpointEventApiModel {
                EventType = (EndpointEventType)model.EventType,
                Id = model.Id,
                Endpoint = model.Endpoint.ToApiModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static AuthenticationMethodApiModel ToApiModel(
            this AuthenticationMethodModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodApiModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (Core.Models.CredentialType?)model.CredentialType ??
                    Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static EndpointApiModel ToApiModel(
            this EndpointModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointApiModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
                Certificate = model.Certificate,
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static EndpointInfoApiModel ToApiModel(
            this EndpointInfoModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointInfoApiModel {
                ApplicationId = model.ApplicationId,
                NotSeenSince = model.NotSeenSince,
                Registration = model.Registration.ToApiModel(),
                ActivationState = (EndpointActivationState?)model.ActivationState,
                EndpointState = (EndpointConnectivityState?)model.EndpointState,
                OutOfSync = model.OutOfSync
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static EndpointRegistrationApiModel ToApiModel(
            this EndpointRegistrationModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointRegistrationApiModel {
                Id = model.Id,
                Endpoint = model.Endpoint.ToApiModel(),
                EndpointUrl = model.EndpointUrl,
                AuthenticationMethods = model.AuthenticationMethods?
                    .Select(p => p.ToApiModel())
                    .ToList(),
                SecurityLevel = model.SecurityLevel,
                SiteId = model.SiteId,
                DiscovererId = model.DiscovererId,
                SupervisorId = model.SupervisorId
            };
        }
    }
}