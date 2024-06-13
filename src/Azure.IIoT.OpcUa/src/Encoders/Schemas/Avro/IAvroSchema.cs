// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas.Avro
{
    using global::Avro;

    /// <summary>
    /// Avro schema
    /// </summary>
    public interface IAvroSchema
    {
        /// <summary>
        /// The avro schema
        /// </summary>
        Schema Schema { get; }
    }
}
