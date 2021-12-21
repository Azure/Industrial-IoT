// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    

    /// <summary>
    /// PublishNodes direct method response
    /// </summary>
    [DataContract]
    public class PublishedNodesResponseApiModel {

        /// <summary>
        /// Status messages
        /// </summary>
        [DataMember(Name = "statusMessage", Order = 0)]
        [Required]
        public List<string> StatusMessage { get; set; }

    }
}
