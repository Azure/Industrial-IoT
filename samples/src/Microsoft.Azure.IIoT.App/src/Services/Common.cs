// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Common {
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Components;

    public class UICommon : ComponentBase {
        public string ExtractSecurityPolicy(string policy) {;
            return policy[(policy.LastIndexOf("#") + 1)..policy.Length];
        }

        public int PageLength { get; set; } = 10;
        public int PageLengthSmall { get; set; } = 4;
        public string None { get; set; } = "(None)";
        public string CredentialKey { get; } = "credential";
        public Dictionary<string, string> ApplicationUri { get; set; } = new Dictionary<string, string>();
    }
}
