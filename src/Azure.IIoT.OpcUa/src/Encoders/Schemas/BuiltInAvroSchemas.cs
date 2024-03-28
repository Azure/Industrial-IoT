// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using System;
    using Opc.Ua;
    using DataSetFieldFieldMask = Publisher.Models.DataSetFieldContentMask;

    /// <summary>
    /// Represents schemas for the encoding of the built in types
    /// as per part 6 of the OPC UA specification.
    /// </summary>
    internal abstract class BuiltInAvroSchemas
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

        /// <summary>
        /// Get schema for built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public abstract Schema GetSchemaForBuiltInType(
            BuiltInType builtInType, int rank = ValueRanks.Scalar);

        /// <summary>
        /// Get a schema for a data value field with the
        /// specified value schema. The union field in the
        /// value variant will then be made a reserved
        /// identifer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="asDataValue"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        public abstract Schema GetSchemaForDataSetField(
            string name, bool asDataValue, Schema valueSchema);

        /// <summary>
        /// Get the schema definition for a type that can
        /// be any type in a hierarchy extension object
        /// schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="bodyType"></param>
        /// <returns></returns>
        public abstract Schema GetSchemaForExntendableType(
            string name, string ns, string dataTypeId,
            Schema bodyType);
    }
}
