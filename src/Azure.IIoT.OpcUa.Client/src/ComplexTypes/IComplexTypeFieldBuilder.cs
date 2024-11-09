/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

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
