// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Avro
{
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using System.Collections.Generic;

    /// <summary>
    /// Create schem extension
    /// </summary>
    internal static class AvroSchemaEx
    {
        /// <summary>
        /// Create nullable
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        internal static Schema AsNullable(this Schema schema)
        {
            return schema == AvroUtils.Null ? schema :
                UnionSchema.Create(new List<Schema>
                {
                    AvroUtils.Null,
                    schema
                });
        }
    }
}
