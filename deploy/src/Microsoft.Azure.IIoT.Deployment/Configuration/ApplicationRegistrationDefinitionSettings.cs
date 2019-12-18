// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using Microsoft.Azure.IIoT.Deployment.Deployment;

    class ApplicationRegistrationDefinitionSettings {

        public ApplicationSettings ServicesApplication { get; set; }
        public ServicePrincipalSettings ServicesApplicationSP { get; set; }
        public ApplicationSettings ClientsApplication { get; set; }
        public ServicePrincipalSettings ClientsApplicationSP { get; set; }
        public ApplicationSettings AksApplication { get; set; }
        public ServicePrincipalSettings AksApplicationSP { get; set; }
        public string AksApplicationRbacSecret { get; set; }

        public ApplicationRegistrationDefinitionSettings() { }

        public ApplicationRegistrationDefinitionSettings(
            ApplicationSettings servicesApplication,
            ServicePrincipalSettings servicesApplicationSP,
            ApplicationSettings clientsApplication,
            ServicePrincipalSettings clientsApplicationSP,
            ApplicationSettings aksApplication,
            ServicePrincipalSettings aksApplicationSP,
            string aksApplicationRbacSecret
        ) {
            ServicesApplication = servicesApplication;
            ServicesApplicationSP = servicesApplicationSP;
            ClientsApplication = clientsApplication;
            ClientsApplicationSP = clientsApplicationSP;
            AksApplication = aksApplication;
            AksApplicationSP = aksApplicationSP;
            AksApplicationRbacSecret = aksApplicationRbacSecret;
        }

        public ApplicationRegistrationDefinition ToApplicationRegistrationDefinition() {
            var appRegDef = new ApplicationRegistrationDefinition(
                ServicesApplication.ToApplication(),
                ServicesApplicationSP.ToServicePrincipal(),
                ClientsApplication.ToApplication(),
                ClientsApplicationSP.ToServicePrincipal(),
                AksApplication.ToApplication(),
                AksApplicationSP.ToServicePrincipal(),
                AksApplicationRbacSecret
            );

            return appRegDef;
        }
    }
}
