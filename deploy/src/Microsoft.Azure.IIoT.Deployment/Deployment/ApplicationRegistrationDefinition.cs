// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using Microsoft.Graph;

    class ApplicationRegistrationDefinition {

        public Application ServicesApplication { get; }
        public ServicePrincipal ServicesApplicationSP { get; }
        public Application ClientsApplication { get; }
        public ServicePrincipal ClientsApplicationSP { get; }
        public Application AksApplication { get; }
        public ServicePrincipal AksApplicationSP { get; }
        public string AksApplicationRbacSecret { get; }

        public ApplicationRegistrationDefinition(
            Application servicesApplication,
            ServicePrincipal servicesApplicationSP,
            Application clientsApplication,
            ServicePrincipal clientsApplicationSP,
            Application aksApplication,
            ServicePrincipal aksApplicationSP,
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
    }
}
