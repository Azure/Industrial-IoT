// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Query continuation
    /// </summary>
    public class QueryContinuation {

        /// <summary>
        /// Original query string
        /// </summary>
        [DataMember(Name = "q")]
        public string Query { get; set; }

        /// <summary>
        /// Continuation token
        /// </summary>
        [DataMember(Name = "t")]
        public string Token { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        [DataMember(Name = "s")]
        public int? PageSize { get; set; }
    }
}
