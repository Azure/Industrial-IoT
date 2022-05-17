// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an event entry in the configuration.
    /// </summary>
    [DataContract]
    public class DataFilterModel {

        /// <summary>
        /// Simple event Type id
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Deadband { get; set; }

        // TODO: INCOMPLETE add the rest of the properties!
    }
}
