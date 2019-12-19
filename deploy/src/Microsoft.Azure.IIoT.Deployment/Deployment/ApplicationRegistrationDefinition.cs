// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using Microsoft.Graph;

    class ApplicationRegistrationDefinition {

        public Application ServiceApplication { get; }
        public ServicePrincipal ServiceApplicationSP { get; }
        public Application ClientApplication { get; }
        public ServicePrincipal ClientApplicationSP { get; }
        public Application AksApplication { get; }
        public ServicePrincipal AksApplicationSP { get; }
        public string AksApplicationRbacSecret { get; }

        public ApplicationRegistrationDefinition(
            Application serviceApplication,
            ServicePrincipal serviceApplicationSP,
            Application clientApplication,
            ServicePrincipal clientApplicationSP,
            Application aksApplication,
            ServicePrincipal aksApplicationSP,
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
    }
}
