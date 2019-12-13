// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using System;

    class ApplicationRegistrationConfiguration {

        public Guid ServicesApplicationId { get; }
        public Guid ClientsApplicationId { get; }
        public Guid AksApplicatoinId { get; }
        public string AksApplicatoinRbacSecret { get; }

        public ApplicationRegistrationConfiguration(
            Guid servicesApplicationId,
            Guid clientsApplicationId,
            Guid aksApplicatoinId,
            string aksApplicatoinRbacSecret
        ) {
            ServicesApplicationId = servicesApplicationId;
            ClientsApplicationId = clientsApplicationId;
            AksApplicatoinId = aksApplicatoinId;
            AksApplicatoinRbacSecret = aksApplicatoinRbacSecret;
        }
    }
}
