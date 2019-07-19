// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Tests.Helpers {
    using System;
    public static class WebServiceHost {
        public static string GetBaseAddress() {
            var port = new Random().Next(40000, 60000);
            var baseAddress = "http://127.0.0.1:" + port;
            return baseAddress;
        }
    }
}
