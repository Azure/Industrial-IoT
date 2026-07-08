// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for writer diagnostic info.
    /// </summary>
    [DataContract]
    public record class DataSetWriterStateDiagnosticModel
    {
        /// <summary>
        /// Dataset writer identifier.
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        public required string Id { get; set; }

        /// <summary>
        /// Dataset writer name.
        /// </summary>
        [DataMember(Name = "dataSetWriterName", Order = 5)]
        public string? DataSetWriterName { get; set; }

        /// <summary>
        /// The endpoint url of the server the writer is
        /// connecting to.
        /// </summary>
        [DataMember(Name = "endpointUrl", Order = 6,
            EmitDefaultValue = false)]
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Whether the connection to the endpoint uses
        /// a secure channel.
        /// </summary>
        [DataMember(Name = "useSecurity", Order = 7,
            EmitDefaultValue = false)]
        public bool? UseSecurity { get; set; }

        /// <summary>
        /// The authentication mode used to connect to the
        /// endpoint.
        /// </summary>
        [DataMember(Name = "opcAuthenticationMode", Order = 8,
            EmitDefaultValue = false)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// The user name used to connect to the endpoint in
        /// case of user name and password authentication.
        /// </summary>
        [DataMember(Name = "opcAuthenticationUsername", Order = 9,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationUsername { get; set; }

        /// <summary>
        /// Diagnostics for the source of the dataset
        /// </summary>
        [DataMember(Name = "source", Order = 3,
            EmitDefaultValue = true)]
        public PublishedDataSetSourceDiagnosticModel? Source { get; set; }
    }
}
