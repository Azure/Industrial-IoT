/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The base class for all complex types.
    /// </summary>
    public abstract class BaseComplexType : IEncodeable, IFormattable,
        IStructureTypeInfo
    {
        /// <inheritdoc/>
        public ExpandedNodeId TypeId { get; set; }

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId { get; set; }

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId { get; set; }

        /// <inheritdoc/>
        public virtual StructureType StructureType
            => StructureType.Structure;

        /// <summary>
        /// Provide XmlNamespace based on systemType
        /// </summary>
        protected string XmlNamespace
        {
            get
            {
                if (_xmlName == null)
                {
                    _xmlName = EncodeableFactory.GetXmlName(GetType());
                }
                return _xmlName != null ? _xmlName.Namespace : string.Empty;
            }
        }

        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        protected BaseComplexType()
        {
            TypeId = ExpandedNodeId.Null;
            BinaryEncodingId = ExpandedNodeId.Null;
            XmlEncodingId = ExpandedNodeId.Null;

            InitializePropertyAttributes();
        }

        /// <summary>
        /// Initializes the object with a <paramref name="typeId"/>.
        /// </summary>
        /// <param name="typeId">The type to copy and create an instance from</param>
        protected BaseComplexType(ExpandedNodeId typeId)
        {
            TypeId = typeId;
            BinaryEncodingId = ExpandedNodeId.Null;
            XmlEncodingId = ExpandedNodeId.Null;

            InitializePropertyAttributes();
        }

        /// <inheritdoc/>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Makes a deep copy of the object.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        /// <exception cref="InvalidOperationException"></exception>
        public new object MemberwiseClone()
        {
            var thisType = GetType();
            if (Activator.CreateInstance(thisType) is not BaseComplexType clone)
            {
                throw new InvalidOperationException("Cannot clone object.");
            }

            clone.TypeId = TypeId;
            clone.BinaryEncodingId = BinaryEncodingId;
            clone.XmlEncodingId = XmlEncodingId;

            // clone all properties of derived class
            foreach (var property in GetPropertyEnumerator())
            {
                property.SetValue(clone, Utils.Clone(property.GetValue(this)));
            }

            return clone;
        }

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(XmlNamespace);

            foreach (var property in GetPropertyEnumerator())
            {
                EncodeProperty(encoder, property);
            }

            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(XmlNamespace);

            foreach (var property in GetPropertyEnumerator())
            {
                DecodeProperty(decoder, property);
            }

            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            if (encodeable is not BaseComplexType valueBaseType)
            {
                return false;
            }

            var valueType = valueBaseType.GetType();
            if (GetType() != valueType)
            {
                return false;
            }

            foreach (var property in GetPropertyEnumerator())
            {
                if (!Utils.IsEqual(property.GetValue(this), property.GetValue(valueBaseType)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <summary>
        /// Returns the string representation of the complex type.
        /// </summary>
        /// <param name="format">(Unused). Leave this as null</param>
        /// <param name="formatProvider">The provider of a mechanism
        /// for retrieving an object to control formatting.</param>
        /// <returns>
        /// A <see cref="string"/> containing the value of the current
        /// embedded instance in the specified format.
        /// </returns>
        /// <exception cref="FormatException">Thrown if the
        /// <i>format</i> parameter is not null</exception>
        public virtual string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (format != null)
            {
                throw new FormatException($"Invalid format string: '{format}'.");
            }
            var body = new StringBuilder();
            foreach (var property in GetPropertyEnumerator())
            {
                AppendPropertyValue(formatProvider, body, property.GetValue(this),
                    property.ValueRank);
            }
            if (body.Length > 0)
            {
                return body.Append('}').ToString();
            }
            if (!NodeId.IsNull(TypeId))
            {
                return string.Format(formatProvider, "{{{0}}}", TypeId);
            }
            return "(null)";
        }

#if INDEXED
        /// <inheritdoc/>
        public virtual object this[int index]
        {
            get => _propertyList[index].GetValue(this);
            set => _propertyList[index].SetValue(this, value);
        }

        /// <inheritdoc/>
        public virtual object this[string name]
        {
            get => _propertyDict[name].GetValue(this);
            set => _propertyDict[name].SetValue(this, value);
        }
#endif

        /// <inheritdoc/>
        public virtual IEnumerable<ComplexTypePropertyInfo> GetPropertyEnumerator()
        {
            return _propertyList;
        }

        /// <summary>
        /// Formatting helper.
        /// </summary>
        /// <param name="body"></param>
        private static void AddSeparator(StringBuilder body)
        {
            if (body.Length == 0)
            {
                body.Append('{');
            }
            else
            {
                body.Append('|');
            }
        }

        /// <summary>
        /// Append a property to the value string. Handle arrays and enumerations.
        /// </summary>
        /// <param name="formatProvider"></param>
        /// <param name="body"></param>
        /// <param name="value"></param>
        /// <param name="valueRank"></param>
        protected static void AppendPropertyValue(IFormatProvider? formatProvider,
            StringBuilder body, object? value, int valueRank)
        {
            AddSeparator(body);
            if (valueRank >= 0 && value is Array array)
            {
                var rank = array.Rank;
                var dimensions = new int[rank];
                var mods = new int[rank];
                for (var ii = 0; ii < rank; ii++)
                {
                    dimensions[ii] = array.GetLength(ii);
                }

                for (var ii = rank - 1; ii >= 0; ii--)
                {
                    mods[ii] = dimensions[ii];
                    if (ii < rank - 1)
                    {
                        mods[ii] *= mods[ii + 1];
                    }
                }

                var count = 0;
                foreach (var item in array)
                {
                    var needSeparator = true;
                    for (var dc = 0; dc < rank; dc++)
                    {
                        if ((count % mods[dc]) == 0)
                        {
                            body.Append('[');
                            needSeparator = false;
                        }
                    }
                    if (needSeparator)
                    {
                        body.Append(',');
                    }
                    AppendPropertyValue(formatProvider, body, item);
                    count++;
                    needSeparator = false;
                    for (var dc = 0; dc < rank; dc++)
                    {
                        if ((count % mods[dc]) == 0)
                        {
                            body.Append(']');
                            needSeparator = true;
                        }
                    }
                    if (needSeparator && count < array.Length)
                    {
                        body.Append(',');
                    }
                }
            }
            else if (valueRank >= 0 && value is IEnumerable enumerable)
            {
                var first = true;
                body.Append('[');
                foreach (var item in enumerable)
                {
                    if (!first)
                    {
                        body.Append(',');
                    }
                    AppendPropertyValue(formatProvider, body, item);
                    first = false;
                }
                body.Append(']');
            }
            else
            {
                AppendPropertyValue(formatProvider, body, value);
            }
        }

        /// <summary>
        /// Append a property to the value string.
        /// </summary>
        /// <param name="formatProvider"></param>
        /// <param name="body"></param>
        /// <param name="value"></param>
        private static void AppendPropertyValue(IFormatProvider? formatProvider,
            StringBuilder body, object? value)
        {
            if (value is byte[] x)
            {
                body.AppendFormat(formatProvider, "Byte[{0}]", x.Length);
                return;
            }

            if (value is XmlElement xmlElements)
            {
                body.AppendFormat(formatProvider, "<{0}>", xmlElements.Name);
                return;
            }
            body.AppendFormat(formatProvider, "{0}", value);
        }

        /// <summary>
        /// Encode a property based on the property type and value rank.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <exception cref="ServiceResultException"></exception>
        protected void EncodeProperty(IEncoder encoder, string? name,
            ComplexTypePropertyInfo property)
        {
            var valueRank = property.ValueRank;
            var builtInType = property.BuiltInType;
            if (valueRank == ValueRanks.Scalar)
            {
                EncodeProperty(encoder, name, property.PropertyInfo, builtInType);
            }
            else if (valueRank >= ValueRanks.OneDimension)
            {
                EncodePropertyArray(encoder, name, property.PropertyInfo,
                    builtInType, valueRank);
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                    "Cannot encode a property with unsupported ValueRank {0}.",
                    valueRank);
            }
        }

        /// <summary>
        /// Encode a property based on the property type and value rank.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="property"></param>
        protected void EncodeProperty(IEncoder encoder,
            ComplexTypePropertyInfo property)
        {
            EncodeProperty(encoder, property.Name, property);
        }

        /// <summary>
        /// Encode a scalar property based on the property type.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <param name="builtInType"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void EncodeProperty(IEncoder encoder, string? name,
            PropertyInfo property, BuiltInType builtInType)
        {
            var propertyType = property.PropertyType;
            if (propertyType.IsEnum)
            {
                builtInType = BuiltInType.Enumeration;
            }
            switch (builtInType)
            {
                case BuiltInType.Boolean:
                    encoder.WriteBoolean(name,
                        (bool)property.GetValue(this)!);
                    break;
                case BuiltInType.SByte:
                    encoder.WriteSByte(name,
                        (sbyte)property.GetValue(this)!);
                    break;
                case BuiltInType.Byte:
                    encoder.WriteByte(name,
                        (byte)property.GetValue(this)!);
                    break;
                case BuiltInType.Int16:
                    encoder.WriteInt16(name,
                        (short)property.GetValue(this)!);
                    break;
                case BuiltInType.UInt16:
                    encoder.WriteUInt16(name,
                        (ushort)property.GetValue(this)!);
                    break;
                case BuiltInType.Int32:
                    encoder.WriteInt32(name,
                        (int)property.GetValue(this)!);
                    break;
                case BuiltInType.UInt32:
                    encoder.WriteUInt32(name,
                        (uint)property.GetValue(this)!);
                    break;
                case BuiltInType.Int64:
                    encoder.WriteInt64(name,
                        (long)property.GetValue(this)!);
                    break;
                case BuiltInType.UInt64:
                    encoder.WriteUInt64(name,
                        (ulong)property.GetValue(this)!);
                    break;
                case BuiltInType.Float:
                    encoder.WriteFloat(name,
                        (float)property.GetValue(this)!);
                    break;
                case BuiltInType.Double:
                    encoder.WriteDouble(name,
                        (double)property.GetValue(this)!);
                    break;
                case BuiltInType.String:
                    encoder.WriteString(name,
                        (string)property.GetValue(this)!);
                    break;
                case BuiltInType.DateTime:
                    encoder.WriteDateTime(name,
                        (DateTime)property.GetValue(this)!);
                    break;
                case BuiltInType.Guid:
                    encoder.WriteGuid(name,
                        (Uuid)property.GetValue(this)!);
                    break;
                case BuiltInType.ByteString:
                    encoder.WriteByteString(name,
                        (byte[]?)property.GetValue(this));
                    break;
                case BuiltInType.XmlElement:
                    encoder.WriteXmlElement(name,
                        (XmlElement?)property.GetValue(this));
                    break;
                case BuiltInType.NodeId:
                    encoder.WriteNodeId(name,
                        (NodeId?)property.GetValue(this));
                    break;
                case BuiltInType.ExpandedNodeId:
                    encoder.WriteExpandedNodeId(name,
                        (ExpandedNodeId?)property.GetValue(this));
                    break;
                case BuiltInType.StatusCode:
                    encoder.WriteStatusCode(name,
                        (StatusCode)property.GetValue(this)!);
                    break;
                case BuiltInType.DiagnosticInfo:
                    encoder.WriteDiagnosticInfo(name,
                        (DiagnosticInfo?)property.GetValue(this));
                    break;
                case BuiltInType.QualifiedName:
                    encoder.WriteQualifiedName(name,
                        (QualifiedName?)property.GetValue(this));
                    break;
                case BuiltInType.LocalizedText:
                    encoder.WriteLocalizedText(name,
                        (LocalizedText?)property.GetValue(this));
                    break;
                case BuiltInType.DataValue:
                    encoder.WriteDataValue(name,
                        (DataValue?)property.GetValue(this));
                    break;
                case BuiltInType.Variant:
                    encoder.WriteVariant(name,
                        (Variant)property.GetValue(this)!);
                    break;
                case BuiltInType.ExtensionObject:
                    encoder.WriteExtensionObject(name,
                        (ExtensionObject?)property.GetValue(this));
                    break;
                case BuiltInType.Enumeration:
                    if (propertyType.IsEnum)
                    {
                        encoder.WriteEnumerated(name,
                            (Enum?)property.GetValue(this));
                        break;
                    }
                    goto case BuiltInType.Int32;
                default:
                    if (typeof(IEncodeable).IsAssignableFrom(propertyType))
                    {
                        encoder.WriteEncodeable(name,
                            (IEncodeable?)property.GetValue(this), propertyType);
                        break;
                    }
                    throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                        "Cannot encode unknown type {0}.", propertyType.Name);
            }
        }

        /// <summary>
        /// Encode an array property based on the base property type.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        private void EncodePropertyArray(IEncoder encoder, string? name,
            PropertyInfo property, BuiltInType builtInType, int valueRank)
        {
            var elementType = property.PropertyType.GetElementType()
                ?? property.PropertyType.GetItemType();
            Debug.Assert(elementType != null);
            if (elementType.IsEnum)
            {
                builtInType = BuiltInType.Enumeration;
            }
            encoder.WriteArray(name, property.GetValue(this),
                valueRank, builtInType);
        }

        /// <summary>
        /// Decode a property based on the property type and value rank.
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="property"></param>
        protected void DecodeProperty(IDecoder decoder,
            ComplexTypePropertyInfo property)
        {
            DecodeProperty(decoder, property.Name, property);
        }

        /// <summary>
        /// Decode a property based on the property type and value rank.
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <exception cref="ServiceResultException"></exception>
        protected void DecodeProperty(IDecoder decoder, string name,
            ComplexTypePropertyInfo property)
        {
            var valueRank = property.ValueRank;
            if (valueRank == ValueRanks.Scalar)
            {
                DecodeProperty(decoder, name, property.PropertyInfo,
                    property.BuiltInType);
            }
            else if (valueRank >= ValueRanks.OneDimension)
            {
                DecodePropertyArray(decoder, name, property.PropertyInfo,
                    property.BuiltInType, valueRank);
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Cannot decode a property with unsupported ValueRank {0}.",
                    valueRank);
            }
        }

        /// <summary>
        /// Decode a scalar property based on the property type.
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <param name="builtInType"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void DecodeProperty(IDecoder decoder, string name,
            PropertyInfo property, BuiltInType builtInType)
        {
            var propertyType = property.PropertyType;
            if (propertyType.IsEnum)
            {
                builtInType = BuiltInType.Enumeration;
            }
            switch (builtInType)
            {
                case BuiltInType.Boolean:
                    property.SetValue(this, decoder.ReadBoolean(name));
                    break;
                case BuiltInType.SByte:
                    property.SetValue(this, decoder.ReadSByte(name));
                    break;
                case BuiltInType.Byte:
                    property.SetValue(this, decoder.ReadByte(name));
                    break;
                case BuiltInType.Int16:
                    property.SetValue(this, decoder.ReadInt16(name));
                    break;
                case BuiltInType.UInt16:
                    property.SetValue(this, decoder.ReadUInt16(name));
                    break;
                case BuiltInType.Int32:
                    property.SetValue(this, decoder.ReadInt32(name));
                    break;
                case BuiltInType.UInt32:
                    property.SetValue(this, decoder.ReadUInt32(name));
                    break;
                case BuiltInType.Int64:
                    property.SetValue(this, decoder.ReadInt64(name));
                    break;
                case BuiltInType.UInt64:
                    property.SetValue(this, decoder.ReadUInt64(name));
                    break;
                case BuiltInType.Float:
                    property.SetValue(this, decoder.ReadFloat(name));
                    break;
                case BuiltInType.Double:
                    property.SetValue(this, decoder.ReadDouble(name));
                    break;
                case BuiltInType.String:
                    property.SetValue(this, decoder.ReadString(name));
                    break;
                case BuiltInType.DateTime:
                    property.SetValue(this, decoder.ReadDateTime(name));
                    break;
                case BuiltInType.Guid:
                    property.SetValue(this, decoder.ReadGuid(name));
                    break;
                case BuiltInType.ByteString:
                    property.SetValue(this, decoder.ReadByteString(name));
                    break;
                case BuiltInType.XmlElement:
                    property.SetValue(this, decoder.ReadXmlElement(name));
                    break;
                case BuiltInType.NodeId:
                    property.SetValue(this, decoder.ReadNodeId(name));
                    break;
                case BuiltInType.ExpandedNodeId:
                    property.SetValue(this, decoder.ReadExpandedNodeId(name));
                    break;
                case BuiltInType.StatusCode:
                    property.SetValue(this, decoder.ReadStatusCode(name));
                    break;
                case BuiltInType.QualifiedName:
                    property.SetValue(this, decoder.ReadQualifiedName(name));
                    break;
                case BuiltInType.LocalizedText:
                    property.SetValue(this, decoder.ReadLocalizedText(name));
                    break;
                case BuiltInType.DataValue:
                    property.SetValue(this, decoder.ReadDataValue(name));
                    break;
                case BuiltInType.Variant:
                    property.SetValue(this, decoder.ReadVariant(name));
                    break;
                case BuiltInType.DiagnosticInfo:
                    property.SetValue(this, decoder.ReadDiagnosticInfo(name));
                    break;
                case BuiltInType.ExtensionObject:
                    if (typeof(IEncodeable).IsAssignableFrom(propertyType))
                    {
                        property.SetValue(this,
                            decoder.ReadEncodeable(name, propertyType));
                        break;
                    }
                    property.SetValue(this, decoder.ReadExtensionObject(name));
                    break;
                case BuiltInType.Enumeration:
                    if (propertyType.IsEnum)
                    {
                        property.SetValue(this,
                            decoder.ReadEnumerated(name, propertyType)); break;
                    }
                    goto case BuiltInType.Int32;
                default:
                    if (typeof(IEncodeable).IsAssignableFrom(propertyType))
                    {
                        property.SetValue(this,
                            decoder.ReadEncodeable(name, propertyType));
                        break;
                    }
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Cannot decode unknown type {0}.", propertyType.Name);
            }
        }

        /// <summary>
        /// Decode an array property based on the base property type.
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        private void DecodePropertyArray(IDecoder decoder, string name,
            PropertyInfo property, BuiltInType builtInType, int valueRank)
        {
            var elementType = property.PropertyType.GetElementType()
                ?? property.PropertyType.GetItemType();
            Debug.Assert(elementType != null);
            if (elementType.IsEnum)
            {
                builtInType = BuiltInType.Enumeration;
            }
            var decodedArray = decoder.ReadArray(name, valueRank,
                builtInType, elementType);
            property.SetValue(this, decodedArray);
        }

        /// <summary>
        /// Initialize the helpers for property enumerator and dictionary.
        /// </summary>
        private void InitializePropertyAttributes()
        {
            var typeAttribute = GetType().GetCustomAttribute<StructureTypeIdAttribute>();
            if (typeAttribute != null)
            {
                TypeId = ExpandedNodeId.Parse(typeAttribute.ComplexTypeId);
                BinaryEncodingId = ExpandedNodeId.Parse(typeAttribute.BinaryEncodingId);
                XmlEncodingId = ExpandedNodeId.Parse(typeAttribute.XmlEncodingId);
            }

            var propertyList = new List<ComplexTypePropertyInfo>();
            foreach (var property in GetType().GetProperties())
            {
                var fieldAttribute = property.GetCustomAttribute<StructureFieldAttribute>();

                if (fieldAttribute == null)
                {
                    continue;
                }

                var dataAttribute = property.GetCustomAttribute<DataMemberAttribute>();
                if (dataAttribute == null)
                {
                    continue;
                }
                var newProperty = new ComplexTypePropertyInfo(property,
                    fieldAttribute, dataAttribute);

                propertyList.Add(newProperty);
            }

            foreach (var item in propertyList.OrderBy(p => p.Order))
            {
                _propertyList.Add(item);
                _propertyDict.TryAdd(item.Name, item);
            }
        }

        /// <summary> The list of properties of this complex type. </summary>
        protected readonly IList<ComplexTypePropertyInfo> _propertyList
            = [];
        /// <summary> The list of properties as dictionary. </summary>
        protected readonly Dictionary<string, ComplexTypePropertyInfo> _propertyDict
            = [];
        private XmlQualifiedName? _xmlName;
    }
}
