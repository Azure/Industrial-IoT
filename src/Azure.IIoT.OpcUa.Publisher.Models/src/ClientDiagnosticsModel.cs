// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Client diagnostics
    /// </summary>
    [DataContract]
    public record class ClientDiagnosticsModel
    {
        /// <summary>
        /// The session diagnostics from the server
        /// </summary>
        [DataMember(Name = "server", Order = 0,
            EmitDefaultValue = false)]
        public SessionDiagnosticsModel? Server { get; init; }

        /// <summary>
        /// The connection information specified by user.
        /// </summary>
        [DataMember(Name = "connection", Order = 2)]
        public required ConnectionModel Connection { get; init; }
    }
}
