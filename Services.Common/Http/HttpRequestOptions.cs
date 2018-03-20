// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Http {

    public class HttpRequestOptions {

        /// <summary>
        /// No cert validation
        /// </summary>
        public bool AllowInsecureSSLServer { get; set; } = false;

        /// <summary>
        /// Request timeout
        /// </summary>
        public int Timeout { get; set; } = 30000;
    }
}
