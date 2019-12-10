// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;

    class ApplicationRegistrationSettings {
        public Guid? ClientsApplicationId { get; set; }
        public Guid? ServicesApplicationId { get; set; }
        public Guid? AksApplicatoinId { get; set; }
        public string AksApplicatoinRbacSecret { get; set; }
    }
}
