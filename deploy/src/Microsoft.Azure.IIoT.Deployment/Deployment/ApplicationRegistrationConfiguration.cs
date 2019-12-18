// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Deployment {

    using System;

    class ApplicationRegistrationConfiguration {

        public Guid ServicesApplicationId { get; }
        public Guid ClientsApplicationId { get; }
        public Guid AksApplicationId { get; }
        public string AksApplicationRbacSecret { get; }

        public ApplicationRegistrationConfiguration(
            Guid servicesApplicationId,
            Guid clientsApplicationId,
            Guid aksApplicationId,
            string aksApplicationRbacSecret
        ) {
            ServicesApplicationId = servicesApplicationId;
            ClientsApplicationId = clientsApplicationId;
            AksApplicationId = aksApplicationId;
            AksApplicationRbacSecret = aksApplicationRbacSecret;
        }
    }
}
