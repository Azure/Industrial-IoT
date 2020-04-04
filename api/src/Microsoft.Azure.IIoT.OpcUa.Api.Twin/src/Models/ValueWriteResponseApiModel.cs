// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Value write response model
    /// </summary>
    [DataContract]
    public class ValueWriteResponseApiModel {

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 0,
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
