// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Data set writer message model
    /// </summary>
    [DataContract]
    public class DataSetWriterMessageSettingsApiModel {

        /// <summary>
        /// Dataset message content
        /// </summary>
        [DataMember(Name = "dataSetMessageContentMask",
            EmitDefaultValue = false)]
        public DataSetContentMask? DataSetMessageContentMask { get; set; }

        /// <summary>
        /// Configured size of network message
        /// </summary>
        [DataMember(Name = "configuredSize",
            EmitDefaultValue = false)]
        public ushort? ConfiguredSize { get; set; }

        /// <summary>
        /// Uadp metwork message number
        /// </summary>
        [DataMember(Name = "networkMessageNumber",
            EmitDefaultValue = false)]
        public ushort? NetworkMessageNumber { get; set; }

        /// <summary>
        /// Uadp dataset offset
        /// </summary>
        [DataMember(Name = "dataSetOffset",
            EmitDefaultValue = false)]
        public ushort? DataSetOffset { get; set; }
    }
}
