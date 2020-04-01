// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using Microsoft.Azure.IIoT.Deployment.Deployment;

    class ApplicationRegistrationDefinitionSettings {

        // Details of service application
        public ApplicationSettings ServiceApplication { get; set; }
        public ServicePrincipalSettings ServiceApplicationSP { get; set; }
        public string ServiceApplicationSecret { get; set; }

        // Details of client application
        public ApplicationSettings ClientApplication { get; set; }
        public ServicePrincipalSettings ClientApplicationSP { get; set; }
        public string ClientApplicationSecret { get; set; }

        // Details of aks application
        public ApplicationSettings AksApplication { get; set; }
        public ServicePrincipalSettings AksApplicationSP { get; set; }
        public string AksApplicationSecret { get; set; }

        public ApplicationRegistrationDefinitionSettings() { }

        public ApplicationRegistrationDefinitionSettings(
            ApplicationSettings serviceApplication,
            ServicePrincipalSettings serviceApplicationSP,
            string serviceApplicationSecret,
            ApplicationSettings clientApplication,
            ServicePrincipalSettings clientApplicationSP,
            string clientApplicationSecret,
            ApplicationSettings aksApplication,
            ServicePrincipalSettings aksApplicationSP,
            string aksApplicationSecret
        ) {
            // Details of service application
            ServiceApplication = serviceApplication;
            ServiceApplicationSP = serviceApplicationSP;
            ServiceApplicationSecret = serviceApplicationSecret;

            // Details of client application
            ClientApplication = clientApplication;
            ClientApplicationSP = clientApplicationSP;
            ClientApplicationSecret = clientApplicationSecret;

            // Details of aks application
            AksApplication = aksApplication;
            AksApplicationSP = aksApplicationSP;
            AksApplicationSecret = aksApplicationSecret;
        }

        public ApplicationRegistrationDefinition ToApplicationRegistrationDefinition() {
            var appRegDef = new ApplicationRegistrationDefinition(
                ServiceApplication.ToApplication(),
                ServiceApplicationSP.ToServicePrincipal(),
                ServiceApplicationSecret,
                ClientApplication.ToApplication(),
                ClientApplicationSP.ToServicePrincipal(),
                ClientApplicationSecret,
                AksApplication.ToApplication(),
                AksApplicationSP.ToServicePrincipal(),
                AksApplicationSecret
            );

            return appRegDef;
        }

        /// <summary>
        /// Create ApplicationRegistrationDefinitionSettings from ApplicationRegistrationDefinition.
        /// </summary>
        /// <param name="applicationRegistrationDefinition"></param>
        /// <returns></returns>
        public static ApplicationRegistrationDefinitionSettings FromApplicationRegistrationDefinition(
            ApplicationRegistrationDefinition applicationRegistrationDefinition
        ) {
            if (applicationRegistrationDefinition is null) {
                throw new ArgumentNullException(nameof(applicationRegistrationDefinition));
            }

            var appRegDefSettings = new ApplicationRegistrationDefinitionSettings(
                new ApplicationSettings(applicationRegistrationDefinition.ServiceApplication),
                new ServicePrincipalSettings(applicationRegistrationDefinition.ServiceApplicationSP),
                applicationRegistrationDefinition.ServiceApplicationSecret,
                new ApplicationSettings(applicationRegistrationDefinition.ClientApplication),
                new ServicePrincipalSettings(applicationRegistrationDefinition.ClientApplicationSP),
                applicationRegistrationDefinition.ClientApplicationSecret,
                new ApplicationSettings(applicationRegistrationDefinition.AksApplication),
                new ServicePrincipalSettings(applicationRegistrationDefinition.AksApplicationSP),
                applicationRegistrationDefinition.AksApplicationSecret
            );

            return appRegDefSettings;
        }

        /// <summary>
        /// Validate that all configuration properties are set.
        /// </summary>
        public void Validate(string parentProperty) {
            // If any of ApplicationRegistration properties is provided,
            // then we will require that all are provided.

            // Details of service application
            if (null == ServiceApplication) {
                throw new Exception($"{parentProperty}.{nameof(ServiceApplication)}" +
                    " configuration property is missing.");
            }

            ServiceApplication.Validate($"{parentProperty}.{nameof(ServiceApplication)}");

            if (null == ServiceApplicationSP) {
                throw new Exception($"{parentProperty}.{nameof(ServiceApplicationSP)}" +
                    " configuration property is missing.");
            }

            ServiceApplicationSP.Validate($"{parentProperty}.{nameof(ServiceApplicationSP)}");

            if (string.IsNullOrEmpty(ServiceApplicationSecret)) {
                throw new Exception($"{parentProperty}.{nameof(ServiceApplicationSecret)}" +
                    " configuration property is missing or is empty.");
            }

            // Details of client application
            if (null == ClientApplication) {
                throw new Exception($"{parentProperty}.{nameof(ClientApplication)}" +
                    " configuration property is missing.");
            }

            ClientApplication.Validate($"{parentProperty}.{nameof(ClientApplication)}");

            if (null == ClientApplicationSP) {
                throw new Exception($"{parentProperty}.{nameof(ClientApplicationSP)}" +
                    " configuration property is missing.");
            }

            ClientApplicationSP.Validate($"{parentProperty}.{nameof(ClientApplicationSP)}");

            if (string.IsNullOrEmpty(ClientApplicationSecret)) {
                throw new Exception($"{parentProperty}.{nameof(ClientApplicationSecret)}" +
                    " configuration property is missing or is empty.");
            }

            // Details of aks application
            if (null == AksApplication) {
                throw new Exception($"{parentProperty}.{nameof(AksApplication)}" +
                    " configuration property is missing.");
            }

            AksApplication.Validate($"{parentProperty}.{nameof(AksApplication)}");

            if (null == AksApplicationSP) {
                throw new Exception($"{parentProperty}.{nameof(AksApplicationSP)}" +
                    " configuration property is missing.");
            }

            AksApplicationSP.Validate($"{parentProperty}.{nameof(AksApplicationSP)}");

            if (string.IsNullOrEmpty(AksApplicationSecret)) {
                throw new Exception($"{parentProperty}.{nameof(AksApplicationSecret)}" +
                    " configuration property is missing or is empty.");
            }
        }
    }
}
