/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Design.Schema {
    using System;

    /// <summary>
    /// Type design extensions
    /// </summary>
    public static class TypeDesignEx {

        /// <summary>
        /// Recursive merge of object and variable types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeDesign GetMergedType(this TypeDesign type) {
            if (type is ReferenceTypeDesign || type is DataTypeDesign) {
                return type;
            }
            if (type.BaseTypeNode == null) {
                // Make copy
                var copy = type.Copy();
                copy.NumericId = 0;
                copy.NumericIdSpecified = false;
                copy.StringId = null;
                return copy;
            }
            // Recursively merge from base type up to us
            var mergedType = GetMergedType(type.BaseTypeNode);
            mergedType.MergeIn(type);
            return mergedType;
        }

        /// <summary>
        /// Merge with type design
        /// </summary>
        /// <param name="type"></param>
        /// <param name="merge"></param>
        private static void MergeIn(this TypeDesign type, TypeDesign merge) {

            type.SymbolicId = merge.SymbolicId;
            type.SymbolicName = merge.SymbolicName;
            type.NumericId = merge.NumericId;
            type.NumericIdSpecified = merge.NumericIdSpecified;
            type.StringId = merge.StringId;
            type.ClassName = merge.ClassName;
            type.BrowseName = merge.BrowseName;
            type.DisplayName = merge.DisplayName;
            type.Description = merge.Description;
            type.BaseType = merge.BaseType;
            type.BaseTypeNode = merge.BaseTypeNode;
            type.IsAbstract = merge.IsAbstract;
            type.Children = null;
            type.References = null;
            type.Category = merge.Category;
            type.Purpose = merge.Purpose;
            type.ReleaseStatus = merge.ReleaseStatus;

            switch (merge) {
                case VariableTypeDesign variableType:
                    type.MergeIn(variableType);
                    break;
                case ObjectTypeDesign objectType:
                    type.MergeIn(objectType);
                    break;
            }
        }

        /// <summary>
        /// Merge in another veriable type design
        /// </summary>
        /// <param name="type"></param>
        /// <param name="merge"></param>
        private static void MergeIn(this TypeDesign type, VariableTypeDesign merge) {
            if (!(type is VariableTypeDesign varType)) {
                throw new FormatException(nameof(merge));
            }
            if (merge.DecodedValue != null) {
                varType.DecodedValue = merge.DecodedValue;
            }
            if (merge.DataType != null &&
                merge.DataType != Constants.BaseDataType) {
                varType.DataType = merge.DataType;
                varType.DataTypeNode = merge.DataTypeNode;
            }
            if (merge.ValueRankSpecified) {
                varType.ValueRank = merge.ValueRank;
                varType.ValueRankSpecified = true;
            }
            if (!string.IsNullOrEmpty(merge.ArrayDimensions)) {
                varType.ArrayDimensions = merge.ArrayDimensions;
            }
            if (merge.AccessLevelSpecified) {
                varType.AccessLevel = merge.AccessLevel;
                varType.AccessLevelSpecified = true;
            }
            if (merge.MinimumSamplingIntervalSpecified) {
                varType.MinimumSamplingInterval = merge.MinimumSamplingInterval;
                varType.MinimumSamplingIntervalSpecified = true;
            }
            if (merge.HistorizingSpecified) {
                varType.Historizing = merge.Historizing;
                varType.HistorizingSpecified = true;
            }
        }

        /// <summary>
        /// Merge in object type design
        /// </summary>
        /// <param name="type"></param>
        /// <param name="merge"></param>
        private static void MergeIn(this TypeDesign type, ObjectTypeDesign merge) {
            if (!(type is ObjectTypeDesign objType)) {
                throw new FormatException(nameof(merge));
            }
            if (merge.SupportsEventsSpecified) {
                objType.SupportsEvents = merge.SupportsEvents;
                objType.SupportsEventsSpecified = true;
            }
        }
    }
}

