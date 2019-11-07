// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;

    class AzureAppsConstants {
        public class AzureKeyVault {
            public const string AppId = "cfa8b339-82a2-471a-a3c9-0fc0be7a4093";

            public static readonly Dictionary<string, Guid> ResourceAccess = new Dictionary<string, Guid> {
                    {"user_impersonation", new Guid("f53da476-18e3-4152-8e01-aec403e6edc0") }
            };
        }

        public class MicrosoftGraph {
            public const string AppId = "00000003-0000-0000-c000-000000000000";

            public static readonly Dictionary<string, Guid> ResourceAccess = new Dictionary<string, Guid> {
                    {"User.Read", new Guid("e1fe6dd8-ba31-4d61-89e7-88639da4683d") }
            };

        }
    }
}
