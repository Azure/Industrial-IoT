// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using System;
    using DataSetFieldFieldMask = Publisher.Models.DataSetFieldContentMask;

    /// <summary>
    /// Represents schemas for the encoding of the built in types
    /// as per part 6 of the OPC UA specification.
    /// </summary>
    internal abstract class BuiltInAvroSchemas : BaseBuiltInSchemas<Schema>
    {
        /// <summary>
        /// Get the respective encoding
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="fieldMask"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static BuiltInAvroSchemas GetEncodingSchemas(
            MessageEncoding? encoding, DataSetFieldFieldMask? fieldMask)
        {
            if (encoding?.HasFlag(MessageEncoding.Json) != false)
            {
                return new JsonAvroSchemas(fieldMask ?? 0u);
            }
            if (encoding?.HasFlag(MessageEncoding.Avro) != false)
            {
                return new AvroBinarySchemas();
            }
            throw new NotSupportedException(
                $"Encoding {encoding} not yet supported!");
        }
    }
}
