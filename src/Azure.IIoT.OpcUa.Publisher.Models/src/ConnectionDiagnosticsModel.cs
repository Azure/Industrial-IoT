// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Connection diagnostics
    /// </summary>
    [DataContract]
    public record class ConnectionDiagnosticsModel
    {
        /// <summary>
        /// The connection information specified by user.
        /// </summary>
        [DataMember(Name = "connection", Order = 0)]
        public required ConnectionModel Connection { get; init; }

        /// <summary>
        /// The session and subscriptions diagnostics from
        /// the server.
        /// </summary>
        [DataMember(Name = "server", Order = 1,
            EmitDefaultValue = false)]
        public SessionDiagnosticsModel? Server { get; init; }
    }
}
