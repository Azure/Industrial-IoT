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
    /// Schema type extensions
    /// </summary>
    public static class SchemaTypesEx {

        /// <summary>
        /// Maps the access level enumeration onto a byte.
        /// </summary>
        public static byte ToStackValue(this AccessLevel accessLevel) {
            switch (accessLevel) {
                case AccessLevel.Read:
                    return AccessLevels.CurrentRead;
                case AccessLevel.Write:
                    return AccessLevels.CurrentWrite;
                case AccessLevel.ReadWrite:
                    return AccessLevels.CurrentReadOrWrite;
            }
            return AccessLevels.None;
        }

        /// <summary>
        /// Maps the modelling rule enumeration onto a string.
        /// </summary>
        public static NodeId ToNodeId(this ModellingRule modellingRule) {
            switch (modellingRule) {
                case ModellingRule.Mandatory:
                    return Objects.ModellingRule_Mandatory;
                case ModellingRule.MandatoryShared:
                    return 79; // TODO
                case ModellingRule.Optional:
                    return Objects.ModellingRule_Optional;
                case ModellingRule.MandatoryPlaceholder:
                    return Objects.ModellingRule_MandatoryPlaceholder;
                case ModellingRule.OptionalPlaceholder:
                    return Objects.ModellingRule_OptionalPlaceholder;
                case ModellingRule.ExposesItsArray:
                    return Objects.ModellingRule_ExposesItsArray;
            }
            return null;
        }

        /// <summary>
        /// Convert value rank to stack value
        /// </summary>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        public static int ToStackValue(this ValueRank valueRank) {
            var (rank, _) = valueRank.ToStackValue(null);
            return rank;
        }

        /// <summary>
        /// Convert value rank and dimensions to stack values
        /// </summary>
        /// <param name="valueRank"></param>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        public static (int, UInt32Collection) ToStackValue(this ValueRank valueRank,
            string arrayDimensions) {
            switch (valueRank) {
                case ValueRank.Array:
                    return (ValueRanks.OneDimension, null);
                case ValueRank.Scalar:
                    return (ValueRanks.Scalar, null);
                case ValueRank.ScalarOrArray:
                    return (ValueRanks.ScalarOrOneDimension, null);
                case ValueRank.OneOrMoreDimensions:
                    if (string.IsNullOrEmpty(arrayDimensions)) {
                        return (ValueRanks.OneOrMoreDimensions, null);
                    }
                    var tokens = arrayDimensions.Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens == null || tokens.Length < 1) {
                        return (ValueRanks.OneOrMoreDimensions, null);
                    }
                    var dimensions = new UInt32Collection();
                    foreach (var dim in tokens) {
                        try {
                            dimensions.Add(Convert.ToUInt32(dim));
                        }
                        catch {
                            dimensions.Add(0);
                        }
                    }
                    return (dimensions.Count, dimensions);
                default:
                    return (ValueRanks.Any, null);
            }
        }
    }
}

