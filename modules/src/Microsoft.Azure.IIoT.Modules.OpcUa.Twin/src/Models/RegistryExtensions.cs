// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;

    /// <summary>
    /// Model extensions for twin module
    /// </summary>
    public static class RegistryExtensions {

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static AuthenticationMethodApiModel ToApiModel(
            this AuthenticationMethodModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodApiModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (IIoT.OpcUa.Api.Core.Models.CredentialType?)model.CredentialType ??
                    IIoT.OpcUa.Api.Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static AuthenticationMethodModel ToServiceModel(
            this AuthenticationMethodApiModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (IIoT.OpcUa.Core.Models.CredentialType?)model.CredentialType ??
                    IIoT.OpcUa.Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static EndpointActivationStatusApiModel ToApiModel(
            this EndpointActivationStatusModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationStatusApiModel {
                Id = model.Id,
                ActivationState = (IIoT.OpcUa.Api.Registry.Models.EndpointActivationState?)model.ActivationState
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static EndpointActivationStatusModel ToServiceModel(
            this EndpointActivationStatusApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationStatusModel {
                Id = model.Id,
                ActivationState = (IIoT.OpcUa.Registry.Models.EndpointActivationState?)model.ActivationState
            };
        }
    }
}
