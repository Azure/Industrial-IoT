// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Avro
{
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using global::Avro;
    using Opc.Ua;
    using DataSetFieldFieldMask = Publisher.Models.DataSetFieldContentMask;
    using System;

    /// <summary>
    /// Represents a encoding as per part 6 of the OPC UA specification
    /// </summary>
    internal abstract class EncodingSchemaBuilder
    {
        /// <summary>
        /// Get the respective encoding
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="fieldMask"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static EncodingSchemaBuilder GetEncoding(MessageEncoding? encoding,
            DataSetFieldFieldMask? fieldMask)
        {
            if (encoding?.HasFlag(MessageEncoding.Json) != false)
            {
                return new JsonBuiltInTypeSchemas(fieldMask ?? 0u);
            }
            if (encoding?.HasFlag(MessageEncoding.Avro) != false)
            {
                return new AvroBuiltInTypeSchemas();
            }
            throw new NotSupportedException("Encoding not yet supported");
        }

        /// <summary>
        /// Get schema for built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="nullable"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public abstract Schema GetSchemaForBuiltInType(
            BuiltInType builtInType, bool nullable = false,
            bool array = false);

        /// <summary>
        /// Get a schema for a data value field with the
        /// specified value schema. The union field in the
        /// value variant will then be made a reserved
        /// identifer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        public abstract Schema GetDataValueFieldSchema(
            string name, Schema valueSchema);

        /// <summary>
        /// Get a schema for a variant field with the
        /// specified schema. The union field in the
        /// variant will then be made a reserved identifer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        public abstract Schema GetVariantFieldSchema(
            string name, Schema valueSchema);

        /// <summary>
        /// Get extension object schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="bodyType"></param>
        /// <returns></returns>
        public abstract Schema GetExtensionObjectSchema(string name,
            string ns, string dataTypeId, Schema bodyType);
    }
}
