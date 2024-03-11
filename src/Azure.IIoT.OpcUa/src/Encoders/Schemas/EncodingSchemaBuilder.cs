// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Avro;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using DataSetFieldFieldMask = Publisher.Models.DataSetFieldContentMask;

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
                return new JsonEncodingSchemaBuilder(fieldMask ?? 0u);
            }

            throw new NotSupportedException("Encoding not yet supported");
        }

        /// <summary>
        /// Get schema for built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="nullable"></param>
        /// <returns></returns>
        public abstract Schema GetSchemaForBuiltInType(
            BuiltInType builtInType, bool nullable = false);

        /// <summary>
        /// Get a schema for a data value field with the specified
        /// value schema
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        public abstract Schema GetDataValueFieldSchema(
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

        /// <summary>
        /// Get variant field
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="bodyType"></param>
        /// <returns></returns>
        public abstract Schema GetVariantField(string name, string ns,
            string dataTypeId, Schema bodyType);

        /// <summary>
        /// Get schema for type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual Schema GetSchemaForEncodeableType<T>()
            where T : IEncodeable
        {
            // TODO
            return AvroUtils.Null;
        }
    }
}
