// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using Microsoft.Azure.IIoT.Deployment.Deployment;

    class AppSettings {
        public RunMode? RunMode { get; set; }
        public AuthenticationSettings Auth { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationURL { get; set; }
        public ResourceGroupSettings ResourceGroup { get; set; }
        public ApplicationRegistrationDefinitionSettings ApplicationRegistration { get; set; }
        public bool? SaveEnvFile { get; set; }
        public bool? NoCleanup { get; set; }
    }
}
