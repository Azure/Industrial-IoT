// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using Microsoft.Graph;

    class ServicePrincipalSettings {

        public Guid Id { get; set; }
        public string DisplayName { get; set; }

        public ServicePrincipalSettings() { }

        public ServicePrincipalSettings(
            Guid id,
            string displayName
        ) {
            Id = id;
            DisplayName = displayName;
        }

        /// <summary>
        /// Create instance of ServicePrincipalSettings from Microsoft.Graph.ServicePrincipal object.
        /// </summary>
        /// <param name="servicePrincipal"></param>
        public ServicePrincipalSettings(
            ServicePrincipal servicePrincipal
        ) {
            Id = new Guid(servicePrincipal.Id);
            DisplayName = servicePrincipal.DisplayName;
        }

        /// <summary>
        /// Create instance of Microsoft.Graph.ServicePrincipal representing current ServicePrincipalSettings.
        /// </summary>
        /// <returns></returns>
        public ServicePrincipal ToServicePrincipal() {
            var servicePrincipal = new ServicePrincipal() {
                Id = Id.ToString(),
                DisplayName = DisplayName
            };

            return servicePrincipal;
        }

        public void Validate(string parentProperty) {
            if (default == Id) {
                throw new Exception($"{parentProperty}.Id" +
                    $" configuration property is missing.");
            }

            if (string.IsNullOrEmpty(DisplayName)) {
                throw new Exception($"{parentProperty}.DisplayName" +
                    $" configuration property is missing or is empty.");
            }
        }
    }
}
