// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client.ComplexTypes
{
    using System;

    /// <summary>
    /// Interface to build property fields.
    /// </summary>
    public interface IComplexTypeFieldBuilder
    {
        /// <summary>
        /// Build the StructureTypeId attribute for a complex type.
        /// </summary>
        /// <param name="complexTypeId"></param>
        /// <param name="binaryEncodingId"></param>
        /// <param name="xmlEncodingId"></param>
        void AddTypeIdAttribute(ExpandedNodeId complexTypeId,
            ExpandedNodeId binaryEncodingId, ExpandedNodeId xmlEncodingId);

        /// <summary>
        /// Create a property field of a class with get and set.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="fieldType"></param>
        /// <param name="order"></param>
        void AddField(StructureField field, Type fieldType, int order);

        /// <summary>
        /// The type of the structure of the field.
        /// </summary>
        /// <param name="valueRank"></param>
        Type GetStructureType(int valueRank);

        /// <summary>
        /// Finish the type creation and returns the new type.
        /// </summary>
        Type CreateType();
    }
}
