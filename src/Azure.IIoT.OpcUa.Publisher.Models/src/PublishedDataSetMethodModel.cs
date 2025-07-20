// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describes methods that can be called and results published
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetMethodModel
    {
        /// <summary>
        /// Identifier of method in the dataset.
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Node id of the method or starting point of browse path
        /// </summary>
        [DataMember(Name = "methodId", Order = 1,
            EmitDefaultValue = false)]
        public string? MethodId { get; set; }

        /// <summary>
        /// Browse path to event notifier node (Publisher extension)
        /// </summary>
        [DataMember(Name = "browsePath", Order = 2,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? BrowsePath { get; set; }

        /// <summary>
        /// Method metadata
        /// </summary>
        [DataMember(Name = "metadata", Order = 3)]
        public MethodMetadataModel? Metadata { get; set; }

        /// <summary>
        /// Simple event Type definition id
        /// </summary>
        [DataMember(Name = "typeDefinitionId", Order = 4)]
        public string? TypeDefinitionId { get; set; }

        /// <summary>
        /// Triggering configuration.
        /// </summary>
        [DataMember(Name = "triggering", Order = 5,
            EmitDefaultValue = false)]
        public PublishedDataSetTriggerModel? Triggering { get; set; }

        /// <summary>
        /// Queue settings writer should use to publish messages
        /// to. Overrides the writer and writer group queue settings.
        /// </summary>
        [DataMember(Name = "publishing", Order = 6,
            EmitDefaultValue = false)]
        public PublishingQueueSettingsModel? Publishing { get; set; }
    }
}
