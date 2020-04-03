// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of an application registration
    /// </summary>
    [DataContract]
    public class ApplicationRegistrationResponseApiModel {

        /// <summary>
        /// New id application was registered under
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public string Id { get; set; }
    }
}
