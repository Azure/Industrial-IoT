// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public sealed class ModbusForm : Form
    {
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
