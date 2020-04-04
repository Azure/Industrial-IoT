// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using Microsoft.Graph;

    class ApplicationRegistrationDefinition {

        // Details of service application
        public Application ServiceApplication { get; }
        public ServicePrincipal ServiceApplicationSP { get; }
        public string ServiceApplicationSecret { get; }

        // Details of client application
        public Application ClientApplication { get; }
        public ServicePrincipal ClientApplicationSP { get; }
        public string ClientApplicationSecret { get; }

        // Details of aks application
        public Application AksApplication { get; }
        public ServicePrincipal AksApplicationSP { get; }
        public string AksApplicationSecret { get; }

        public ApplicationRegistrationDefinition(
            Application serviceApplication,
            ServicePrincipal serviceApplicationSP,
            string serviceApplicationSecret,
            Application clientApplication,
            ServicePrincipal clientApplicationSP,
            string clientApplicationSecret,
            Application aksApplication,
            ServicePrincipal aksApplicationSP,
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
    }
}
