/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlcModel
{
    #region PlcDataType Class
#if (!OPCUA_EXCLUDE_PlcDataType)
    /// <summary>
    /// Temperature in °C, pressure in Pa and heater state.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = PlcModel.Namespaces.Plc)]
    public partial class PlcDataType : IEncodeable
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public PlcDataType()
        {
            Initialize();
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
            m_temperature = new PlcTemperatureType();
            m_pressure = (int)0;
            m_heaterState = PlcHeaterStateType.Off;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "Temperature", IsRequired = false, Order = 1)]
        public PlcTemperatureType Temperature
        {
            get
            {
                return m_temperature;
            }

            set
            {
                m_temperature = value;

                if (value == null)
                {
                    m_temperature = new PlcTemperatureType();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Pressure", IsRequired = false, Order = 2)]
        public int Pressure
        {
            get { return m_pressure; }
            set { m_pressure = value; }
        }

        /// <remarks />
        [DataMember(Name = "HeaterState", IsRequired = false, Order = 3)]
        public PlcHeaterStateType HeaterState
        {
            get { return m_heaterState; }
            set { m_heaterState = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId
        {
            get { return DataTypeIds.PlcDataType; }
        }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId
        {
            get { return ObjectIds.PlcDataType_Encoding_DefaultBinary; }
        }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId
        {
            get { return ObjectIds.PlcDataType_Encoding_DefaultXml; }
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(PlcModel.Namespaces.Plc);

            encoder.WriteEncodeable("Temperature", Temperature, typeof(PlcTemperatureType));
            encoder.WriteInt32("Pressure", Pressure);
            encoder.WriteEnumerated("HeaterState", HeaterState);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(PlcModel.Namespaces.Plc);

            Temperature = (PlcTemperatureType)decoder.ReadEncodeable("Temperature", typeof(PlcTemperatureType));
            Pressure = decoder.ReadInt32("Pressure");
            HeaterState = (PlcHeaterStateType)decoder.ReadEnumerated("HeaterState", typeof(PlcHeaterStateType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            PlcDataType value = encodeable as PlcDataType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_temperature, value.m_temperature)) return false;
            if (!Utils.IsEqual(m_pressure, value.m_pressure)) return false;
            if (!Utils.IsEqual(m_heaterState, value.m_heaterState)) return false;

            return true;
        }

#if !NET_STANDARD
        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (PlcDataType)this.MemberwiseClone();
        }
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            PlcDataType clone = (PlcDataType)base.MemberwiseClone();

            clone.m_temperature = (PlcTemperatureType)Utils.Clone(this.m_temperature);
            clone.m_pressure = (int)Utils.Clone(this.m_pressure);
            clone.m_heaterState = (PlcHeaterStateType)Utils.Clone(this.m_heaterState);

            return clone;
        }
        #endregion

        #region Private Fields
        private PlcTemperatureType m_temperature;
        private int m_pressure;
        private PlcHeaterStateType m_heaterState;
        #endregion
    }

    #region PlcDataTypeCollection Class
    /// <summary>
    /// A collection of PlcDataType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfPlcDataType", Namespace = PlcModel.Namespaces.Plc, ItemName = "PlcDataType")]
#if !NET_STANDARD
    public partial class PlcDataTypeCollection : List<PlcDataType>, ICloneable
#else
    public partial class PlcDataTypeCollection : List<PlcDataType>
#endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public PlcDataTypeCollection() { }

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public PlcDataTypeCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public PlcDataTypeCollection(IEnumerable<PlcDataType> collection) : base(collection) { }
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator PlcDataTypeCollection(PlcDataType[] values)
        {
            if (values != null)
            {
                return new PlcDataTypeCollection(values);
            }

            return new PlcDataTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator PlcDataType[](PlcDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

#if !NET_STANDARD
        #region ICloneable Methods
        /// <summary>
        /// Creates a deep copy of the collection.
        /// </summary>
        public object Clone()
        {
            return (PlcDataTypeCollection)this.MemberwiseClone();
        }
        #endregion
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            PlcDataTypeCollection clone = new PlcDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((PlcDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
#endif
    #endregion

    #region PlcTemperatureType Class
#if (!OPCUA_EXCLUDE_PlcTemperatureType)
    /// <summary>
    /// Temperature in °C next to the heater at the bottom, and away from the heater at the top.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = PlcModel.Namespaces.Plc)]
    public partial class PlcTemperatureType : IEncodeable
    {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public PlcTemperatureType()
        {
            Initialize();
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
            m_top = (int)0;
            m_bottom = (int)0;
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "Top", IsRequired = false, Order = 1)]
        public int Top
        {
            get { return m_top; }
            set { m_top = value; }
        }

        /// <remarks />
        [DataMember(Name = "Bottom", IsRequired = false, Order = 2)]
        public int Bottom
        {
            get { return m_bottom; }
            set { m_bottom = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId
        {
            get { return DataTypeIds.PlcTemperatureType; }
        }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId
        {
            get { return ObjectIds.PlcTemperatureType_Encoding_DefaultBinary; }
        }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId
        {
            get { return ObjectIds.PlcTemperatureType_Encoding_DefaultXml; }
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(PlcModel.Namespaces.Plc);

            encoder.WriteInt32("Top", Top);
            encoder.WriteInt32("Bottom", Bottom);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(PlcModel.Namespaces.Plc);

            Top = decoder.ReadInt32("Top");
            Bottom = decoder.ReadInt32("Bottom");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            PlcTemperatureType value = encodeable as PlcTemperatureType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_top, value.m_top)) return false;
            if (!Utils.IsEqual(m_bottom, value.m_bottom)) return false;

            return true;
        }

#if !NET_STANDARD
        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (PlcTemperatureType)this.MemberwiseClone();
        }
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            PlcTemperatureType clone = (PlcTemperatureType)base.MemberwiseClone();

            clone.m_top = (int)Utils.Clone(this.m_top);
            clone.m_bottom = (int)Utils.Clone(this.m_bottom);

            return clone;
        }
        #endregion

        #region Private Fields
        private int m_top;
        private int m_bottom;
        #endregion
    }

    #region PlcTemperatureTypeCollection Class
    /// <summary>
    /// A collection of PlcTemperatureType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfPlcTemperatureType", Namespace = PlcModel.Namespaces.Plc, ItemName = "PlcTemperatureType")]
#if !NET_STANDARD
    public partial class PlcTemperatureTypeCollection : List<PlcTemperatureType>, ICloneable
#else
    public partial class PlcTemperatureTypeCollection : List<PlcTemperatureType>
#endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public PlcTemperatureTypeCollection() { }

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public PlcTemperatureTypeCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public PlcTemperatureTypeCollection(IEnumerable<PlcTemperatureType> collection) : base(collection) { }
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator PlcTemperatureTypeCollection(PlcTemperatureType[] values)
        {
            if (values != null)
            {
                return new PlcTemperatureTypeCollection(values);
            }

            return new PlcTemperatureTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator PlcTemperatureType[](PlcTemperatureTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

#if !NET_STANDARD
        #region ICloneable Methods
        /// <summary>
        /// Creates a deep copy of the collection.
        /// </summary>
        public object Clone()
        {
            return (PlcTemperatureTypeCollection)this.MemberwiseClone();
        }
        #endregion
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            PlcTemperatureTypeCollection clone = new PlcTemperatureTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((PlcTemperatureType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
#endif
    #endregion

    #region PlcHeaterStateType Enumeration
#if (!OPCUA_EXCLUDE_PlcHeaterStateType)
    /// <summary>
    /// Heater working state.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = PlcModel.Namespaces.Plc)]
    public enum PlcHeaterStateType
    {
        /// <remarks />
        [EnumMember(Value = "Off_0")]
        Off = 0,

        /// <remarks />
        [EnumMember(Value = "On_1")]
        On = 1,
    }

    #region PlcHeaterStateTypeCollection Class
    /// <summary>
    /// A collection of PlcHeaterStateType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfPlcHeaterStateType", Namespace = PlcModel.Namespaces.Plc, ItemName = "PlcHeaterStateType")]
#if !NET_STANDARD
    public partial class PlcHeaterStateTypeCollection : List<PlcHeaterStateType>, ICloneable
#else
    public partial class PlcHeaterStateTypeCollection : List<PlcHeaterStateType>
#endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public PlcHeaterStateTypeCollection() { }

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public PlcHeaterStateTypeCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public PlcHeaterStateTypeCollection(IEnumerable<PlcHeaterStateType> collection) : base(collection) { }
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator PlcHeaterStateTypeCollection(PlcHeaterStateType[] values)
        {
            if (values != null)
            {
                return new PlcHeaterStateTypeCollection(values);
            }

            return new PlcHeaterStateTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator PlcHeaterStateType[](PlcHeaterStateTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

#if !NET_STANDARD
        #region ICloneable Methods
        /// <summary>
        /// Creates a deep copy of the collection.
        /// </summary>
        public object Clone()
        {
            return (PlcHeaterStateTypeCollection)this.MemberwiseClone();
        }
        #endregion
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            PlcHeaterStateTypeCollection clone = new PlcHeaterStateTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((PlcHeaterStateType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
#endif
    #endregion
}