// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Writer group job model
    /// </summary>
    [DataContract]
    public class WriterGroupJobApiModel {

        /// <summary>
        /// Writer group
        /// </summary>
        [DataMember(Name = "writerGroup")]
        public WriterGroupApiModel WriterGroup { get; set; }

        /// <summary>
        /// Connection string
        /// </summary>
        [DataMember(Name = "connectionString")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Messaging mode - defaults to monitoreditem
        /// </summary>
        [DataMember(Name = "messagingMode",
            EmitDefaultValue = false)]
        public MessagingMode? MessagingMode { get; set; }

        /// <summary>
        /// Publisher engine configuration (publisher extension)
        /// </summary>
        [DataMember(Name = "engine",
            EmitDefaultValue = false)]
        public EngineConfigurationApiModel Engine { get; set; }
    }
}