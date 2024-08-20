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

#nullable enable

namespace Asset
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public class ThingDescription
    {
        [JsonProperty("@context")]
#pragma warning disable CA1819 // Properties should not return arrays
        public object[]? Context { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("securityDefinitions")]
        public SecurityDefinitions? SecurityDefinitions { get; set; }

        [JsonProperty("security")]
#pragma warning disable CA1819 // Properties should not return arrays
        public string[]? Security { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [JsonProperty("@type")]
#pragma warning disable CA1819 // Properties should not return arrays
        public string[]? Type { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("base")]
        public string? Base { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("properties")]
#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, Property>? Properties { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }

    public class OpcUaNamespaces
    {
        [JsonProperty("opcua")]
#pragma warning disable CA1819 // Properties should not return arrays
        public Uri[]? Namespaces { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    public class Property
    {
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }

        [JsonProperty("opcua:nodeId")]
        public string? OpcUaNodeId { get; set; }

        [JsonProperty("opcua:type")]
        public string? OpcUaType { get; set; }

        [JsonProperty("readOnly")]
        public bool ReadOnly { get; set; }

        [JsonProperty("observable")]
        public bool Observable { get; set; }

        [JsonProperty("forms")]
#pragma warning disable CA1819 // Properties should not return arrays
        public object[]? Forms { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum Op
    {
        [EnumMember(Value = "readproperty")]
        ReadProperty,
        [EnumMember(Value = "writeproperty")]
        WriteProperty,
        [EnumMember(Value = "observeproperty")]
        ObserveProperty,
        [EnumMember(Value = "unobserveproperty")]
        UnobserveProperty,
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum TypeEnum
    {
        [EnumMember(Value = "number")]
        Number
    }

    public class GenericForm
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
        [JsonProperty("op")]
#pragma warning disable CA1819 // Properties should not return arrays
        public Op[]? Op { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }

    public class SecurityDefinitions
    {
        [JsonProperty("nosec_sc")]
        public NosecSc? NosecSc { get; set; }
    }

    public class NosecSc
    {
        [JsonProperty("scheme")]
        public string? Scheme { get; set; }
    }

    public class ModbusForm
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
        [JsonProperty("op")]
#pragma warning disable CA1819 // Properties should not return arrays
        public Op[]? Op { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
        [JsonProperty("modv:type")]
        public ModbusType PayloadType { get; set; }
        [JsonProperty("modv:entity")]
        public ModbusEntity? Entity { get; set; }
        [JsonProperty("modv:function")]
        public ModbusFunction? Function { get; set; }
        [JsonProperty("modv:timeout")]
        public int? Timeout { get; set; }
        [JsonProperty("modv:mostSignificantByte")]
        public bool? MostSignificantByte { get; set; }
        [JsonProperty("modv:mostSignificantWord")]
        public bool? MostSignificantWord { get; set; }
        [JsonProperty("modv:zeroBasedAddressing")]
        public bool? ZeroBasedAddressing { get; set; }
        [JsonProperty("modv:pollingTime")]
        public long ModbusPollingTime { get; set; }
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ModbusEntity
    {
        [EnumMember(Value = "Coil")]
        Coil,
        [EnumMember(Value = "DiscreteInput")]
        DiscreteInput,
        [EnumMember(Value = "HoldingRegister")]
        HoldingRegister,
        [EnumMember(Value = "InputRegister")]
        InputRegister,
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ModbusFunction
    {
        [EnumMember(Value = "readCoil")]
        ReadCoil = 1,
        [EnumMember(Value = "readDiscreteInput")]
        ReadDiscreteInput = 2,
        [EnumMember(Value = "readHoldingRegisters")]
        ReadHoldingRegisters = 3,
        [EnumMember(Value = "readInputRegisters")]
        ReadInputRegisters = 4,
        [EnumMember(Value = "writeSingleCoil")]
        WriteSingleCoil = 5,
        [EnumMember(Value = "writeSingleHoldingRegister")]
        WriteSingleHoldingRegister = 6,
        [EnumMember(Value = "writeMultipleCoils")]
        WriteMultipleCoils = 15,
        [EnumMember(Value = "writeMultipleHoldingRegisters")]
        WriteMultipleHoldingRegisters = 16,

        // Not needed
        // [EnumMember(Value = "readWriteMultipleRegisters")]
        // ReadWriteMultipleRegisters = 23,
        // [EnumMember(Value = "readFifoQueue")]
        // ReadFifoQueue = 24,
        // [EnumMember(Value = "readDeviceIdentification")]
        // ReadDeviceIdentification = 43
    }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ModbusType
    {
        [EnumMember(Value = "xsd:integer")]
        Xsdinteger,
        [EnumMember(Value = "xsd:boolean")]
        Xsdboolean,
        [EnumMember(Value = "xsd:string")]
        Xsdstring,
        [EnumMember(Value = "xsd:float")]
        Xsdfloat,
        [EnumMember(Value = "xsd:decimal")]
        Xsddecimal,
        [EnumMember(Value = "xsd:byte")]
        Xsdbyte,
        [EnumMember(Value = "xsd:short")]
        Xsdshort,
        [EnumMember(Value = "xsd:int")]
        Xsdint,
        [EnumMember(Value = "xsd:long")]
        Xsdlong,
        [EnumMember(Value = "xsd:unsignedByte")]
        XsdunsignedByte,
        [EnumMember(Value = "xsd:unsignedShort")]
        XsdunsignedShort,
        [EnumMember(Value = "xsd:unsignedInt")]
        XsdunsignedInt,
        [EnumMember(Value = "xsd:unsignedLong")]
        XsdunsignedLong,
        [EnumMember(Value = "xsd:double")]
        Xsddouble,
        [EnumMember(Value = "xsd:hexBinary")]
        XsdhexBinary,
    }
}
