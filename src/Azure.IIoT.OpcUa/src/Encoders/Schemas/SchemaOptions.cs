// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Opc.Ua;

    /// <summary>
    /// Options for the schema generation
    /// </summary>
    public record class SchemaOptions
    {
        /// <summary>
        /// Namespace to use as root for the schema
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Escape field names and symbols in schema
        /// </summary>
        public bool EscapeSymbols { get; set; }

        /// <summary>
        /// Namespace table
        /// </summary>
        public NamespaceTable? Namespaces { get; set; }

        /// <summary>
        /// Prefer generating avro schema over json schema
        /// </summary>
        public bool? PreferAvroOverJsonSchema { get; set; }
    }
}
