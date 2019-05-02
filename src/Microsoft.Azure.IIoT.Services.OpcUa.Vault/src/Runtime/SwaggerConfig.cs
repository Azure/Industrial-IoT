// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime
{
    public class SwaggerConfig
    {
        /// <summary>
        /// Swagger configuration
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>Application id</summary>
        public string AppId { get; set; }
        /// <summary>Application key</summary>
        public string AppSecret { get; set; }
        /// <summary>Allow Http scheme</summary>
        public bool WithHttpScheme { get; set; }
    }
}
