/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Data generator that generates non null typed data.  This is a
    /// fork of the DataGenerator, but ensuring that the data generated
    /// is consistent for the use in unit tests.
    /// </summary>
    public class TestDataGenerator
    {
        /// <summary>
        /// Set max array length
        /// </summary>
        public int MaxArrayLength { get; set; } = 100;

        /// <summary>
        /// Set max string length
        /// </summary>
        public int MaxStringLength { get; set; } = 100;

        /// <summary>
        /// Set min date time value
        /// </summary>
        public DateTime MinDateTimeValue { get; set; }

        /// <summary>
        /// Set max date time value
        /// </summary>
        public DateTime MaxDateTimeValue { get; set; }

        /// <summary>
        /// Max xml attribute count
        /// </summary>
        public int MaxXmlAttributeCount { get; set; } = 10;

        /// <summary>
        /// Max xml element count
        /// </summary>
        public int MaxXmlElementCount { get; set; } = 10;

        /// <summary>
        /// Namespace uris
        /// </summary>
        public NamespaceTable NamespaceUris { get; set; }

        /// <summary>
        /// Server uris
        /// </summary>
        public StringTable ServerUris { get; set; }

        /// <summary>
        /// Frequency of boundary values used
        /// </summary>
        public int BoundaryValueFrequency { get; set; } = 20;

        /// <summary>
        /// Create generator
        /// </summary>
        /// <param name="random"></param>
        public TestDataGenerator(IRandomSource random = null)
        {
            MinDateTimeValue = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            MaxDateTimeValue = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            NamespaceUris = new NamespaceTable();
            ServerUris = new StringTable();
            _random = random ?? new RandomSource();
            _boundaryValues = new SortedDictionary<string, object[]>();
            for (var i = 0; i < kAvailableBoundaryValues.Length; i++)
            {
                _boundaryValues[kAvailableBoundaryValues[i].SystemType.Name] =
                    kAvailableBoundaryValues[i].Values.ToArray();
            }
            _tokenValues = LoadStringData("Opc.Ua.Types.Utils.LocalizedData.txt");
            if (_tokenValues.Count == 0)
            {
                _tokenValues = LoadStringData("Opc.Ua.Utils.LocalizedData.txt");
            }
            _availableLocales = new string[_tokenValues.Count];
            var num = 0;
            foreach (var key in _tokenValues.Keys)
            {
                _availableLocales[num++] = key;
            }
        }

        /// <summary>
        /// Get random data
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        /// <param name="arrayDimensions"></param>
        /// <param name="typeTree"></param>
        /// <returns></returns>
        public object GetRandom(NodeId dataType, int valueRank, IList<uint> arrayDimensions,
            ITypeTable typeTree)
        {
            var builtInType = Ua.TypeInfo.GetBuiltInType(dataType, typeTree);
            int num;
            switch (valueRank)
            {
                case -2:
                    num = (arrayDimensions == null || arrayDimensions.Count == 0) ?
                        GetRandomRange(0, 1) : arrayDimensions.Count;
                    break;
                case -3:
                    num = GetRandomRange(0, 1);
                    break;
                case 0:
                    num = (arrayDimensions == null || arrayDimensions.Count == 0) ?
                        GetRandomRange(1, 1) : arrayDimensions.Count;
                    break;
                case -1:
                    num = 0;
                    break;
                default:
                    num = valueRank;
                    break;
            }
            if (num == 0)
            {
                if (builtInType == BuiltInType.Variant)
                {
                    var builtInType2 = BuiltInType.Variant;
                    while (builtInType2 == BuiltInType.Variant || builtInType2 == BuiltInType.DataValue)
                    {
                        builtInType2 = (BuiltInType)_random.NextInt32(24);
                    }
                    return GetRandomVariant(builtInType2, isArray: false);
                }
                return GetRandom(builtInType);
            }
            var array = new int[num];
            for (var i = 0; i < num; i++)
            {
                if (arrayDimensions != null && arrayDimensions.Count > i)
                {
                    array[i] = (int)arrayDimensions[i];
                }
                while (array[i] == 0)
                {
                    array[i] = _random.NextInt32(MaxArrayLength);
                }
            }
            var array2 = Ua.TypeInfo.CreateArray(builtInType, array);
            var length = array2.Length;
            var array3 = new int[array.Length];
            for (var j = 0; j < length; j++)
            {
                var num2 = array2.Length;
                for (var k = 0; k < array3.Length; k++)
                {
                    num2 /= array[k];
                    array3[k] = j / num2 % array[k];
                }
                var obj = GetRandom(dataType, -1, null, typeTree);
                if (obj != null)
                {
                    if (builtInType == BuiltInType.Guid)
                    {
                        obj = new Uuid((Guid)obj);
                    }
                    array2.SetValue(obj, array3);
                }
            }
            return array2;
        }

        /// <summary>
        /// Get random data
        /// </summary>
        /// <param name="expectedType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public object GetRandom(BuiltInType expectedType)
        {
            switch (expectedType)
            {
                case BuiltInType.Boolean:
                    return GetRandomBoolean();
                case BuiltInType.SByte:
                    return GetRandomSByte();
                case BuiltInType.Byte:
                    return GetRandomByte();
                case BuiltInType.Int16:
                    return GetRandomInt16();
                case BuiltInType.UInt16:
                    return GetRandomUInt16();
                case BuiltInType.Int32:
                    return GetRandomInt32();
                case BuiltInType.UInt32:
                    return GetRandomUInt32();
                case BuiltInType.Int64:
                    return GetRandomInt64();
                case BuiltInType.UInt64:
                    return GetRandomUInt64();
                case BuiltInType.Float:
                    return GetRandomFloat();
                case BuiltInType.Double:
                    return GetRandomDouble();
                case BuiltInType.String:
                    return GetRandomString();
                case BuiltInType.DateTime:
                    return GetRandomDateTime();
                case BuiltInType.Guid:
                    return GetRandomGuid();
                case BuiltInType.ByteString:
                    return GetRandomByteString();
                case BuiltInType.XmlElement:
                    return GetRandomXmlElement();
                case BuiltInType.NodeId:
                    return GetRandomNodeId();
                case BuiltInType.ExpandedNodeId:
                    return GetRandomExpandedNodeId();
                case BuiltInType.QualifiedName:
                    return GetRandomQualifiedName();
                case BuiltInType.LocalizedText:
                    return GetRandomLocalizedText();
                case BuiltInType.StatusCode:
                    return GetRandomStatusCode();
                case BuiltInType.Variant:
                    return GetRandomVariant();
                case BuiltInType.Enumeration:
                    return GetRandomInt32();
                case BuiltInType.ExtensionObject:
                    return GetRandomExtensionObject();
                case BuiltInType.Number:
                    {
                        var builtInType = (BuiltInType)(_random.NextInt32(9) + 2);
                        return GetRandomVariant(builtInType, isArray: false);
                    }
                case BuiltInType.Integer:
                    {
                        var builtInType = (BuiltInType)((_random.NextInt32(3) * 2) + 2);
                        return GetRandomVariant(builtInType, isArray: false);
                    }
                case BuiltInType.UInteger:
                    {
                        var builtInType = (BuiltInType)((_random.NextInt32(3) * 2) + 3);
                        return GetRandomVariant(builtInType, isArray: false);
                    }
                case BuiltInType.Null:
                    return null;
                default:
                    throw new ArgumentException($"Unexpected scalar type {expectedType} passed");
            }
        }

        /// <summary>
        /// Get random array data
        /// </summary>
        /// <param name="expectedType"></param>
        /// <param name="length"></param>
        /// <param name="fixedLength"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Array GetRandomArray(BuiltInType expectedType, int length,
            bool fixedLength)
        {
            switch (expectedType)
            {
                case BuiltInType.Boolean:
                    return GetRandomArray<bool>(length, fixedLength);
                case BuiltInType.SByte:
                    return GetRandomArray<sbyte>(length, fixedLength);
                case BuiltInType.Byte:
                    return GetRandomArray<byte>(length, fixedLength);
                case BuiltInType.Int16:
                    return GetRandomArray<short>(length, fixedLength);
                case BuiltInType.UInt16:
                    return GetRandomArray<ushort>(length, fixedLength);
                case BuiltInType.Int32:
                    return GetRandomArray<int>(length, fixedLength);
                case BuiltInType.UInt32:
                    return GetRandomArray<uint>(length, fixedLength);
                case BuiltInType.Int64:
                    return GetRandomArray<long>(length, fixedLength);
                case BuiltInType.UInt64:
                    return GetRandomArray<ulong>(length, fixedLength);
                case BuiltInType.Float:
                    return GetRandomArray<float>(length, fixedLength);
                case BuiltInType.Double:
                    return GetRandomArray<double>(length, fixedLength);
                case BuiltInType.String:
                    return GetRandomArray<string>(length, fixedLength);
                case BuiltInType.DateTime:
                    return GetRandomArray<DateTime>(length, fixedLength);
                case BuiltInType.Guid:
                    return GetRandomArray<Uuid>(length, fixedLength);
                case BuiltInType.ByteString:
                    return GetRandomArray<byte[]>(length, fixedLength);
                case BuiltInType.XmlElement:
                    return GetRandomArray<XmlElement>(length, fixedLength);
                case BuiltInType.NodeId:
                    return GetRandomArray<NodeId>(length, fixedLength);
                case BuiltInType.ExpandedNodeId:
                    return GetRandomArray<ExpandedNodeId>(length, fixedLength);
                case BuiltInType.QualifiedName:
                    return GetRandomArray<QualifiedName>(length, fixedLength);
                case BuiltInType.LocalizedText:
                    return GetRandomArray<LocalizedText>(length, fixedLength);
                case BuiltInType.StatusCode:
                    return GetRandomArray<StatusCode>(length, fixedLength);
                case BuiltInType.Variant:
                    return GetRandomArray<Variant>(length, fixedLength);
                case BuiltInType.Enumeration:
                    return GetRandomArray<int>(length, fixedLength);
                case BuiltInType.ExtensionObject:
                    return GetRandomArray<ExtensionObject>(length, fixedLength);
                case BuiltInType.Number:
                    {
                        var builtInType3 = (BuiltInType)(_random.NextInt32(9) + 2);
                        return GetRandomArrayInVariant(builtInType3, length, fixedLength);
                    }
                case BuiltInType.Integer:
                    {
                        var builtInType2 = (BuiltInType)((_random.NextInt32(3) * 2) + 2);
                        return GetRandomArrayInVariant(builtInType2, length, fixedLength);
                    }
                case BuiltInType.UInteger:
                    {
                        var builtInType = (BuiltInType)((_random.NextInt32(3) * 2) + 3);
                        return GetRandomArrayInVariant(builtInType, length, fixedLength);
                    }
                case BuiltInType.Null:
                    return null;
                default:
                    throw new ArgumentException($"Unexpected array type {expectedType} passed");
            }
        }

        /// <summary>
        /// Get random
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetRandom<T>()
        {
            if (UseBoundaryValue())
            {
                var boundaryValue = GetBoundaryValue(typeof(T));
                if (boundaryValue != null)
                {
                    return (T)boundaryValue;
                }
            }
            return (T)GetRandom(typeof(T));
        }

        /// <summary>
        /// Get random array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="length"></param>
        /// <param name="fixedLength"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T[] GetRandomArray<T>(int length = 100, bool fixedLength = false)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    $"Length is negative {length}");
            }
            if (!fixedLength)
            {
                length = _random.NextInt32(length);
            }
            var array = new T[length];
            for (var i = 0; i < array.Length; i++)
            {
                object obj = null;
                do
                {
                    if (UseBoundaryValue())
                    {
                        obj = GetBoundaryValue(typeof(T));
                    }
                    obj ??= GetRandom<T>();
                }
                while (obj == null);
                array[i] = (T)obj;
            }
            return array;
        }

        /// <summary>
        /// Random boolean
        /// </summary>
        /// <returns></returns>
        public bool GetRandomBoolean()
        {
            return _random.NextInt32(1) != 0;
        }

        /// <summary>
        /// Random signed byte
        /// </summary>
        /// <returns></returns>
        public sbyte GetRandomSByte()
        {
            var num = _random.NextInt32(255);
            if (num > 127)
            {
                return (sbyte)(-128 + (num - 127) - 1);
            }
            return (sbyte)num;
        }

        /// <summary>
        /// Random byte
        /// </summary>
        /// <returns></returns>
        public byte GetRandomByte()
        {
            return (byte)_random.NextInt32(255);
        }

        /// <summary>
        /// Random short
        /// </summary>
        /// <returns></returns>
        public short GetRandomInt16()
        {
            var array = new byte[2];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToInt16(array, 0);
        }

        /// <summary>
        /// random ushort
        /// </summary>
        /// <returns></returns>
        public ushort GetRandomUInt16()
        {
            var array = new byte[2];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToUInt16(array, 0);
        }

        /// <summary>
        /// Random int32
        /// </summary>
        /// <returns></returns>
        public int GetRandomInt32()
        {
            var array = new byte[4];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToInt32(array, 0);
        }

        /// <summary>
        /// Random uint32
        /// </summary>
        /// <returns></returns>
        public uint GetRandomUInt32()
        {
            var array = new byte[4];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToUInt32(array, 0);
        }

        /// <summary>
        /// Random uint64
        /// </summary>
        /// <returns></returns>
        public long GetRandomInt64()
        {
            var array = new byte[8];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToInt64(array, 0);
        }

        /// <summary>
        /// Random int64
        /// </summary>
        /// <returns></returns>
        public ulong GetRandomUInt64()
        {
            var array = new byte[8];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToUInt64(array, 0);
        }

        public float GetRandomFloat()
        {
            var array = new byte[4];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToSingle(array, 0);
        }

        /// <summary>
        /// Get random double
        /// </summary>
        /// <returns></returns>
        public double GetRandomDouble()
        {
            var array = new byte[8];
            _random.NextBytes(array, 0, array.Length);
            return BitConverter.ToDouble(array, 0);
        }

        /// <summary>
        /// Random string
        /// </summary>
        /// <returns></returns>
        public string GetRandomString()
        {
            return CreateString(GetRandomLocale(), false);
        }

        public DateTime GetRandomDateTime()
        {
            var min = (int)(MinDateTimeValue.Ticks >> 32);
            var max = (int)(MaxDateTimeValue.Ticks >> 32);
            long num = GetRandomRange(min, max);
            var num2 = num << 32;
            var randomUInt = GetRandomUInt32();
            return new DateTime(num2 + randomUInt, DateTimeKind.Utc);
        }

        /// <summary>
        /// Get random guid
        /// </summary>
        /// <returns></returns>
        public Guid GetRandomGuid()
        {
            var array = new byte[16];
            _random.NextBytes(array, 0, array.Length);
            return new Guid(array);
        }

        /// <summary>
        /// Get random byte array
        /// </summary>
        /// <returns></returns>
        public byte[] GetRandomByteString()
        {
            var num = _random.NextInt32(MaxStringLength);
            var array = new byte[num];
            _random.NextBytes(array, 0, array.Length);
            return array;
        }

        /// <summary>
        /// Get random xml element
        /// </summary>
        /// <returns></returns>
        public XmlElement GetRandomXmlElement()
        {
            var randomLocale = GetRandomLocale();
            var randomLocale2 = GetRandomLocale();
            var xmlDocument = new XmlDocument();
            var xmlElement = xmlDocument.CreateElement("n0",
                CreateString(randomLocale, true), Utils.Format("http://{0}", CreateString(randomLocale, true)));
            xmlDocument.AppendChild(xmlElement);
            var num = _random.NextInt32(MaxXmlAttributeCount);
            for (var i = 0; i < num; i++)
            {
                var name = CreateString(randomLocale, true);
                var xmlAttribute = xmlDocument.CreateAttribute(name);
                xmlAttribute.Value = CreateString(randomLocale2, true);
                xmlElement.SetAttributeNode(xmlAttribute);
            }
            var num2 = _random.NextInt32(MaxXmlElementCount);
            for (var j = 0; j < num2; j++)
            {
                var localName = CreateString(randomLocale, true);
                var xmlElement2 = xmlDocument.CreateElement(xmlElement.Prefix, localName, xmlElement.NamespaceURI);
                xmlElement2.InnerText = CreateString(randomLocale2, false);
                xmlElement.AppendChild(xmlElement2);
            }
            return xmlElement;
        }

        /// <summary>
        /// Get random node id
        /// </summary>
        /// <returns></returns>
        public NodeId GetRandomNodeId()
        {
            var namespaceIndex = (ushort)_random.NextInt32(NamespaceUris.Count - 1);
            switch (_random.NextInt32(4))
            {
                case 1:
                    return new NodeId(CreateString(GetRandomLocale(), true), namespaceIndex);
                case 2:
                    return new NodeId(GetRandomGuid(), namespaceIndex);
                case 3:
                    return new NodeId(GetRandomByteString(), namespaceIndex);
                default:
                    return new NodeId(GetRandomUInt32(), namespaceIndex);
            }
        }

        /// <summary>
        /// Get random expanded node id
        /// </summary>
        /// <returns></returns>
        public ExpandedNodeId GetRandomExpandedNodeId()
        {
            var randomNodeId = GetRandomNodeId();
            var serverIndex = (ushort)((ServerUris.Count != 0) ?
                ((ushort)_random.NextInt32(ServerUris.Count - 1)) : 0);
            return new ExpandedNodeId(randomNodeId, NamespaceUris.GetString(randomNodeId.NamespaceIndex), serverIndex);
        }

        /// <summary>
        /// Get random qn
        /// </summary>
        /// <returns></returns>
        public QualifiedName GetRandomQualifiedName()
        {
            var namespaceIndex = (ushort)_random.NextInt32(NamespaceUris.Count - 1);
            return new QualifiedName(CreateString(GetRandomLocale(), true), namespaceIndex);
        }

        /// <summary>
        /// Get random localized text
        /// </summary>
        /// <returns></returns>
        public LocalizedText GetRandomLocalizedText()
        {
            var randomLocale = GetRandomLocale();
            return new LocalizedText(randomLocale, CreateString(randomLocale, false));
        }

        /// <summary>
        /// Get random status code
        /// </summary>
        /// <returns></returns>
        public StatusCode GetRandomStatusCode()
        {
            var randomRange = GetRandomRange(32769, 32951);
            return (uint)(2147549184u + (randomRange << 16));
        }

        /// <summary>
        /// Create random variant
        /// </summary>
        /// <param name="allowArrays"></param>
        /// <returns></returns>
        public Variant GetRandomVariant(bool allowArrays = true)
        {
            var builtInType = BuiltInType.Variant;
            while (builtInType == BuiltInType.Variant || builtInType == BuiltInType.DataValue || builtInType == BuiltInType.Null)
            {
                builtInType = (BuiltInType)_random.NextInt32(24);
            }
            return GetRandomVariant(builtInType, allowArrays && _random.NextInt32(1) == 1);
        }

        private bool UseBoundaryValue()
        {
            if (!_useBoundaryValues)
            {
                return false;
            }
            return _random.NextInt32(99) < BoundaryValueFrequency;
        }

        private Variant[] GetRandomArrayInVariant(BuiltInType builtInType,
            int length, bool fixedLength)
        {
            var randomArray = GetRandomArray(builtInType, length, fixedLength);
            var array = new Variant[randomArray.Length];
            var typeInfo = new Ua.TypeInfo(builtInType, -1);
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = new Variant(randomArray.GetValue(i), typeInfo);
            }
            return array;
        }

        private Variant GetRandomVariant(BuiltInType builtInType, bool isArray)
        {
            if (builtInType == BuiltInType.Null)
            {
                return Variant.Null;
            }
            var num = -1;
            if (isArray)
            {
                num = _random.NextInt32(MaxArrayLength - 1);
            }
            else if (builtInType == BuiltInType.Variant)
            {
                num = 1;
            }
            if (num >= 0)
            {
                switch (builtInType)
                {
                    case BuiltInType.Boolean:
                        return new Variant(GetRandomArray<bool>(num, true));
                    case BuiltInType.SByte:
                        return new Variant(GetRandomArray<sbyte>(num, true));
                    case BuiltInType.Byte:
                        return new Variant(GetRandomArray<byte>(num, true));
                    case BuiltInType.Int16:
                        return new Variant(GetRandomArray<short>(num, true));
                    case BuiltInType.UInt16:
                        return new Variant(GetRandomArray<ushort>(num, true));
                    case BuiltInType.Int32:
                        return new Variant(GetRandomArray<int>(num, true));
                    case BuiltInType.UInt32:
                        return new Variant(GetRandomArray<uint>(num, true));
                    case BuiltInType.Int64:
                        return new Variant(GetRandomArray<long>(num, true));
                    case BuiltInType.UInt64:
                        return new Variant(GetRandomArray<ulong>(num, true));
                    case BuiltInType.Float:
                        return new Variant(GetRandomArray<float>(num, true));
                    case BuiltInType.Double:
                        return new Variant(GetRandomArray<double>(num, true));
                    case BuiltInType.String:
                        return new Variant(GetRandomArray<string>(num, true));
                    case BuiltInType.DateTime:
                        return new Variant(GetRandomArray<DateTime>(num, true));
                    case BuiltInType.Guid:
                        return new Variant(GetRandomArray<Guid>(num, true));
                    case BuiltInType.ByteString:
                        return new Variant(GetRandomArray<byte[]>(num, true));
                    case BuiltInType.XmlElement:
                        return new Variant(GetRandomArray<XmlElement>(num, true));
                    case BuiltInType.NodeId:
                        return new Variant(GetRandomArray<NodeId>(num, true));
                    case BuiltInType.ExpandedNodeId:
                        return new Variant(GetRandomArray<ExpandedNodeId>(num, true));
                    case BuiltInType.QualifiedName:
                        return new Variant(GetRandomArray<QualifiedName>(num, true));
                    case BuiltInType.LocalizedText:
                        return new Variant(GetRandomArray<LocalizedText>(num, true));
                    case BuiltInType.StatusCode:
                        return new Variant(GetRandomArray<StatusCode>(num, true));
                    case BuiltInType.Variant:
                        return new Variant(GetRandomArray<Variant>(num, true));
                    case BuiltInType.ExtensionObject:
                        return new Variant(GetRandomArray<ExtensionObject>(num, true));
                    default:
                        throw new ArgumentException($"Unexpected type {builtInType} constructing variant");
                }
            }
            return new Variant(GetRandom(builtInType));
        }

        public ExtensionObject GetRandomExtensionObject()
        {
            var randomNodeId = GetRandomNodeId();
            if (!NodeId.IsNull(randomNodeId))
            {
                return new ExtensionObject(randomNodeId, (_random.NextInt32(1) == 0) ?
                    GetRandomXmlElement() : GetRandomByteString());
            }
            return new ExtensionObject();
        }

        private static SortedDictionary<string, string[]> LoadStringData(string resourceName)
        {
            var sortedDictionary = new SortedDictionary<string, string[]>();
            try
            {
                string text = null;
                List<string> list = null;
                var stream = typeof(DataGenerator).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    var fileInfo = new FileInfo(resourceName);
                    stream = fileInfo.OpenRead();
                }
                using (var streamReader = new StreamReader(stream))
                {
                    for (var text2 = streamReader.ReadLine(); text2 != null; text2 = streamReader.ReadLine())
                    {
                        var text3 = text2.Trim();
                        if (!string.IsNullOrEmpty(text3))
                        {
                            if (text3.StartsWith('='))
                            {
                                if (text != null)
                                {
                                    sortedDictionary.Add(text, list.ToArray());
                                }
                                text = text3[1..];
                                list = new List<string>();
                            }
                            else
                            {
                                list.Add(text3);
                            }
                        }
                    }
                }
                return sortedDictionary;
            }
            catch (Exception)
            {
                return sortedDictionary;
            }
        }

        private object GetBoundaryValue(Type type)
        {
            if (type == null)
            {
                return null;
            }
            if (!_boundaryValues.TryGetValue(type.Name, out var value))
            {
                return null;
            }
            if (value == null || value.Length == 0)
            {
                return null;
            }
            var num = _random.NextInt32(value.Length - 1);
            if (type.IsInstanceOfType(value[num]))
            {
                return value[num];
            }
            return null;
        }

        private int GetRandomRange(int min, int max)
        {
            if (min < 0)
            {
                min = 0;
            }
            if (max < 0)
            {
                max = 0;
            }
            if (min >= max)
            {
                return min;
            }
            return _random.NextInt32(max - min) + min;
        }

        private object GetRandom(Type expectedType)
        {
            var random = GetRandom(Ua.TypeInfo.Construct(expectedType).BuiltInType);
            if (expectedType == typeof(Uuid))
            {
                return new Uuid((Guid)random);
            }
            return random;
        }

        private string GetRandomLocale()
        {
            var num = _random.NextInt32(_availableLocales.Length - 1);
            return _availableLocales[num];
        }

        private string CreateString(string locale, bool isSymbol)
        {
            if (!_tokenValues.TryGetValue(locale, out var value))
            {
                value = _tokenValues["en-US"];
            }
            var num = (!isSymbol) ? (_random.NextInt32(MaxStringLength) + 1) : (_random.NextInt32(2) + 1);
            var stringBuilder = new StringBuilder();
            while (stringBuilder.Length < num)
            {
                if (!isSymbol && stringBuilder.Length > 0)
                {
                    stringBuilder.Append(' ');
                }
                var num2 = _random.NextInt32(value.Length - 1);
                stringBuilder.Append(value[num2]);
                if (!isSymbol && _random.NextInt32(1) != 0)
                {
                    num2 = _random.NextInt32("`~!@#$%^&*()_-+={}[]:\"';?><,./".Length - 1);
                    stringBuilder.Append("`~!@#$%^&*()_-+={}[]:\"';?><,./"[num2]);
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Boundary value holder
        /// </summary>
        private sealed class BoundaryValues
        {
            public Type SystemType { get; set; }

            public List<object> Values { get; set; }

            public BoundaryValues(Type systemType, params object[] values)
            {
                SystemType = systemType;
                if (values != null)
                {
                    Values = new List<object>(values);
                }
                else
                {
                    Values = new List<object>();
                }
            }
        }

        private static readonly BoundaryValues[] kAvailableBoundaryValues = new[] {
            new BoundaryValues(typeof(sbyte), sbyte.MinValue, (sbyte)0, sbyte.MaxValue),
            new BoundaryValues(typeof(byte), (byte)0, byte.MaxValue),
            new BoundaryValues(typeof(short), short.MinValue, (short)0, short.MaxValue),
            new BoundaryValues(typeof(ushort), (ushort)0, ushort.MaxValue),
            new BoundaryValues(typeof(int), -2147483648, 0, 2147483647),
            new BoundaryValues(typeof(uint), 0u, uint.MaxValue),
            new BoundaryValues(typeof(long), -9223372036854775808L, 0L, 9223372036854775807L),
            new BoundaryValues(typeof(ulong), 0uL, ulong.MaxValue),
            new BoundaryValues(typeof(float), 1.401298E-45f, 3.40282347E+38f, -3.40282347E+38f,
                float.NaN, float.NegativeInfinity, float.PositiveInfinity, 0f),
            new BoundaryValues(typeof(double), 4.94065645841247E-324, 1.7976931348623157E+308,
                -1.7976931348623157E+308, double.NaN, double.NegativeInfinity,
                double.PositiveInfinity, 0.0),
            new BoundaryValues(typeof(string), string.Empty),
         // TODO:  Fix
         //   new BoundaryValues(typeof(DateTime), DateTime.MinValue, DateTime.MaxValue,
         //       new DateTime(1099, 1, 1), new DateTime(2039, 4, 4),
         //       new DateTime(2001, 9, 11, 9, 15, 0, DateTimeKind.Local)),
            new BoundaryValues(typeof(byte[]), Array.Empty<byte>()),
            new BoundaryValues(typeof(StatusCode), 0u, 1073741824u, 2147483648u)
        };

        private readonly IRandomSource _random;
        private readonly SortedDictionary<string, object[]> _boundaryValues;
        private readonly bool _useBoundaryValues = true;
        private readonly string[] _availableLocales;
        private readonly SortedDictionary<string, string[]> _tokenValues;
    }
}
