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

namespace Opc.Ua.Client.ComplexTypes.Reflection
{
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Complex type property info.
    /// </summary>
    public sealed class ComplexTypePropertyInfo
    {
        /// <summary>
        /// Property info
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Optional property
        /// </summary>
        public bool IsOptional
            => FieldAttribute.IsOptional;

        /// <summary>
        /// Value rank
        /// </summary>
        public int ValueRank
            => FieldAttribute.ValueRank;

        /// <summary>
        /// Built in
        /// </summary>
        public BuiltInType BuiltInType
            => (BuiltInType)FieldAttribute.BuiltInType;

        /// <summary>
        /// Order
        /// </summary>
        public int Order
            => DataAttribute.Order;

        /// <summary>
        /// Optional mask for the field in the property.
        /// </summary>
        public uint OptionalFieldMask { get; set; }

        /// <summary>
        /// The structure field attributes of the complex type.
        /// </summary>
        public StructureFieldAttribute FieldAttribute { get; }

        /// <summary>
        /// The data attributes of the complex type.
        /// </summary>
        public DataMemberAttribute DataAttribute { get; }

        /// <summary>
        /// Get the name of the complex type.
        /// </summary>
        public string Name => PropertyInfo.Name;

        /// <summary>
        /// Create the infos for the complex type.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="fieldAttribute"></param>
        /// <param name="dataAttribute"></param>
        public ComplexTypePropertyInfo(PropertyInfo propertyInfo,
            StructureFieldAttribute fieldAttribute,
            DataMemberAttribute dataAttribute)
        {
            PropertyInfo = propertyInfo;
            FieldAttribute = fieldAttribute;
            DataAttribute = dataAttribute;
            OptionalFieldMask = 0;
        }

        /// <summary>
        /// Get the value of a property.
        /// </summary>
        /// <param name="o"></param>
        public object? GetValue(object o)
        {
            return PropertyInfo.GetValue(o);
        }

        /// <summary>
        /// Set the value of a property.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="v"></param>
        public void SetValue(object o, object v)
        {
            PropertyInfo.SetValue(o, v);
        }
    }
}
