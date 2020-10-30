// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Node id serialized as object
    /// </summary>
    [DataContract]
    public class NodeIdModel {

        /// <summary> Identifier </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Identifier { get; set; }
    }
}
