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
    using System.Text;

    /// <summary>
    /// Implements a union complex type.
    /// </summary>
    public class UnionComplexType : BaseComplexType
    {
        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public UnionComplexType()
        {
        }

        /// <summary>
        /// Initializes the object with a <paramref name="typeId"/>.
        /// </summary>
        /// <param name="typeId">The type to copy and create an instance from</param>
        public UnionComplexType(ExpandedNodeId typeId) : base(typeId)
        {
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Makes a deep copy of the object.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public new object MemberwiseClone()
        {
            var clone = (UnionComplexType)base.MemberwiseClone();
            clone._switchField = _switchField;
            return clone;
        }

        /// <summary>
        /// The union selector determines which property is valid.
        /// A value of 0 means all properties are invalid, x=1..n means the
        /// xth property is valid.
        /// </summary>
        public uint SwitchField => _switchField;

        /// <inheritdoc/>
        public override StructureType StructureType => StructureType.Union;

        /// <inheritdoc/>
        public override void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(XmlNamespace);

            string? fieldName = null;
            if (encoder.UseReversibleEncoding)
            {
                encoder.WriteUInt32("SwitchField", _switchField);
                fieldName = "Value";
            }

            if (_switchField != 0)
            {
                var unionSelector = 1;
                ComplexTypePropertyInfo? unionProperty = null;
                foreach (var property in GetPropertyEnumerator())
                {
                    if (unionSelector == _switchField)
                    {
                        unionProperty = property;
                        break;
                    }
                    unionSelector++;
                }
                if (unionProperty == null)
                {
                    throw ServiceResultException.Create(
                        StatusCodes.BadDataEncodingInvalid,
                        "Union selector out of range");
                }
                EncodeProperty(encoder, fieldName, unionProperty);
            }
            else if (!encoder.UseReversibleEncoding)
            {
                encoder.WriteString(null, "null");
            }

            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(XmlNamespace);

            _switchField = decoder.ReadUInt32("SwitchField");

            var unionSelector = _switchField;
            if (unionSelector > 0)
            {
                foreach (var property in GetPropertyEnumerator())
                {
                    if (--unionSelector == 0)
                    {
                        DecodeProperty(decoder, "Value", property);
                        break;
                    }
                }
            }
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public override bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            if (encodeable is not UnionComplexType valueBaseType)
            {
                return false;
            }

            if (SwitchField != valueBaseType.SwitchField)
            {
                return false;
            }

            var valueType = valueBaseType.GetType();
            if (GetType() != valueType)
            {
                return false;
            }

            if (_switchField != 0)
            {
                var unionSelector = _switchField;
                foreach (var property in GetPropertyEnumerator())
                {
                    if (--unionSelector == 0)
                    {
                        if (!Utils.IsEqual(property.GetValue(this), property.GetValue(valueBaseType)))
                        {
                            return false;
                        }
                        break;
                    }
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public override string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (format != null)
            {
                throw new FormatException($"Invalid format string: '{format}'.");
            }

            var body = new StringBuilder();
            if (_switchField != 0)
            {
                var unionSelector = _switchField;
                foreach (var property in GetPropertyEnumerator())
                {
                    if (--unionSelector == 0)
                    {
                        var unionProperty = property.GetValue(this);
                        AppendPropertyValue(formatProvider, body, unionProperty,
                            property.ValueRank);
                        break;
                    }
                }
            }
            if (body.Length > 0)
            {
                body.Append('}');
                return body.ToString();
            }
            if (!NodeId.IsNull(TypeId))
            {
                return string.Format(formatProvider, "{{{0}}}", TypeId);
            }
            return "(null)";
        }

#if INDEXED
        /// <summary>
        /// Access property values by index.
        /// </summary>
        /// <param name="index"></param>
        /// <remarks>
        /// The value of a Union is determined by the union selector.
        /// Calling get on an unselected property returns null,
        ///     otherwise the selected object.
        /// Calling get with an invalid index (e.g.-1) returns the selected object.
        /// Calling set with a valid object on a selected property sets the value and the
        /// union selector.
        /// Calling set with a null object or an invalid index unselects the union.
        /// </remarks>
        public override object this[int index]
        {
            get
            {
                if (index + 1 == (int)_switchField)
                {
                    return _propertyList[index].GetValue(this);
                }
                if (index < 0 &&
                    _switchField > 0)
                {
                    return _propertyList[(int)_switchField - 1].GetValue(this);
                }
                return null;
            }
            set
            {
                if (index >= 0)
                {
                    _propertyList[index].SetValue(this, value);
                    // note: selector is updated in SetValue by emitted code for union
                    // _unionSelector = (uint)(index + 1);
                    if (value != null)
                    {
                        return;
                    }
                    // reset union selector if value is a null
                }
                _switchField = 0;
            }
        }

        /// <summary>
        /// Access property values by name.
        /// </summary>
        /// <param name="name"></param>
        /// <remarks>
        /// The value of a Union is determined by the union selector.
        /// Calling get on an unselected property returns null,
        /// otherwise the selected object.
        /// Calling get with an invalid name returns the selected object.
        /// Calling set with a valid object on a selected property sets the value and the
        /// union selector.
        /// Calling set with a null object or an invalid name unselects the union.
        /// </remarks>
        public override object this[string name]
        {
            get
            {
                if (SwitchField > 0)
                {
                    ComplexTypePropertyInfo property;
                    if (_propertyDict.TryGetValue(name, out property))
                    {
                        if ((int)_switchField == property.Order)
                        {
                            return property.GetValue(this);
                        }
                    }
                    else
                    {
                        return _propertyList[(int)SwitchField - 1].GetValue(this);
                    }
                }
                return null;
            }
            set
            {
                ComplexTypePropertyInfo property;
                if (_propertyDict.TryGetValue(name, out property))
                {
                    property.SetValue(this, value);
                    // note: selector is updated in SetValue by emitted code for union
                    // _unionSelector = (uint)(property.Order);
                    if (value != null)
                    {
                        return;
                    }
                    // reset union selector if value is a null
                }
                _switchField = 0;
            }
        }
#endif

        /// <summary>
        /// The selector for the value of the Union.
        /// </summary>
        protected uint _switchField;
    }
}
