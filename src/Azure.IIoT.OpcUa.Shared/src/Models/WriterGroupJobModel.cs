// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// PubSub writer group job
    /// </summary>
    [DataContract]
    public sealed record class WriterGroupJobModel
    {
        /// <summary>
        /// Writer group configuration
        /// </summary>
        [DataMember(Name = "writerGroup", Order = 0)]
        public WriterGroupModel? WriterGroup { get; set; }

        /// <summary>
        /// Injected connection string
        /// </summary>
        [DataMember(Name = "connectionString", Order = 1)]
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Messaging mode to use
        /// </summary>
        [DataMember(Name = "messagingMode", Order = 2,
            EmitDefaultValue = false)]
        public MessagingMode? MessagingMode { get; set; }

        /// <summary>
        /// Engine configuration
        /// </summary>
        [DataMember(Name = "engine", Order = 3,
            EmitDefaultValue = false)]
        public EngineConfigurationModel? Engine { get; set; }
    }
}
