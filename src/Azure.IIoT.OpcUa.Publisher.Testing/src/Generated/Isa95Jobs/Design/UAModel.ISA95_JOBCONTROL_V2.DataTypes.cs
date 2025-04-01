/* ========================================================================
 * Copyright (c) 2005-2024 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace UAModel.ISA95_JOBCONTROL_V2
{
    #region ISA95EquipmentDataType Class
    #if (!OPCUA_EXCLUDE_ISA95EquipmentDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95EquipmentDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        EquipmentUse = 0x2,
        /// <remarks />
        Quantity = 0x4,
        /// <remarks />
        EngineeringUnits = 0x8,
        /// <remarks />
        Properties = 0x10
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95EquipmentDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95EquipmentDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95EquipmentDataTypeFields.None;
            m_iD = null;
            m_description = new LocalizedTextCollection();
            m_equipmentUse = null;
            m_quantity = null;
            m_engineeringUnits = new Opc.Ua.EUInformation();
            m_properties = new ISA95PropertyDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95EquipmentDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "ID", IsRequired = false, Order = 1)]
        public string ID
        {
            get { return m_iD;  }
            set { m_iD = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 2)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "EquipmentUse", IsRequired = false, Order = 3)]
        public string EquipmentUse
        {
            get { return m_equipmentUse;  }
            set { m_equipmentUse = value; }
        }

        /// <remarks />
        [DataMember(Name = "Quantity", IsRequired = false, Order = 4)]
        public string Quantity
        {
            get { return m_quantity;  }
            set { m_quantity = value; }
        }

        /// <remarks />
        [DataMember(Name = "EngineeringUnits", IsRequired = false, Order = 5)]
        public Opc.Ua.EUInformation EngineeringUnits
        {
            get
            {
                return m_engineeringUnits;
            }

            set
            {
                m_engineeringUnits = value;

                if (value == null)
                {
                    m_engineeringUnits = new Opc.Ua.EUInformation();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Properties", IsRequired = false, Order = 6)]
        public ISA95PropertyDataTypeCollection Properties
        {
            get
            {
                return m_properties;
            }

            set
            {
                m_properties = value;

                if (value == null)
                {
                    m_properties = new ISA95PropertyDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95EquipmentDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95EquipmentDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95EquipmentDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95EquipmentDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("ID", ID);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EquipmentUse) != 0) encoder.WriteString("EquipmentUse", EquipmentUse);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Quantity) != 0) encoder.WriteString("Quantity", Quantity);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EngineeringUnits) != 0) encoder.WriteEncodeable("EngineeringUnits", EngineeringUnits, typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Properties) != 0) encoder.WriteEncodeableArray("Properties", Properties.ToArray(), typeof(ISA95PropertyDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95EquipmentDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            ID = decoder.ReadString("ID");
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EquipmentUse) != 0) EquipmentUse = decoder.ReadString("EquipmentUse");
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Quantity) != 0) Quantity = decoder.ReadString("Quantity");
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EngineeringUnits) != 0) EngineeringUnits = (Opc.Ua.EUInformation)decoder.ReadEncodeable("EngineeringUnits", typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Properties) != 0) Properties = (ISA95PropertyDataTypeCollection)decoder.ReadEncodeableArray("Properties", typeof(ISA95PropertyDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95EquipmentDataType value = encodeable as ISA95EquipmentDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_iD, value.m_iD)) return false;
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EquipmentUse) != 0) if (!Utils.IsEqual(m_equipmentUse, value.m_equipmentUse)) return false;
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Quantity) != 0) if (!Utils.IsEqual(m_quantity, value.m_quantity)) return false;
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EngineeringUnits) != 0) if (!Utils.IsEqual(m_engineeringUnits, value.m_engineeringUnits)) return false;
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Properties) != 0) if (!Utils.IsEqual(m_properties, value.m_properties)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95EquipmentDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95EquipmentDataType clone = (ISA95EquipmentDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_iD = (string)Utils.Clone(this.m_iD);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EquipmentUse) != 0) clone.m_equipmentUse = (string)Utils.Clone(this.m_equipmentUse);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Quantity) != 0) clone.m_quantity = (string)Utils.Clone(this.m_quantity);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.EngineeringUnits) != 0) clone.m_engineeringUnits = (Opc.Ua.EUInformation)Utils.Clone(this.m_engineeringUnits);
            if ((EncodingMask & ISA95EquipmentDataTypeFields.Properties) != 0) clone.m_properties = (ISA95PropertyDataTypeCollection)Utils.Clone(this.m_properties);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_iD;
        private LocalizedTextCollection m_description;
        private string m_equipmentUse;
        private string m_quantity;
        private Opc.Ua.EUInformation m_engineeringUnits;
        private ISA95PropertyDataTypeCollection m_properties;
        #endregion
    }

    #region ISA95EquipmentDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95EquipmentDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95EquipmentDataType")]
    public partial class ISA95EquipmentDataTypeCollection : List<ISA95EquipmentDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95EquipmentDataTypeCollection() {}

        /// <remarks />
        public ISA95EquipmentDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95EquipmentDataTypeCollection(IEnumerable<ISA95EquipmentDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95EquipmentDataTypeCollection(ISA95EquipmentDataType[] values)
        {
            if (values != null)
            {
                return new ISA95EquipmentDataTypeCollection(values);
            }

            return new ISA95EquipmentDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95EquipmentDataType[](ISA95EquipmentDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95EquipmentDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95EquipmentDataTypeCollection clone = new ISA95EquipmentDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95EquipmentDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95JobOrderAndStateDataType Class
    #if (!OPCUA_EXCLUDE_ISA95JobOrderAndStateDataType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95JobOrderAndStateDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderAndStateDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            m_jobOrder = new ISA95JobOrderDataType();
            m_state = new ISA95StateDataTypeCollection();
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "JobOrder", IsRequired = false, Order = 1)]
        public ISA95JobOrderDataType JobOrder
        {
            get
            {
                return m_jobOrder;
            }

            set
            {
                m_jobOrder = value;

                if (value == null)
                {
                    m_jobOrder = new ISA95JobOrderDataType();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "State", IsRequired = false, Order = 2)]
        public ISA95StateDataTypeCollection State
        {
            get
            {
                return m_state;
            }

            set
            {
                m_state = value;

                if (value == null)
                {
                    m_state = new ISA95StateDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95JobOrderAndStateDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95JobOrderAndStateDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95JobOrderAndStateDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95JobOrderAndStateDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            encoder.WriteEncodeable("JobOrder", JobOrder, typeof(ISA95JobOrderDataType));
            encoder.WriteEncodeableArray("State", State.ToArray(), typeof(ISA95StateDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            JobOrder = (ISA95JobOrderDataType)decoder.ReadEncodeable("JobOrder", typeof(ISA95JobOrderDataType));
            State = (ISA95StateDataTypeCollection)decoder.ReadEncodeableArray("State", typeof(ISA95StateDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95JobOrderAndStateDataType value = encodeable as ISA95JobOrderAndStateDataType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_jobOrder, value.m_jobOrder)) return false;
            if (!Utils.IsEqual(m_state, value.m_state)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95JobOrderAndStateDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95JobOrderAndStateDataType clone = (ISA95JobOrderAndStateDataType)base.MemberwiseClone();

            clone.m_jobOrder = (ISA95JobOrderDataType)Utils.Clone(this.m_jobOrder);
            clone.m_state = (ISA95StateDataTypeCollection)Utils.Clone(this.m_state);

            return clone;
        }
        #endregion

        #region Private Fields
        private ISA95JobOrderDataType m_jobOrder;
        private ISA95StateDataTypeCollection m_state;
        #endregion
    }

    #region ISA95JobOrderAndStateDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95JobOrderAndStateDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95JobOrderAndStateDataType")]
    public partial class ISA95JobOrderAndStateDataTypeCollection : List<ISA95JobOrderAndStateDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderAndStateDataTypeCollection() {}

        /// <remarks />
        public ISA95JobOrderAndStateDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95JobOrderAndStateDataTypeCollection(IEnumerable<ISA95JobOrderAndStateDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95JobOrderAndStateDataTypeCollection(ISA95JobOrderAndStateDataType[] values)
        {
            if (values != null)
            {
                return new ISA95JobOrderAndStateDataTypeCollection(values);
            }

            return new ISA95JobOrderAndStateDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95JobOrderAndStateDataType[](ISA95JobOrderAndStateDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95JobOrderAndStateDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95JobOrderAndStateDataTypeCollection clone = new ISA95JobOrderAndStateDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95JobOrderAndStateDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95JobOrderDataType Class
    #if (!OPCUA_EXCLUDE_ISA95JobOrderDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95JobOrderDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        WorkMasterID = 0x2,
        /// <remarks />
        StartTime = 0x4,
        /// <remarks />
        EndTime = 0x8,
        /// <remarks />
        Priority = 0x10,
        /// <remarks />
        JobOrderParameters = 0x20,
        /// <remarks />
        PersonnelRequirements = 0x40,
        /// <remarks />
        EquipmentRequirements = 0x80,
        /// <remarks />
        PhysicalAssetRequirements = 0x100,
        /// <remarks />
        MaterialRequirements = 0x200
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95JobOrderDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95JobOrderDataTypeFields.None;
            m_jobOrderID = null;
            m_description = new LocalizedTextCollection();
            m_workMasterID = new ISA95WorkMasterDataTypeCollection();
            m_startTime = DateTime.MinValue;
            m_endTime = DateTime.MinValue;
            m_priority = (short)0;
            m_jobOrderParameters = new ISA95ParameterDataTypeCollection();
            m_personnelRequirements = new ISA95PersonnelDataTypeCollection();
            m_equipmentRequirements = new ISA95EquipmentDataTypeCollection();
            m_physicalAssetRequirements = new ISA95PhysicalAssetDataTypeCollection();
            m_materialRequirements = new ISA95MaterialDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95JobOrderDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "JobOrderID", IsRequired = false, Order = 1)]
        public string JobOrderID
        {
            get { return m_jobOrderID;  }
            set { m_jobOrderID = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 2)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "WorkMasterID", IsRequired = false, Order = 3)]
        public ISA95WorkMasterDataTypeCollection WorkMasterID
        {
            get
            {
                return m_workMasterID;
            }

            set
            {
                m_workMasterID = value;

                if (value == null)
                {
                    m_workMasterID = new ISA95WorkMasterDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "StartTime", IsRequired = false, Order = 4)]
        public DateTime StartTime
        {
            get { return m_startTime;  }
            set { m_startTime = value; }
        }

        /// <remarks />
        [DataMember(Name = "EndTime", IsRequired = false, Order = 5)]
        public DateTime EndTime
        {
            get { return m_endTime;  }
            set { m_endTime = value; }
        }

        /// <remarks />
        [DataMember(Name = "Priority", IsRequired = false, Order = 6)]
        public short Priority
        {
            get { return m_priority;  }
            set { m_priority = value; }
        }

        /// <remarks />
        [DataMember(Name = "JobOrderParameters", IsRequired = false, Order = 7)]
        public ISA95ParameterDataTypeCollection JobOrderParameters
        {
            get
            {
                return m_jobOrderParameters;
            }

            set
            {
                m_jobOrderParameters = value;

                if (value == null)
                {
                    m_jobOrderParameters = new ISA95ParameterDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "PersonnelRequirements", IsRequired = false, Order = 8)]
        public ISA95PersonnelDataTypeCollection PersonnelRequirements
        {
            get
            {
                return m_personnelRequirements;
            }

            set
            {
                m_personnelRequirements = value;

                if (value == null)
                {
                    m_personnelRequirements = new ISA95PersonnelDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "EquipmentRequirements", IsRequired = false, Order = 9)]
        public ISA95EquipmentDataTypeCollection EquipmentRequirements
        {
            get
            {
                return m_equipmentRequirements;
            }

            set
            {
                m_equipmentRequirements = value;

                if (value == null)
                {
                    m_equipmentRequirements = new ISA95EquipmentDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "PhysicalAssetRequirements", IsRequired = false, Order = 10)]
        public ISA95PhysicalAssetDataTypeCollection PhysicalAssetRequirements
        {
            get
            {
                return m_physicalAssetRequirements;
            }

            set
            {
                m_physicalAssetRequirements = value;

                if (value == null)
                {
                    m_physicalAssetRequirements = new ISA95PhysicalAssetDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "MaterialRequirements", IsRequired = false, Order = 11)]
        public ISA95MaterialDataTypeCollection MaterialRequirements
        {
            get
            {
                return m_materialRequirements;
            }

            set
            {
                m_materialRequirements = value;

                if (value == null)
                {
                    m_materialRequirements = new ISA95MaterialDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95JobOrderDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95JobOrderDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95JobOrderDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95JobOrderDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("JobOrderID", JobOrderID);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.WorkMasterID) != 0) encoder.WriteEncodeableArray("WorkMasterID", WorkMasterID.ToArray(), typeof(ISA95WorkMasterDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.StartTime) != 0) encoder.WriteDateTime("StartTime", StartTime);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EndTime) != 0) encoder.WriteDateTime("EndTime", EndTime);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Priority) != 0) encoder.WriteInt16("Priority", Priority);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.JobOrderParameters) != 0) encoder.WriteEncodeableArray("JobOrderParameters", JobOrderParameters.ToArray(), typeof(ISA95ParameterDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PersonnelRequirements) != 0) encoder.WriteEncodeableArray("PersonnelRequirements", PersonnelRequirements.ToArray(), typeof(ISA95PersonnelDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EquipmentRequirements) != 0) encoder.WriteEncodeableArray("EquipmentRequirements", EquipmentRequirements.ToArray(), typeof(ISA95EquipmentDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PhysicalAssetRequirements) != 0) encoder.WriteEncodeableArray("PhysicalAssetRequirements", PhysicalAssetRequirements.ToArray(), typeof(ISA95PhysicalAssetDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.MaterialRequirements) != 0) encoder.WriteEncodeableArray("MaterialRequirements", MaterialRequirements.ToArray(), typeof(ISA95MaterialDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95JobOrderDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            JobOrderID = decoder.ReadString("JobOrderID");
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95JobOrderDataTypeFields.WorkMasterID) != 0) WorkMasterID = (ISA95WorkMasterDataTypeCollection)decoder.ReadEncodeableArray("WorkMasterID", typeof(ISA95WorkMasterDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.StartTime) != 0) StartTime = decoder.ReadDateTime("StartTime");
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EndTime) != 0) EndTime = decoder.ReadDateTime("EndTime");
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Priority) != 0) Priority = decoder.ReadInt16("Priority");
            if ((EncodingMask & ISA95JobOrderDataTypeFields.JobOrderParameters) != 0) JobOrderParameters = (ISA95ParameterDataTypeCollection)decoder.ReadEncodeableArray("JobOrderParameters", typeof(ISA95ParameterDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PersonnelRequirements) != 0) PersonnelRequirements = (ISA95PersonnelDataTypeCollection)decoder.ReadEncodeableArray("PersonnelRequirements", typeof(ISA95PersonnelDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EquipmentRequirements) != 0) EquipmentRequirements = (ISA95EquipmentDataTypeCollection)decoder.ReadEncodeableArray("EquipmentRequirements", typeof(ISA95EquipmentDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PhysicalAssetRequirements) != 0) PhysicalAssetRequirements = (ISA95PhysicalAssetDataTypeCollection)decoder.ReadEncodeableArray("PhysicalAssetRequirements", typeof(ISA95PhysicalAssetDataType));
            if ((EncodingMask & ISA95JobOrderDataTypeFields.MaterialRequirements) != 0) MaterialRequirements = (ISA95MaterialDataTypeCollection)decoder.ReadEncodeableArray("MaterialRequirements", typeof(ISA95MaterialDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95JobOrderDataType value = encodeable as ISA95JobOrderDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_jobOrderID, value.m_jobOrderID)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.WorkMasterID) != 0) if (!Utils.IsEqual(m_workMasterID, value.m_workMasterID)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.StartTime) != 0) if (!Utils.IsEqual(m_startTime, value.m_startTime)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EndTime) != 0) if (!Utils.IsEqual(m_endTime, value.m_endTime)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Priority) != 0) if (!Utils.IsEqual(m_priority, value.m_priority)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.JobOrderParameters) != 0) if (!Utils.IsEqual(m_jobOrderParameters, value.m_jobOrderParameters)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PersonnelRequirements) != 0) if (!Utils.IsEqual(m_personnelRequirements, value.m_personnelRequirements)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EquipmentRequirements) != 0) if (!Utils.IsEqual(m_equipmentRequirements, value.m_equipmentRequirements)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PhysicalAssetRequirements) != 0) if (!Utils.IsEqual(m_physicalAssetRequirements, value.m_physicalAssetRequirements)) return false;
            if ((EncodingMask & ISA95JobOrderDataTypeFields.MaterialRequirements) != 0) if (!Utils.IsEqual(m_materialRequirements, value.m_materialRequirements)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95JobOrderDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95JobOrderDataType clone = (ISA95JobOrderDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_jobOrderID = (string)Utils.Clone(this.m_jobOrderID);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.WorkMasterID) != 0) clone.m_workMasterID = (ISA95WorkMasterDataTypeCollection)Utils.Clone(this.m_workMasterID);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.StartTime) != 0) clone.m_startTime = (DateTime)Utils.Clone(this.m_startTime);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EndTime) != 0) clone.m_endTime = (DateTime)Utils.Clone(this.m_endTime);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.Priority) != 0) clone.m_priority = (short)Utils.Clone(this.m_priority);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.JobOrderParameters) != 0) clone.m_jobOrderParameters = (ISA95ParameterDataTypeCollection)Utils.Clone(this.m_jobOrderParameters);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PersonnelRequirements) != 0) clone.m_personnelRequirements = (ISA95PersonnelDataTypeCollection)Utils.Clone(this.m_personnelRequirements);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.EquipmentRequirements) != 0) clone.m_equipmentRequirements = (ISA95EquipmentDataTypeCollection)Utils.Clone(this.m_equipmentRequirements);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.PhysicalAssetRequirements) != 0) clone.m_physicalAssetRequirements = (ISA95PhysicalAssetDataTypeCollection)Utils.Clone(this.m_physicalAssetRequirements);
            if ((EncodingMask & ISA95JobOrderDataTypeFields.MaterialRequirements) != 0) clone.m_materialRequirements = (ISA95MaterialDataTypeCollection)Utils.Clone(this.m_materialRequirements);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_jobOrderID;
        private LocalizedTextCollection m_description;
        private ISA95WorkMasterDataTypeCollection m_workMasterID;
        private DateTime m_startTime;
        private DateTime m_endTime;
        private short m_priority;
        private ISA95ParameterDataTypeCollection m_jobOrderParameters;
        private ISA95PersonnelDataTypeCollection m_personnelRequirements;
        private ISA95EquipmentDataTypeCollection m_equipmentRequirements;
        private ISA95PhysicalAssetDataTypeCollection m_physicalAssetRequirements;
        private ISA95MaterialDataTypeCollection m_materialRequirements;
        #endregion
    }

    #region ISA95JobOrderDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95JobOrderDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95JobOrderDataType")]
    public partial class ISA95JobOrderDataTypeCollection : List<ISA95JobOrderDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95JobOrderDataTypeCollection() {}

        /// <remarks />
        public ISA95JobOrderDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95JobOrderDataTypeCollection(IEnumerable<ISA95JobOrderDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95JobOrderDataTypeCollection(ISA95JobOrderDataType[] values)
        {
            if (values != null)
            {
                return new ISA95JobOrderDataTypeCollection(values);
            }

            return new ISA95JobOrderDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95JobOrderDataType[](ISA95JobOrderDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95JobOrderDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95JobOrderDataTypeCollection clone = new ISA95JobOrderDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95JobOrderDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95JobResponseDataType Class
    #if (!OPCUA_EXCLUDE_ISA95JobResponseDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95JobResponseDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        StartTime = 0x2,
        /// <remarks />
        EndTime = 0x4,
        /// <remarks />
        JobResponseData = 0x8,
        /// <remarks />
        PersonnelActuals = 0x10,
        /// <remarks />
        EquipmentActuals = 0x20,
        /// <remarks />
        PhysicalAssetActuals = 0x40,
        /// <remarks />
        MaterialActuals = 0x80
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95JobResponseDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95JobResponseDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95JobResponseDataTypeFields.None;
            m_jobResponseID = null;
            m_description = null;
            m_jobOrderID = null;
            m_startTime = DateTime.MinValue;
            m_endTime = DateTime.MinValue;
            m_jobState = new ISA95StateDataTypeCollection();
            m_jobResponseData = new ISA95ParameterDataTypeCollection();
            m_personnelActuals = new ISA95PersonnelDataTypeCollection();
            m_equipmentActuals = new ISA95EquipmentDataTypeCollection();
            m_physicalAssetActuals = new ISA95PhysicalAssetDataTypeCollection();
            m_materialActuals = new ISA95MaterialDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95JobResponseDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "JobResponseID", IsRequired = false, Order = 1)]
        public string JobResponseID
        {
            get { return m_jobResponseID;  }
            set { m_jobResponseID = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 2)]
        public LocalizedText Description
        {
            get { return m_description;  }
            set { m_description = value; }
        }

        /// <remarks />
        [DataMember(Name = "JobOrderID", IsRequired = false, Order = 3)]
        public string JobOrderID
        {
            get { return m_jobOrderID;  }
            set { m_jobOrderID = value; }
        }

        /// <remarks />
        [DataMember(Name = "StartTime", IsRequired = false, Order = 4)]
        public DateTime StartTime
        {
            get { return m_startTime;  }
            set { m_startTime = value; }
        }

        /// <remarks />
        [DataMember(Name = "EndTime", IsRequired = false, Order = 5)]
        public DateTime EndTime
        {
            get { return m_endTime;  }
            set { m_endTime = value; }
        }

        /// <remarks />
        [DataMember(Name = "JobState", IsRequired = false, Order = 6)]
        public ISA95StateDataTypeCollection JobState
        {
            get
            {
                return m_jobState;
            }

            set
            {
                m_jobState = value;

                if (value == null)
                {
                    m_jobState = new ISA95StateDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "JobResponseData", IsRequired = false, Order = 7)]
        public ISA95ParameterDataTypeCollection JobResponseData
        {
            get
            {
                return m_jobResponseData;
            }

            set
            {
                m_jobResponseData = value;

                if (value == null)
                {
                    m_jobResponseData = new ISA95ParameterDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "PersonnelActuals", IsRequired = false, Order = 8)]
        public ISA95PersonnelDataTypeCollection PersonnelActuals
        {
            get
            {
                return m_personnelActuals;
            }

            set
            {
                m_personnelActuals = value;

                if (value == null)
                {
                    m_personnelActuals = new ISA95PersonnelDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "EquipmentActuals", IsRequired = false, Order = 9)]
        public ISA95EquipmentDataTypeCollection EquipmentActuals
        {
            get
            {
                return m_equipmentActuals;
            }

            set
            {
                m_equipmentActuals = value;

                if (value == null)
                {
                    m_equipmentActuals = new ISA95EquipmentDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "PhysicalAssetActuals", IsRequired = false, Order = 10)]
        public ISA95PhysicalAssetDataTypeCollection PhysicalAssetActuals
        {
            get
            {
                return m_physicalAssetActuals;
            }

            set
            {
                m_physicalAssetActuals = value;

                if (value == null)
                {
                    m_physicalAssetActuals = new ISA95PhysicalAssetDataTypeCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "MaterialActuals", IsRequired = false, Order = 11)]
        public ISA95MaterialDataTypeCollection MaterialActuals
        {
            get
            {
                return m_materialActuals;
            }

            set
            {
                m_materialActuals = value;

                if (value == null)
                {
                    m_materialActuals = new ISA95MaterialDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95JobResponseDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95JobResponseDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95JobResponseDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95JobResponseDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("JobResponseID", JobResponseID);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.Description) != 0) encoder.WriteLocalizedText("Description", Description);
            encoder.WriteString("JobOrderID", JobOrderID);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.StartTime) != 0) encoder.WriteDateTime("StartTime", StartTime);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EndTime) != 0) encoder.WriteDateTime("EndTime", EndTime);
            encoder.WriteEncodeableArray("JobState", JobState.ToArray(), typeof(ISA95StateDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.JobResponseData) != 0) encoder.WriteEncodeableArray("JobResponseData", JobResponseData.ToArray(), typeof(ISA95ParameterDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PersonnelActuals) != 0) encoder.WriteEncodeableArray("PersonnelActuals", PersonnelActuals.ToArray(), typeof(ISA95PersonnelDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EquipmentActuals) != 0) encoder.WriteEncodeableArray("EquipmentActuals", EquipmentActuals.ToArray(), typeof(ISA95EquipmentDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PhysicalAssetActuals) != 0) encoder.WriteEncodeableArray("PhysicalAssetActuals", PhysicalAssetActuals.ToArray(), typeof(ISA95PhysicalAssetDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.MaterialActuals) != 0) encoder.WriteEncodeableArray("MaterialActuals", MaterialActuals.ToArray(), typeof(ISA95MaterialDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95JobResponseDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            JobResponseID = decoder.ReadString("JobResponseID");
            if ((EncodingMask & ISA95JobResponseDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedText("Description");
            JobOrderID = decoder.ReadString("JobOrderID");
            if ((EncodingMask & ISA95JobResponseDataTypeFields.StartTime) != 0) StartTime = decoder.ReadDateTime("StartTime");
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EndTime) != 0) EndTime = decoder.ReadDateTime("EndTime");
            JobState = (ISA95StateDataTypeCollection)decoder.ReadEncodeableArray("JobState", typeof(ISA95StateDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.JobResponseData) != 0) JobResponseData = (ISA95ParameterDataTypeCollection)decoder.ReadEncodeableArray("JobResponseData", typeof(ISA95ParameterDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PersonnelActuals) != 0) PersonnelActuals = (ISA95PersonnelDataTypeCollection)decoder.ReadEncodeableArray("PersonnelActuals", typeof(ISA95PersonnelDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EquipmentActuals) != 0) EquipmentActuals = (ISA95EquipmentDataTypeCollection)decoder.ReadEncodeableArray("EquipmentActuals", typeof(ISA95EquipmentDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PhysicalAssetActuals) != 0) PhysicalAssetActuals = (ISA95PhysicalAssetDataTypeCollection)decoder.ReadEncodeableArray("PhysicalAssetActuals", typeof(ISA95PhysicalAssetDataType));
            if ((EncodingMask & ISA95JobResponseDataTypeFields.MaterialActuals) != 0) MaterialActuals = (ISA95MaterialDataTypeCollection)decoder.ReadEncodeableArray("MaterialActuals", typeof(ISA95MaterialDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95JobResponseDataType value = encodeable as ISA95JobResponseDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_jobResponseID, value.m_jobResponseID)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if (!Utils.IsEqual(m_jobOrderID, value.m_jobOrderID)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.StartTime) != 0) if (!Utils.IsEqual(m_startTime, value.m_startTime)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EndTime) != 0) if (!Utils.IsEqual(m_endTime, value.m_endTime)) return false;
            if (!Utils.IsEqual(m_jobState, value.m_jobState)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.JobResponseData) != 0) if (!Utils.IsEqual(m_jobResponseData, value.m_jobResponseData)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PersonnelActuals) != 0) if (!Utils.IsEqual(m_personnelActuals, value.m_personnelActuals)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EquipmentActuals) != 0) if (!Utils.IsEqual(m_equipmentActuals, value.m_equipmentActuals)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PhysicalAssetActuals) != 0) if (!Utils.IsEqual(m_physicalAssetActuals, value.m_physicalAssetActuals)) return false;
            if ((EncodingMask & ISA95JobResponseDataTypeFields.MaterialActuals) != 0) if (!Utils.IsEqual(m_materialActuals, value.m_materialActuals)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95JobResponseDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95JobResponseDataType clone = (ISA95JobResponseDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_jobResponseID = (string)Utils.Clone(this.m_jobResponseID);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.Description) != 0) clone.m_description = (LocalizedText)Utils.Clone(this.m_description);
            clone.m_jobOrderID = (string)Utils.Clone(this.m_jobOrderID);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.StartTime) != 0) clone.m_startTime = (DateTime)Utils.Clone(this.m_startTime);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EndTime) != 0) clone.m_endTime = (DateTime)Utils.Clone(this.m_endTime);
            clone.m_jobState = (ISA95StateDataTypeCollection)Utils.Clone(this.m_jobState);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.JobResponseData) != 0) clone.m_jobResponseData = (ISA95ParameterDataTypeCollection)Utils.Clone(this.m_jobResponseData);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PersonnelActuals) != 0) clone.m_personnelActuals = (ISA95PersonnelDataTypeCollection)Utils.Clone(this.m_personnelActuals);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.EquipmentActuals) != 0) clone.m_equipmentActuals = (ISA95EquipmentDataTypeCollection)Utils.Clone(this.m_equipmentActuals);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.PhysicalAssetActuals) != 0) clone.m_physicalAssetActuals = (ISA95PhysicalAssetDataTypeCollection)Utils.Clone(this.m_physicalAssetActuals);
            if ((EncodingMask & ISA95JobResponseDataTypeFields.MaterialActuals) != 0) clone.m_materialActuals = (ISA95MaterialDataTypeCollection)Utils.Clone(this.m_materialActuals);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_jobResponseID;
        private LocalizedText m_description;
        private string m_jobOrderID;
        private DateTime m_startTime;
        private DateTime m_endTime;
        private ISA95StateDataTypeCollection m_jobState;
        private ISA95ParameterDataTypeCollection m_jobResponseData;
        private ISA95PersonnelDataTypeCollection m_personnelActuals;
        private ISA95EquipmentDataTypeCollection m_equipmentActuals;
        private ISA95PhysicalAssetDataTypeCollection m_physicalAssetActuals;
        private ISA95MaterialDataTypeCollection m_materialActuals;
        #endregion
    }

    #region ISA95JobResponseDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95JobResponseDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95JobResponseDataType")]
    public partial class ISA95JobResponseDataTypeCollection : List<ISA95JobResponseDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95JobResponseDataTypeCollection() {}

        /// <remarks />
        public ISA95JobResponseDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95JobResponseDataTypeCollection(IEnumerable<ISA95JobResponseDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95JobResponseDataTypeCollection(ISA95JobResponseDataType[] values)
        {
            if (values != null)
            {
                return new ISA95JobResponseDataTypeCollection(values);
            }

            return new ISA95JobResponseDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95JobResponseDataType[](ISA95JobResponseDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95JobResponseDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95JobResponseDataTypeCollection clone = new ISA95JobResponseDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95JobResponseDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95MaterialDataType Class
    #if (!OPCUA_EXCLUDE_ISA95MaterialDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95MaterialDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        MaterialClassID = 0x1,
        /// <remarks />
        MaterialDefinitionID = 0x2,
        /// <remarks />
        MaterialLotID = 0x4,
        /// <remarks />
        MaterialSublotID = 0x8,
        /// <remarks />
        Description = 0x10,
        /// <remarks />
        MaterialUse = 0x20,
        /// <remarks />
        Quantity = 0x40,
        /// <remarks />
        EngineeringUnits = 0x80,
        /// <remarks />
        Properties = 0x100
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95MaterialDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95MaterialDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95MaterialDataTypeFields.None;
            m_materialClassID = null;
            m_materialDefinitionID = null;
            m_materialLotID = null;
            m_materialSublotID = null;
            m_description = new LocalizedTextCollection();
            m_materialUse = null;
            m_quantity = null;
            m_engineeringUnits = new Opc.Ua.EUInformation();
            m_properties = new ISA95PropertyDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95MaterialDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "MaterialClassID", IsRequired = false, Order = 1)]
        public string MaterialClassID
        {
            get { return m_materialClassID;  }
            set { m_materialClassID = value; }
        }

        /// <remarks />
        [DataMember(Name = "MaterialDefinitionID", IsRequired = false, Order = 2)]
        public string MaterialDefinitionID
        {
            get { return m_materialDefinitionID;  }
            set { m_materialDefinitionID = value; }
        }

        /// <remarks />
        [DataMember(Name = "MaterialLotID", IsRequired = false, Order = 3)]
        public string MaterialLotID
        {
            get { return m_materialLotID;  }
            set { m_materialLotID = value; }
        }

        /// <remarks />
        [DataMember(Name = "MaterialSublotID", IsRequired = false, Order = 4)]
        public string MaterialSublotID
        {
            get { return m_materialSublotID;  }
            set { m_materialSublotID = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 5)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "MaterialUse", IsRequired = false, Order = 6)]
        public string MaterialUse
        {
            get { return m_materialUse;  }
            set { m_materialUse = value; }
        }

        /// <remarks />
        [DataMember(Name = "Quantity", IsRequired = false, Order = 7)]
        public string Quantity
        {
            get { return m_quantity;  }
            set { m_quantity = value; }
        }

        /// <remarks />
        [DataMember(Name = "EngineeringUnits", IsRequired = false, Order = 8)]
        public Opc.Ua.EUInformation EngineeringUnits
        {
            get
            {
                return m_engineeringUnits;
            }

            set
            {
                m_engineeringUnits = value;

                if (value == null)
                {
                    m_engineeringUnits = new Opc.Ua.EUInformation();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Properties", IsRequired = false, Order = 9)]
        public ISA95PropertyDataTypeCollection Properties
        {
            get
            {
                return m_properties;
            }

            set
            {
                m_properties = value;

                if (value == null)
                {
                    m_properties = new ISA95PropertyDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95MaterialDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95MaterialDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95MaterialDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95MaterialDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialClassID) != 0) encoder.WriteString("MaterialClassID", MaterialClassID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialDefinitionID) != 0) encoder.WriteString("MaterialDefinitionID", MaterialDefinitionID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialLotID) != 0) encoder.WriteString("MaterialLotID", MaterialLotID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialSublotID) != 0) encoder.WriteString("MaterialSublotID", MaterialSublotID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialUse) != 0) encoder.WriteString("MaterialUse", MaterialUse);
            if ((EncodingMask & ISA95MaterialDataTypeFields.Quantity) != 0) encoder.WriteString("Quantity", Quantity);
            if ((EncodingMask & ISA95MaterialDataTypeFields.EngineeringUnits) != 0) encoder.WriteEncodeable("EngineeringUnits", EngineeringUnits, typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95MaterialDataTypeFields.Properties) != 0) encoder.WriteEncodeableArray("Properties", Properties.ToArray(), typeof(ISA95PropertyDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95MaterialDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialClassID) != 0) MaterialClassID = decoder.ReadString("MaterialClassID");
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialDefinitionID) != 0) MaterialDefinitionID = decoder.ReadString("MaterialDefinitionID");
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialLotID) != 0) MaterialLotID = decoder.ReadString("MaterialLotID");
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialSublotID) != 0) MaterialSublotID = decoder.ReadString("MaterialSublotID");
            if ((EncodingMask & ISA95MaterialDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialUse) != 0) MaterialUse = decoder.ReadString("MaterialUse");
            if ((EncodingMask & ISA95MaterialDataTypeFields.Quantity) != 0) Quantity = decoder.ReadString("Quantity");
            if ((EncodingMask & ISA95MaterialDataTypeFields.EngineeringUnits) != 0) EngineeringUnits = (Opc.Ua.EUInformation)decoder.ReadEncodeable("EngineeringUnits", typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95MaterialDataTypeFields.Properties) != 0) Properties = (ISA95PropertyDataTypeCollection)decoder.ReadEncodeableArray("Properties", typeof(ISA95PropertyDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95MaterialDataType value = encodeable as ISA95MaterialDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialClassID) != 0) if (!Utils.IsEqual(m_materialClassID, value.m_materialClassID)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialDefinitionID) != 0) if (!Utils.IsEqual(m_materialDefinitionID, value.m_materialDefinitionID)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialLotID) != 0) if (!Utils.IsEqual(m_materialLotID, value.m_materialLotID)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialSublotID) != 0) if (!Utils.IsEqual(m_materialSublotID, value.m_materialSublotID)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialUse) != 0) if (!Utils.IsEqual(m_materialUse, value.m_materialUse)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.Quantity) != 0) if (!Utils.IsEqual(m_quantity, value.m_quantity)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.EngineeringUnits) != 0) if (!Utils.IsEqual(m_engineeringUnits, value.m_engineeringUnits)) return false;
            if ((EncodingMask & ISA95MaterialDataTypeFields.Properties) != 0) if (!Utils.IsEqual(m_properties, value.m_properties)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95MaterialDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95MaterialDataType clone = (ISA95MaterialDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialClassID) != 0) clone.m_materialClassID = (string)Utils.Clone(this.m_materialClassID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialDefinitionID) != 0) clone.m_materialDefinitionID = (string)Utils.Clone(this.m_materialDefinitionID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialLotID) != 0) clone.m_materialLotID = (string)Utils.Clone(this.m_materialLotID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialSublotID) != 0) clone.m_materialSublotID = (string)Utils.Clone(this.m_materialSublotID);
            if ((EncodingMask & ISA95MaterialDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95MaterialDataTypeFields.MaterialUse) != 0) clone.m_materialUse = (string)Utils.Clone(this.m_materialUse);
            if ((EncodingMask & ISA95MaterialDataTypeFields.Quantity) != 0) clone.m_quantity = (string)Utils.Clone(this.m_quantity);
            if ((EncodingMask & ISA95MaterialDataTypeFields.EngineeringUnits) != 0) clone.m_engineeringUnits = (Opc.Ua.EUInformation)Utils.Clone(this.m_engineeringUnits);
            if ((EncodingMask & ISA95MaterialDataTypeFields.Properties) != 0) clone.m_properties = (ISA95PropertyDataTypeCollection)Utils.Clone(this.m_properties);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_materialClassID;
        private string m_materialDefinitionID;
        private string m_materialLotID;
        private string m_materialSublotID;
        private LocalizedTextCollection m_description;
        private string m_materialUse;
        private string m_quantity;
        private Opc.Ua.EUInformation m_engineeringUnits;
        private ISA95PropertyDataTypeCollection m_properties;
        #endregion
    }

    #region ISA95MaterialDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95MaterialDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95MaterialDataType")]
    public partial class ISA95MaterialDataTypeCollection : List<ISA95MaterialDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95MaterialDataTypeCollection() {}

        /// <remarks />
        public ISA95MaterialDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95MaterialDataTypeCollection(IEnumerable<ISA95MaterialDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95MaterialDataTypeCollection(ISA95MaterialDataType[] values)
        {
            if (values != null)
            {
                return new ISA95MaterialDataTypeCollection(values);
            }

            return new ISA95MaterialDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95MaterialDataType[](ISA95MaterialDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95MaterialDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95MaterialDataTypeCollection clone = new ISA95MaterialDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95MaterialDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95ParameterDataType Class
    #if (!OPCUA_EXCLUDE_ISA95ParameterDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95ParameterDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        EngineeringUnits = 0x2,
        /// <remarks />
        Subparameters = 0x4
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95ParameterDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95ParameterDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95ParameterDataTypeFields.None;
            m_iD = null;
            m_value = Variant.Null;
            m_description = new LocalizedTextCollection();
            m_engineeringUnits = new Opc.Ua.EUInformation();
            m_subparameters = new ISA95ParameterDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95ParameterDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "ID", IsRequired = false, Order = 1)]
        public string ID
        {
            get { return m_iD;  }
            set { m_iD = value; }
        }

        /// <remarks />
        [DataMember(Name = "Value", IsRequired = false, Order = 2)]
        public Variant Value
        {
            get { return m_value;  }
            set { m_value = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 3)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "EngineeringUnits", IsRequired = false, Order = 4)]
        public Opc.Ua.EUInformation EngineeringUnits
        {
            get
            {
                return m_engineeringUnits;
            }

            set
            {
                m_engineeringUnits = value;

                if (value == null)
                {
                    m_engineeringUnits = new Opc.Ua.EUInformation();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Subparameters", IsRequired = false, Order = 5)]
        public ISA95ParameterDataTypeCollection Subparameters
        {
            get
            {
                return m_subparameters;
            }

            set
            {
                m_subparameters = value;

                if (value == null)
                {
                    m_subparameters = new ISA95ParameterDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95ParameterDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95ParameterDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95ParameterDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95ParameterDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("ID", ID);
            encoder.WriteVariant("Value", Value);
            if ((EncodingMask & ISA95ParameterDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95ParameterDataTypeFields.EngineeringUnits) != 0) encoder.WriteEncodeable("EngineeringUnits", EngineeringUnits, typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95ParameterDataTypeFields.Subparameters) != 0) encoder.WriteEncodeableArray("Subparameters", Subparameters.ToArray(), typeof(ISA95ParameterDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95ParameterDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            ID = decoder.ReadString("ID");
            Value = decoder.ReadVariant("Value");
            if ((EncodingMask & ISA95ParameterDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95ParameterDataTypeFields.EngineeringUnits) != 0) EngineeringUnits = (Opc.Ua.EUInformation)decoder.ReadEncodeable("EngineeringUnits", typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95ParameterDataTypeFields.Subparameters) != 0) Subparameters = (ISA95ParameterDataTypeCollection)decoder.ReadEncodeableArray("Subparameters", typeof(ISA95ParameterDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95ParameterDataType value = encodeable as ISA95ParameterDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_iD, value.m_iD)) return false;
            if (!Utils.IsEqual(m_value, value.m_value)) return false;
            if ((EncodingMask & ISA95ParameterDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95ParameterDataTypeFields.EngineeringUnits) != 0) if (!Utils.IsEqual(m_engineeringUnits, value.m_engineeringUnits)) return false;
            if ((EncodingMask & ISA95ParameterDataTypeFields.Subparameters) != 0) if (!Utils.IsEqual(m_subparameters, value.m_subparameters)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95ParameterDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95ParameterDataType clone = (ISA95ParameterDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_iD = (string)Utils.Clone(this.m_iD);
            clone.m_value = (Variant)Utils.Clone(this.m_value);
            if ((EncodingMask & ISA95ParameterDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95ParameterDataTypeFields.EngineeringUnits) != 0) clone.m_engineeringUnits = (Opc.Ua.EUInformation)Utils.Clone(this.m_engineeringUnits);
            if ((EncodingMask & ISA95ParameterDataTypeFields.Subparameters) != 0) clone.m_subparameters = (ISA95ParameterDataTypeCollection)Utils.Clone(this.m_subparameters);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_iD;
        private Variant m_value;
        private LocalizedTextCollection m_description;
        private Opc.Ua.EUInformation m_engineeringUnits;
        private ISA95ParameterDataTypeCollection m_subparameters;
        #endregion
    }

    #region ISA95ParameterDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95ParameterDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95ParameterDataType")]
    public partial class ISA95ParameterDataTypeCollection : List<ISA95ParameterDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95ParameterDataTypeCollection() {}

        /// <remarks />
        public ISA95ParameterDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95ParameterDataTypeCollection(IEnumerable<ISA95ParameterDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95ParameterDataTypeCollection(ISA95ParameterDataType[] values)
        {
            if (values != null)
            {
                return new ISA95ParameterDataTypeCollection(values);
            }

            return new ISA95ParameterDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95ParameterDataType[](ISA95ParameterDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95ParameterDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95ParameterDataTypeCollection clone = new ISA95ParameterDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95ParameterDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95PersonnelDataType Class
    #if (!OPCUA_EXCLUDE_ISA95PersonnelDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95PersonnelDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        PersonnelUse = 0x2,
        /// <remarks />
        Quantity = 0x4,
        /// <remarks />
        EngineeringUnits = 0x8,
        /// <remarks />
        Properties = 0x10
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95PersonnelDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95PersonnelDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95PersonnelDataTypeFields.None;
            m_iD = null;
            m_description = new LocalizedTextCollection();
            m_personnelUse = null;
            m_quantity = null;
            m_engineeringUnits = new Opc.Ua.EUInformation();
            m_properties = new ISA95PropertyDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95PersonnelDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "ID", IsRequired = false, Order = 1)]
        public string ID
        {
            get { return m_iD;  }
            set { m_iD = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 2)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "PersonnelUse", IsRequired = false, Order = 3)]
        public string PersonnelUse
        {
            get { return m_personnelUse;  }
            set { m_personnelUse = value; }
        }

        /// <remarks />
        [DataMember(Name = "Quantity", IsRequired = false, Order = 4)]
        public string Quantity
        {
            get { return m_quantity;  }
            set { m_quantity = value; }
        }

        /// <remarks />
        [DataMember(Name = "EngineeringUnits", IsRequired = false, Order = 5)]
        public Opc.Ua.EUInformation EngineeringUnits
        {
            get
            {
                return m_engineeringUnits;
            }

            set
            {
                m_engineeringUnits = value;

                if (value == null)
                {
                    m_engineeringUnits = new Opc.Ua.EUInformation();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Properties", IsRequired = false, Order = 6)]
        public ISA95PropertyDataTypeCollection Properties
        {
            get
            {
                return m_properties;
            }

            set
            {
                m_properties = value;

                if (value == null)
                {
                    m_properties = new ISA95PropertyDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95PersonnelDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95PersonnelDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95PersonnelDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95PersonnelDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("ID", ID);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.PersonnelUse) != 0) encoder.WriteString("PersonnelUse", PersonnelUse);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Quantity) != 0) encoder.WriteString("Quantity", Quantity);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.EngineeringUnits) != 0) encoder.WriteEncodeable("EngineeringUnits", EngineeringUnits, typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Properties) != 0) encoder.WriteEncodeableArray("Properties", Properties.ToArray(), typeof(ISA95PropertyDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95PersonnelDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            ID = decoder.ReadString("ID");
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95PersonnelDataTypeFields.PersonnelUse) != 0) PersonnelUse = decoder.ReadString("PersonnelUse");
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Quantity) != 0) Quantity = decoder.ReadString("Quantity");
            if ((EncodingMask & ISA95PersonnelDataTypeFields.EngineeringUnits) != 0) EngineeringUnits = (Opc.Ua.EUInformation)decoder.ReadEncodeable("EngineeringUnits", typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Properties) != 0) Properties = (ISA95PropertyDataTypeCollection)decoder.ReadEncodeableArray("Properties", typeof(ISA95PropertyDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95PersonnelDataType value = encodeable as ISA95PersonnelDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_iD, value.m_iD)) return false;
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95PersonnelDataTypeFields.PersonnelUse) != 0) if (!Utils.IsEqual(m_personnelUse, value.m_personnelUse)) return false;
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Quantity) != 0) if (!Utils.IsEqual(m_quantity, value.m_quantity)) return false;
            if ((EncodingMask & ISA95PersonnelDataTypeFields.EngineeringUnits) != 0) if (!Utils.IsEqual(m_engineeringUnits, value.m_engineeringUnits)) return false;
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Properties) != 0) if (!Utils.IsEqual(m_properties, value.m_properties)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95PersonnelDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95PersonnelDataType clone = (ISA95PersonnelDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_iD = (string)Utils.Clone(this.m_iD);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.PersonnelUse) != 0) clone.m_personnelUse = (string)Utils.Clone(this.m_personnelUse);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Quantity) != 0) clone.m_quantity = (string)Utils.Clone(this.m_quantity);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.EngineeringUnits) != 0) clone.m_engineeringUnits = (Opc.Ua.EUInformation)Utils.Clone(this.m_engineeringUnits);
            if ((EncodingMask & ISA95PersonnelDataTypeFields.Properties) != 0) clone.m_properties = (ISA95PropertyDataTypeCollection)Utils.Clone(this.m_properties);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_iD;
        private LocalizedTextCollection m_description;
        private string m_personnelUse;
        private string m_quantity;
        private Opc.Ua.EUInformation m_engineeringUnits;
        private ISA95PropertyDataTypeCollection m_properties;
        #endregion
    }

    #region ISA95PersonnelDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95PersonnelDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95PersonnelDataType")]
    public partial class ISA95PersonnelDataTypeCollection : List<ISA95PersonnelDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95PersonnelDataTypeCollection() {}

        /// <remarks />
        public ISA95PersonnelDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95PersonnelDataTypeCollection(IEnumerable<ISA95PersonnelDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95PersonnelDataTypeCollection(ISA95PersonnelDataType[] values)
        {
            if (values != null)
            {
                return new ISA95PersonnelDataTypeCollection(values);
            }

            return new ISA95PersonnelDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95PersonnelDataType[](ISA95PersonnelDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95PersonnelDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95PersonnelDataTypeCollection clone = new ISA95PersonnelDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95PersonnelDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95PhysicalAssetDataType Class
    #if (!OPCUA_EXCLUDE_ISA95PhysicalAssetDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95PhysicalAssetDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        PhysicalAssetUse = 0x2,
        /// <remarks />
        Quantity = 0x4,
        /// <remarks />
        EngineeringUnits = 0x8,
        /// <remarks />
        Properties = 0x10
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95PhysicalAssetDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95PhysicalAssetDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95PhysicalAssetDataTypeFields.None;
            m_iD = null;
            m_description = new LocalizedTextCollection();
            m_physicalAssetUse = null;
            m_quantity = null;
            m_engineeringUnits = new Opc.Ua.EUInformation();
            m_properties = new ISA95PropertyDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95PhysicalAssetDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "ID", IsRequired = false, Order = 1)]
        public string ID
        {
            get { return m_iD;  }
            set { m_iD = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 2)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "PhysicalAssetUse", IsRequired = false, Order = 3)]
        public string PhysicalAssetUse
        {
            get { return m_physicalAssetUse;  }
            set { m_physicalAssetUse = value; }
        }

        /// <remarks />
        [DataMember(Name = "Quantity", IsRequired = false, Order = 4)]
        public string Quantity
        {
            get { return m_quantity;  }
            set { m_quantity = value; }
        }

        /// <remarks />
        [DataMember(Name = "EngineeringUnits", IsRequired = false, Order = 5)]
        public Opc.Ua.EUInformation EngineeringUnits
        {
            get
            {
                return m_engineeringUnits;
            }

            set
            {
                m_engineeringUnits = value;

                if (value == null)
                {
                    m_engineeringUnits = new Opc.Ua.EUInformation();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Properties", IsRequired = false, Order = 6)]
        public ISA95PropertyDataTypeCollection Properties
        {
            get
            {
                return m_properties;
            }

            set
            {
                m_properties = value;

                if (value == null)
                {
                    m_properties = new ISA95PropertyDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95PhysicalAssetDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95PhysicalAssetDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95PhysicalAssetDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95PhysicalAssetDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("ID", ID);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.PhysicalAssetUse) != 0) encoder.WriteString("PhysicalAssetUse", PhysicalAssetUse);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Quantity) != 0) encoder.WriteString("Quantity", Quantity);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.EngineeringUnits) != 0) encoder.WriteEncodeable("EngineeringUnits", EngineeringUnits, typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Properties) != 0) encoder.WriteEncodeableArray("Properties", Properties.ToArray(), typeof(ISA95PropertyDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95PhysicalAssetDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            ID = decoder.ReadString("ID");
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.PhysicalAssetUse) != 0) PhysicalAssetUse = decoder.ReadString("PhysicalAssetUse");
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Quantity) != 0) Quantity = decoder.ReadString("Quantity");
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.EngineeringUnits) != 0) EngineeringUnits = (Opc.Ua.EUInformation)decoder.ReadEncodeable("EngineeringUnits", typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Properties) != 0) Properties = (ISA95PropertyDataTypeCollection)decoder.ReadEncodeableArray("Properties", typeof(ISA95PropertyDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95PhysicalAssetDataType value = encodeable as ISA95PhysicalAssetDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_iD, value.m_iD)) return false;
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.PhysicalAssetUse) != 0) if (!Utils.IsEqual(m_physicalAssetUse, value.m_physicalAssetUse)) return false;
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Quantity) != 0) if (!Utils.IsEqual(m_quantity, value.m_quantity)) return false;
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.EngineeringUnits) != 0) if (!Utils.IsEqual(m_engineeringUnits, value.m_engineeringUnits)) return false;
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Properties) != 0) if (!Utils.IsEqual(m_properties, value.m_properties)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95PhysicalAssetDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95PhysicalAssetDataType clone = (ISA95PhysicalAssetDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_iD = (string)Utils.Clone(this.m_iD);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.PhysicalAssetUse) != 0) clone.m_physicalAssetUse = (string)Utils.Clone(this.m_physicalAssetUse);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Quantity) != 0) clone.m_quantity = (string)Utils.Clone(this.m_quantity);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.EngineeringUnits) != 0) clone.m_engineeringUnits = (Opc.Ua.EUInformation)Utils.Clone(this.m_engineeringUnits);
            if ((EncodingMask & ISA95PhysicalAssetDataTypeFields.Properties) != 0) clone.m_properties = (ISA95PropertyDataTypeCollection)Utils.Clone(this.m_properties);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_iD;
        private LocalizedTextCollection m_description;
        private string m_physicalAssetUse;
        private string m_quantity;
        private Opc.Ua.EUInformation m_engineeringUnits;
        private ISA95PropertyDataTypeCollection m_properties;
        #endregion
    }

    #region ISA95PhysicalAssetDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95PhysicalAssetDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95PhysicalAssetDataType")]
    public partial class ISA95PhysicalAssetDataTypeCollection : List<ISA95PhysicalAssetDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95PhysicalAssetDataTypeCollection() {}

        /// <remarks />
        public ISA95PhysicalAssetDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95PhysicalAssetDataTypeCollection(IEnumerable<ISA95PhysicalAssetDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95PhysicalAssetDataTypeCollection(ISA95PhysicalAssetDataType[] values)
        {
            if (values != null)
            {
                return new ISA95PhysicalAssetDataTypeCollection(values);
            }

            return new ISA95PhysicalAssetDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95PhysicalAssetDataType[](ISA95PhysicalAssetDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95PhysicalAssetDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95PhysicalAssetDataTypeCollection clone = new ISA95PhysicalAssetDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95PhysicalAssetDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95PropertyDataType Class
    #if (!OPCUA_EXCLUDE_ISA95PropertyDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95PropertyDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        EngineeringUnits = 0x2,
        /// <remarks />
        Subproperties = 0x4
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95PropertyDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95PropertyDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95PropertyDataTypeFields.None;
            m_iD = null;
            m_value = Variant.Null;
            m_description = new LocalizedTextCollection();
            m_engineeringUnits = new Opc.Ua.EUInformation();
            m_subproperties = new ISA95PropertyDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95PropertyDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "ID", IsRequired = false, Order = 1)]
        public string ID
        {
            get { return m_iD;  }
            set { m_iD = value; }
        }

        /// <remarks />
        [DataMember(Name = "Value", IsRequired = false, Order = 2)]
        public Variant Value
        {
            get { return m_value;  }
            set { m_value = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 3)]
        public LocalizedTextCollection Description
        {
            get
            {
                return m_description;
            }

            set
            {
                m_description = value;

                if (value == null)
                {
                    m_description = new LocalizedTextCollection();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "EngineeringUnits", IsRequired = false, Order = 4)]
        public Opc.Ua.EUInformation EngineeringUnits
        {
            get
            {
                return m_engineeringUnits;
            }

            set
            {
                m_engineeringUnits = value;

                if (value == null)
                {
                    m_engineeringUnits = new Opc.Ua.EUInformation();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "Subproperties", IsRequired = false, Order = 5)]
        public ISA95PropertyDataTypeCollection Subproperties
        {
            get
            {
                return m_subproperties;
            }

            set
            {
                m_subproperties = value;

                if (value == null)
                {
                    m_subproperties = new ISA95PropertyDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95PropertyDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95PropertyDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95PropertyDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95PropertyDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("ID", ID);
            encoder.WriteVariant("Value", Value);
            if ((EncodingMask & ISA95PropertyDataTypeFields.Description) != 0) encoder.WriteLocalizedTextArray("Description", Description);
            if ((EncodingMask & ISA95PropertyDataTypeFields.EngineeringUnits) != 0) encoder.WriteEncodeable("EngineeringUnits", EngineeringUnits, typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95PropertyDataTypeFields.Subproperties) != 0) encoder.WriteEncodeableArray("Subproperties", Subproperties.ToArray(), typeof(ISA95PropertyDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95PropertyDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            ID = decoder.ReadString("ID");
            Value = decoder.ReadVariant("Value");
            if ((EncodingMask & ISA95PropertyDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedTextArray("Description");
            if ((EncodingMask & ISA95PropertyDataTypeFields.EngineeringUnits) != 0) EngineeringUnits = (Opc.Ua.EUInformation)decoder.ReadEncodeable("EngineeringUnits", typeof(Opc.Ua.EUInformation));
            if ((EncodingMask & ISA95PropertyDataTypeFields.Subproperties) != 0) Subproperties = (ISA95PropertyDataTypeCollection)decoder.ReadEncodeableArray("Subproperties", typeof(ISA95PropertyDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95PropertyDataType value = encodeable as ISA95PropertyDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_iD, value.m_iD)) return false;
            if (!Utils.IsEqual(m_value, value.m_value)) return false;
            if ((EncodingMask & ISA95PropertyDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95PropertyDataTypeFields.EngineeringUnits) != 0) if (!Utils.IsEqual(m_engineeringUnits, value.m_engineeringUnits)) return false;
            if ((EncodingMask & ISA95PropertyDataTypeFields.Subproperties) != 0) if (!Utils.IsEqual(m_subproperties, value.m_subproperties)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95PropertyDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95PropertyDataType clone = (ISA95PropertyDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_iD = (string)Utils.Clone(this.m_iD);
            clone.m_value = (Variant)Utils.Clone(this.m_value);
            if ((EncodingMask & ISA95PropertyDataTypeFields.Description) != 0) clone.m_description = (LocalizedTextCollection)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95PropertyDataTypeFields.EngineeringUnits) != 0) clone.m_engineeringUnits = (Opc.Ua.EUInformation)Utils.Clone(this.m_engineeringUnits);
            if ((EncodingMask & ISA95PropertyDataTypeFields.Subproperties) != 0) clone.m_subproperties = (ISA95PropertyDataTypeCollection)Utils.Clone(this.m_subproperties);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_iD;
        private Variant m_value;
        private LocalizedTextCollection m_description;
        private Opc.Ua.EUInformation m_engineeringUnits;
        private ISA95PropertyDataTypeCollection m_subproperties;
        #endregion
    }

    #region ISA95PropertyDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95PropertyDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95PropertyDataType")]
    public partial class ISA95PropertyDataTypeCollection : List<ISA95PropertyDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95PropertyDataTypeCollection() {}

        /// <remarks />
        public ISA95PropertyDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95PropertyDataTypeCollection(IEnumerable<ISA95PropertyDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95PropertyDataTypeCollection(ISA95PropertyDataType[] values)
        {
            if (values != null)
            {
                return new ISA95PropertyDataTypeCollection(values);
            }

            return new ISA95PropertyDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95PropertyDataType[](ISA95PropertyDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95PropertyDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95PropertyDataTypeCollection clone = new ISA95PropertyDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95PropertyDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95StateDataType Class
    #if (!OPCUA_EXCLUDE_ISA95StateDataType)
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95StateDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95StateDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            m_browsePath = new Opc.Ua.RelativePath();
            m_stateText = null;
            m_stateNumber = (uint)0;
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "BrowsePath", IsRequired = false, Order = 1)]
        public Opc.Ua.RelativePath BrowsePath
        {
            get
            {
                return m_browsePath;
            }

            set
            {
                m_browsePath = value;

                if (value == null)
                {
                    m_browsePath = new Opc.Ua.RelativePath();
                }
            }
        }

        /// <remarks />
        [DataMember(Name = "StateText", IsRequired = false, Order = 2)]
        public LocalizedText StateText
        {
            get { return m_stateText;  }
            set { m_stateText = value; }
        }

        /// <remarks />
        [DataMember(Name = "StateNumber", IsRequired = false, Order = 3)]
        public uint StateNumber
        {
            get { return m_stateNumber;  }
            set { m_stateNumber = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95StateDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95StateDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95StateDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95StateDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            encoder.WriteEncodeable("BrowsePath", BrowsePath, typeof(Opc.Ua.RelativePath));
            encoder.WriteLocalizedText("StateText", StateText);
            encoder.WriteUInt32("StateNumber", StateNumber);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            BrowsePath = (Opc.Ua.RelativePath)decoder.ReadEncodeable("BrowsePath", typeof(Opc.Ua.RelativePath));
            StateText = decoder.ReadLocalizedText("StateText");
            StateNumber = decoder.ReadUInt32("StateNumber");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95StateDataType value = encodeable as ISA95StateDataType;

            if (value == null)
            {
                return false;
            }

            if (!Utils.IsEqual(m_browsePath, value.m_browsePath)) return false;
            if (!Utils.IsEqual(m_stateText, value.m_stateText)) return false;
            if (!Utils.IsEqual(m_stateNumber, value.m_stateNumber)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95StateDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95StateDataType clone = (ISA95StateDataType)base.MemberwiseClone();

            clone.m_browsePath = (Opc.Ua.RelativePath)Utils.Clone(this.m_browsePath);
            clone.m_stateText = (LocalizedText)Utils.Clone(this.m_stateText);
            clone.m_stateNumber = (uint)Utils.Clone(this.m_stateNumber);

            return clone;
        }
        #endregion

        #region Private Fields
        private Opc.Ua.RelativePath m_browsePath;
        private LocalizedText m_stateText;
        private uint m_stateNumber;
        #endregion
    }

    #region ISA95StateDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95StateDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95StateDataType")]
    public partial class ISA95StateDataTypeCollection : List<ISA95StateDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95StateDataTypeCollection() {}

        /// <remarks />
        public ISA95StateDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95StateDataTypeCollection(IEnumerable<ISA95StateDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95StateDataTypeCollection(ISA95StateDataType[] values)
        {
            if (values != null)
            {
                return new ISA95StateDataTypeCollection(values);
            }

            return new ISA95StateDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95StateDataType[](ISA95StateDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95StateDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95StateDataTypeCollection clone = new ISA95StateDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95StateDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion

    #region ISA95WorkMasterDataType Class
    #if (!OPCUA_EXCLUDE_ISA95WorkMasterDataType)
    /// <remarks />
    /// <exclude />

    public enum ISA95WorkMasterDataTypeFields : uint
    {
        None = 0,
        /// <remarks />
        Description = 0x1,
        /// <remarks />
        Parameters = 0x2
    }

    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2)]
    public partial class ISA95WorkMasterDataType : IEncodeable, IJsonEncodeable
    {
        #region Constructors
        /// <remarks />
        public ISA95WorkMasterDataType()
        {
            Initialize();
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            EncodingMask = ISA95WorkMasterDataTypeFields.None;
            m_iD = null;
            m_description = null;
            m_parameters = new ISA95ParameterDataTypeCollection();
        }
        #endregion

        #region Public Properties
        // <remarks />
        [DataMember(Name = "EncodingMask", IsRequired = true, Order = 0)]
        public ISA95WorkMasterDataTypeFields EncodingMask { get; set; }

        /// <remarks />
        [DataMember(Name = "ID", IsRequired = false, Order = 1)]
        public string ID
        {
            get { return m_iD;  }
            set { m_iD = value; }
        }

        /// <remarks />
        [DataMember(Name = "Description", IsRequired = false, Order = 2)]
        public LocalizedText Description
        {
            get { return m_description;  }
            set { m_description = value; }
        }

        /// <remarks />
        [DataMember(Name = "Parameters", IsRequired = false, Order = 3)]
        public ISA95ParameterDataTypeCollection Parameters
        {
            get
            {
                return m_parameters;
            }

            set
            {
                m_parameters = value;

                if (value == null)
                {
                    m_parameters = new ISA95ParameterDataTypeCollection();
                }
            }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId => DataTypeIds.ISA95WorkMasterDataType;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId => ObjectIds.ISA95WorkMasterDataType_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId => ObjectIds.ISA95WorkMasterDataType_Encoding_DefaultXml;

        /// <summary cref="IJsonEncodeable.JsonEncodingId" />
        public virtual ExpandedNodeId JsonEncodingId => ObjectIds.ISA95WorkMasterDataType_Encoding_DefaultJson;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder)
        {
            encoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);
            encoder.WriteUInt32(nameof(EncodingMask), (uint)EncodingMask);

            encoder.WriteString("ID", ID);
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Description) != 0) encoder.WriteLocalizedText("Description", Description);
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Parameters) != 0) encoder.WriteEncodeableArray("Parameters", Parameters.ToArray(), typeof(ISA95ParameterDataType));

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder)
        {
            decoder.PushNamespace(UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2);

            EncodingMask = (ISA95WorkMasterDataTypeFields)decoder.ReadUInt32(nameof(EncodingMask));

            ID = decoder.ReadString("ID");
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Description) != 0) Description = decoder.ReadLocalizedText("Description");
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Parameters) != 0) Parameters = (ISA95ParameterDataTypeCollection)decoder.ReadEncodeableArray("Parameters", typeof(ISA95ParameterDataType));

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            if (Object.ReferenceEquals(this, encodeable))
            {
                return true;
            }

            ISA95WorkMasterDataType value = encodeable as ISA95WorkMasterDataType;

            if (value == null)
            {
                return false;
            }

            if (value.EncodingMask != this.EncodingMask) return false;

            if (!Utils.IsEqual(m_iD, value.m_iD)) return false;
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Description) != 0) if (!Utils.IsEqual(m_description, value.m_description)) return false;
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Parameters) != 0) if (!Utils.IsEqual(m_parameters, value.m_parameters)) return false;

            return true;
        }

        /// <summary cref="ICloneable.Clone" />
        public virtual object Clone()
        {
            return (ISA95WorkMasterDataType)this.MemberwiseClone();
        }

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95WorkMasterDataType clone = (ISA95WorkMasterDataType)base.MemberwiseClone();

            clone.EncodingMask = this.EncodingMask;

            clone.m_iD = (string)Utils.Clone(this.m_iD);
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Description) != 0) clone.m_description = (LocalizedText)Utils.Clone(this.m_description);
            if ((EncodingMask & ISA95WorkMasterDataTypeFields.Parameters) != 0) clone.m_parameters = (ISA95ParameterDataTypeCollection)Utils.Clone(this.m_parameters);

            return clone;
        }
        #endregion

        #region Private Fields
        private string m_iD;
        private LocalizedText m_description;
        private ISA95ParameterDataTypeCollection m_parameters;
        #endregion
    }

    #region ISA95WorkMasterDataTypeCollection Class
    /// <remarks />
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfISA95WorkMasterDataType", Namespace = UAModel.ISA95_JOBCONTROL_V2.Namespaces.ISA95_JOBCONTROL_V2, ItemName = "ISA95WorkMasterDataType")]
    public partial class ISA95WorkMasterDataTypeCollection : List<ISA95WorkMasterDataType>, ICloneable
    {
        #region Constructors
        /// <remarks />
        public ISA95WorkMasterDataTypeCollection() {}

        /// <remarks />
        public ISA95WorkMasterDataTypeCollection(int capacity) : base(capacity) {}

        /// <remarks />
        public ISA95WorkMasterDataTypeCollection(IEnumerable<ISA95WorkMasterDataType> collection) : base(collection) {}
        #endregion

        #region Static Operators
        /// <remarks />
        public static implicit operator ISA95WorkMasterDataTypeCollection(ISA95WorkMasterDataType[] values)
        {
            if (values != null)
            {
                return new ISA95WorkMasterDataTypeCollection(values);
            }

            return new ISA95WorkMasterDataTypeCollection();
        }

        /// <remarks />
        public static explicit operator ISA95WorkMasterDataType[](ISA95WorkMasterDataTypeCollection values)
        {
            if (values != null)
            {
                return values.ToArray();
            }

            return null;
        }
        #endregion

        #region ICloneable Methods
        /// <remarks />
        public object Clone()
        {
            return (ISA95WorkMasterDataTypeCollection)this.MemberwiseClone();
        }
        #endregion

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone()
        {
            ISA95WorkMasterDataTypeCollection clone = new ISA95WorkMasterDataTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++)
            {
                clone.Add((ISA95WorkMasterDataType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
    #endif
    #endregion
}
