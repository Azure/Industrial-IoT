// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// The certificate request states.
    /// </summary>
    [DataContract]
    public enum CertificateRequestState {

        /// <summary>
        /// The request is new.
        /// </summary>
        [EnumMember]
        New,

        /// <summary>
        /// The request was approved.
        /// </summary>
        [EnumMember]
        Approved,

        /// <summary>
        /// The request was rejected.
        /// </summary>
        [EnumMember]
        Rejected,

        /// <summary>
        /// The request failed
        /// </summary>
        [EnumMember]
        Failure,

        /// <summary>
        /// The request is finished.
        /// </summary>
        [EnumMember]
        Completed,

        /// <summary>
        /// The client has accepted result
        /// </summary>
        [EnumMember]
        Accepted
    }
}
