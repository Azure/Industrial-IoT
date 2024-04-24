// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    using Opc.Ua;

    /// <summary>
    /// Represents schemas for the encoding of the built in types
    /// as per part 6 of the OPC UA specification.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseBuiltInSchemas<T>
    {
        /// <summary>
        /// Get schema for built in type
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public abstract T GetSchemaForBuiltInType(
            BuiltInType builtInType, SchemaRank rank = SchemaRank.Scalar);

        /// <summary>
        /// Get a schema for a data value field with the
        /// specified value schema. The union field in the
        /// value variant will then be made a reserved
        /// identifer
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="asDataValue"></param>
        /// <param name="valueSchema"></param>
        /// <returns></returns>
        public abstract T GetSchemaForDataSetField(
            string ns, bool asDataValue, T valueSchema);

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
        public abstract T GetSchemaForExtendableType(
            string name, string ns, string dataTypeId, T bodyType);

        /// <summary>
        /// Get schema for specified value rank
        /// schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public abstract T GetSchemaForRank(T schema, SchemaRank rank);
    }
}
