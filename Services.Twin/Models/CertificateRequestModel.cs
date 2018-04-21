// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {

    public class CertificateRequestModel {

        /// <summary>
        /// Request identifier
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// State of the request
        /// </summary>
        public int? State { get; set; }

        /// <summary>
        /// Certificate
        /// </summary>
        public byte[] Certificate { get; set; }

        /// <summary>
        /// Prive key
        /// </summary>
        public byte[] PrivateKey { get; set; }

        /// <summary>
        /// Authority id
        /// </summary>
        public string AuthorityId { get; set; }
    }
}

