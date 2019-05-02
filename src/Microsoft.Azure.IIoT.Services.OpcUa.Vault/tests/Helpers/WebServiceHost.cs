// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Test.Helpers
{
    public class WebServiceHost
    {
        public static string GetBaseAddress()
        {
            int port = new Random().Next(40000, 60000);
            string baseAddress = "http://127.0.0.1:" + port;
            return baseAddress;
        }
    }
}
