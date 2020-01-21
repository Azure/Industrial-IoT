// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Models {

    /// <summary>
    /// Cors whitelist
    /// </summary>
    public class CorsWhitelistModel {

        /// <summary>
        /// Origins
        /// </summary>
        public string[] Origins { get; set; }

        /// <summary>
        /// Methods
        /// </summary>
        public string[] Methods { get; set; }

        /// <summary>
        /// Headers
        /// </summary>
        public string[] Headers { get; set; }
    }
}
