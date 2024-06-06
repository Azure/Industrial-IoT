// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Schemas
{
    /// <summary>
    /// Schema rank
    /// </summary>
    public enum SchemaRank
    {
        /// <summary>
        /// Schema is scalar
        /// </summary>
        Scalar = 0,

        /// <summary>
        /// Schema is array
        /// </summary>
        Collection = 1,

        /// <summary>
        /// Schema is matrix
        /// </summary>
        Matrix = 2,
    }
}
