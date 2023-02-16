// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Engine {
    using Newtonsoft.Json;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method call exception model.
    /// </summary>
    [DataContract]
    public class MethodCallStatusExceptionModel {

        /// <summary>
        /// Exception message.
        /// </summary>
        [DataMember(Name = "Message", Order = 0,
            EmitDefaultValue = true)]
        public string Message { get; set; }

        /// <summary>
        /// Details of the exception.
        /// </summary>
        [DataMember(Name = "Details", Order = 1,
            EmitDefaultValue = true)]
        [JsonConverter(typeof(Tests.Utils.NewtonSoftJsonSerializerRaw.RawJsonConverter))]
        public string Details { get; set; }
    }
}
