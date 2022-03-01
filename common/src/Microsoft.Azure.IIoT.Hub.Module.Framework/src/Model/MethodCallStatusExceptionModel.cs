// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Module.Framework.Hosting {
    using Newtonsoft.Json;
    using System.Runtime.Serialization;
    using static Microsoft.Azure.IIoT.Serializers.NewtonSoft.NewtonSoftJsonSerializer;

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
        /// Description.
        /// </summary>
        [DataMember(Name = "Description", Order = 1,
            EmitDefaultValue = true)]
        [JsonConverter(typeof(RawJsonConverter))]
        public string Description { get; set; }
    }
}
