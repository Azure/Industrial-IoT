// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using Microsoft.Azure.IIoT.Deployment.Deployment;

    class ApplicationRegistrationDefinitionSettings {

        public ApplicationSettings ServiceApplication { get; set; }
        public ServicePrincipalSettings ServiceApplicationSP { get; set; }
        public ApplicationSettings ClientApplication { get; set; }
        public ServicePrincipalSettings ClientApplicationSP { get; set; }
        public ApplicationSettings AksApplication { get; set; }
        public ServicePrincipalSettings AksApplicationSP { get; set; }
        public string AksApplicationRbacSecret { get; set; }

        public ApplicationRegistrationDefinitionSettings() { }

        public ApplicationRegistrationDefinitionSettings(
            ApplicationSettings serviceApplication,
            ServicePrincipalSettings serviceApplicationSP,
            ApplicationSettings clientApplication,
            ServicePrincipalSettings clientApplicationSP,
            ApplicationSettings aksApplication,
            ServicePrincipalSettings aksApplicationSP,
            string aksApplicationRbacSecret
        ) {
            ServiceApplication = serviceApplication;
            ServiceApplicationSP = serviceApplicationSP;
            ClientApplication = clientApplication;
            ClientApplicationSP = clientApplicationSP;
            AksApplication = aksApplication;
            AksApplicationSP = aksApplicationSP;
            AksApplicationRbacSecret = aksApplicationRbacSecret;
        }

        public ApplicationRegistrationDefinition ToApplicationRegistrationDefinition() {
            var appRegDef = new ApplicationRegistrationDefinition(
                ServiceApplication.ToApplication(),
                ServiceApplicationSP.ToServicePrincipal(),
                ClientApplication.ToApplication(),
                ClientApplicationSP.ToServicePrincipal(),
                AksApplication.ToApplication(),
                AksApplicationSP.ToServicePrincipal(),
                AksApplicationRbacSecret
            );

            return appRegDef;
        }

        /// <summary>
        /// Validate that all configuration properties are set.
        /// </summary>
        public void Validate() {
            // If any of ApplicationRegistration properties is provided,
            // then we will require that all are provided.

            if (null == ServiceApplication) {
                throw new Exception("ApplicationRegistration.ServiceApplication" +
                    " configuration property is missing.");
            }

            if (null == ServiceApplicationSP) {
                throw new Exception("ApplicationRegistration.ServiceApplicationSP" +
                    " configuration property is missing.");
            }

            if (null == ClientApplication) {
                throw new Exception("ApplicationRegistration.ClientApplication" +
                    " configuration property is missing.");
            }

            if (null == ClientApplicationSP) {
                throw new Exception("ApplicationRegistration.ClientApplicationSP" +
                    " configuration property is missing.");
            }

            if (null == AksApplication) {
                throw new Exception("ApplicationRegistration.AksApplication" +
                    " configuration property is missing.");
            }

            if (null == AksApplicationSP) {
                throw new Exception("ApplicationRegistration.AksApplicationSP" +
                    " configuration property is missing.");
            }

            if (string.IsNullOrEmpty(AksApplicationRbacSecret)) {
                throw new Exception("ApplicationRegistration.AksApplicationRbacSecret" +
                    " configuration property is missing or is empty.");
            }
        }
    }
}
