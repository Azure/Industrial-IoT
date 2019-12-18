// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using Microsoft.Graph;

    class ServicePrincipalSettings {

        public string Id { get; set; }
        public string DisplayName { get; set; }

        public ServicePrincipalSettings() { }

        public ServicePrincipalSettings(
            string id,
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
            Id = servicePrincipal.Id;
            DisplayName = servicePrincipal.DisplayName;
        }

        /// <summary>
        /// Create instance of Microsoft.Graph.ServicePrincipal representing current ServicePrincipalSettings.
        /// </summary>
        /// <returns></returns>
        public ServicePrincipal ToServicePrincipal() {
            var servicePrincipal = new ServicePrincipal() {
                Id = Id,
                DisplayName = DisplayName
            };

            return servicePrincipal;
        }
    }
}
