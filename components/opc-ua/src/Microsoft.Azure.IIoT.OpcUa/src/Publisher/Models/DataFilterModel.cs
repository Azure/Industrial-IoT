// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an data related filter for the OpcNode
    /// </summary>
    [DataContract]
    public class DataFilterModel {

        // TODO: INCOMPLETE add the rest of the properties!

        /// <summary>
        /// Deadband filter 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Deadband { get; set; }
    }
}
