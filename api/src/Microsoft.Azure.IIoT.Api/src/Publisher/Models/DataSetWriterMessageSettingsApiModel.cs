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
        [DataMember(Name = "dataSetMessageContentMask", Order = 0,
            EmitDefaultValue = false)]
        public DataSetContentMask? DataSetMessageContentMask { get; set; }

        /// <summary>
        /// Configured size of network message
        /// </summary>
        [DataMember(Name = "configuredSize", Order = 1,
            EmitDefaultValue = false)]
        public ushort? ConfiguredSize { get; set; }

        /// <summary>
        /// Uadp metwork message number
        /// </summary>
        [DataMember(Name = "networkMessageNumber", Order = 2,
            EmitDefaultValue = false)]
        public ushort? NetworkMessageNumber { get; set; }

        /// <summary>
        /// Uadp dataset offset
        /// </summary>
        [DataMember(Name = "dataSetOffset", Order = 3,
            EmitDefaultValue = false)]
        public ushort? DataSetOffset { get; set; }
    }
}
