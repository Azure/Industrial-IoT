// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    /// <summary>
    /// The member names a JSON network message and data set message carry on
    /// the wire, and the type-name suffixes the generated schemas use.
    /// </summary>
    /// <remarks>
    /// A generated schema is only useful if it names the members the runtime
    /// actually writes, so these are stated here rather than derived from an
    /// encoder type. They used to be taken with <c>nameof</c> from the custom
    /// encoder's message classes, which tied schema generation to an
    /// implementation that 3.0 removed and, worse, made the schema silently
    /// wrong the moment the two diverged - which is exactly what happened to
    /// the writer group member.
    /// </remarks>
    internal static class PubSubMessageMembers
    {
        /// <summary>Network message identifier.</summary>
        public const string MessageId = "MessageId";

        /// <summary>Network and data set message type discriminator.</summary>
        public const string MessageType = "MessageType";

        /// <summary>Publisher identity.</summary>
        public const string PublisherId = "PublisherId";

        /// <summary>Data set class identity.</summary>
        public const string DataSetClassId = "DataSetClassId";

        /// <summary>
        /// The writer group a network message belongs to.
        /// </summary>
        /// <remarks>
        /// 3.0 publishes this as <c>WriterGroupName</c>. Up to 2.x the custom
        /// encoder wrote the same member as <c>DataSetWriterGroup</c>, and the
        /// generated schema took that spelling from it, so a consumer
        /// validating 3.0 telemetry against a 2.x schema sees an unexpected
        /// member and a missing required one.
        /// </remarks>
        public const string WriterGroupName = "WriterGroupName";

        /// <summary>The data set messages a network message carries.</summary>
        public const string Messages = "Messages";

        /// <summary>The fields a data set message carries.</summary>
        public const string Payload = "Payload";

        /// <summary>Type-name suffix for a generated network message schema.</summary>
        public const string NetworkMessageTypeName = "NetworkMessage";

        /// <summary>Type-name suffix for a generated data set message schema.</summary>
        public const string DataSetMessageTypeName = "DataSetMessage";
    }
}
