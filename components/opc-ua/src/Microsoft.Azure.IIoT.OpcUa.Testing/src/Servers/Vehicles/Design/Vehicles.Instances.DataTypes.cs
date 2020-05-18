/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
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
using System.Runtime.Serialization;
using Opc.Ua;
using Vehicles.Types;

namespace Vehicles.Instances {
    #region ParkingLotType Enumeration
#if (!OPCUA_EXCLUDE_ParkingLotType)
    /// <summary>
    /// 
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Vehicles.Instances.Namespaces.VehiclesInstances)]
    public enum ParkingLotType {
        /// <remarks />
        [EnumMember(Value = "Open_1")]
        Open = 1,

        /// <remarks />
        [EnumMember(Value = "Covered_2")]
        Covered = 2,
    }

    #region ParkingLotTypeCollection Class
    /// <summary>
    /// A collection of ParkingLotType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfParkingLotType", Namespace = Vehicles.Instances.Namespaces.VehiclesInstances, ItemName = "ParkingLotType")]
#if !NET_STANDARD
    public partial class ParkingLotTypeCollection : List<ParkingLotType>, ICloneable
#else
    public partial class ParkingLotTypeCollection : List<ParkingLotType>
#endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public ParkingLotTypeCollection() { }

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public ParkingLotTypeCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public ParkingLotTypeCollection(IEnumerable<ParkingLotType> collection) : base(collection) { }
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator ParkingLotTypeCollection(ParkingLotType[] values) {
            if (values != null) {
                return new ParkingLotTypeCollection(values);
            }

            return new ParkingLotTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator ParkingLotType[](ParkingLotTypeCollection values) {
            if (values != null) {
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
        public object Clone() {
            return (ParkingLotTypeCollection)this.MemberwiseClone();
        }
        #endregion
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone() {
            ParkingLotTypeCollection clone = new ParkingLotTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++) {
                clone.Add((ParkingLotType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
#endif
    #endregion

    #region BicycleType Class
#if (!OPCUA_EXCLUDE_BicycleType)
    /// <summary>
    /// 
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [DataContract(Namespace = Vehicles.Instances.Namespaces.VehiclesInstances)]
    public partial class BicycleType : VehicleType {
        #region Constructors
        /// <summary>
        /// The default constructor.
        /// </summary>
        public BicycleType() {
            Initialize();
        }

        /// <summary>
        /// Called by the .NET framework during deserialization.
        /// </summary>
        [OnDeserializing]
        private void Initialize(StreamingContext context) {
            Initialize();
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize() {
            m_noOfGears = (uint)0;
            m_manufacterName = null;
        }
        #endregion

        #region Public Properties
        /// <remarks />
        [DataMember(Name = "NoOfGears", IsRequired = false, Order = 1)]
        public uint NoOfGears {
            get { return m_noOfGears; }
            set { m_noOfGears = value; }
        }

        /// <remarks />
        [DataMember(Name = "ManufacterName", IsRequired = false, Order = 2)]
        public QualifiedName ManufacterName {
            get { return m_manufacterName; }
            set { m_manufacterName = value; }
        }
        #endregion

        #region IEncodeable Members
        /// <summary cref="IEncodeable.TypeId" />
        public override ExpandedNodeId TypeId {
            get { return DataTypeIds.BicycleType; }
        }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public override ExpandedNodeId BinaryEncodingId {
            get { return ObjectIds.BicycleType_Encoding_DefaultBinary; }
        }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public override ExpandedNodeId XmlEncodingId {
            get { return ObjectIds.BicycleType_Encoding_DefaultXml; }
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public override void Encode(IEncoder encoder) {
            base.Encode(encoder);

            encoder.PushNamespace(Vehicles.Instances.Namespaces.VehiclesInstances);

            encoder.WriteUInt32("NoOfGears", NoOfGears);
            encoder.WriteQualifiedName("ManufacterName", ManufacterName);

            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public override void Decode(IDecoder decoder) {
            base.Decode(decoder);

            decoder.PushNamespace(Vehicles.Instances.Namespaces.VehiclesInstances);

            NoOfGears = decoder.ReadUInt32("NoOfGears");
            ManufacterName = decoder.ReadQualifiedName("ManufacterName");

            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public override bool IsEqual(IEncodeable encodeable) {
            if (Object.ReferenceEquals(this, encodeable)) {
                return true;
            }

            BicycleType value = encodeable as BicycleType;

            if (value == null) {
                return false;
            }

            if (!base.IsEqual(encodeable)) return false;
            if (!Utils.IsEqual(m_noOfGears, value.m_noOfGears)) return false;
            if (!Utils.IsEqual(m_manufacterName, value.m_manufacterName)) return false;

            return true;
        }

#if !NET_STANDARD
        /// <summary cref="ICloneable.Clone" />
        public override object Clone() {
            return (BicycleType)this.MemberwiseClone();
        }
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone() {
            BicycleType clone = (BicycleType)base.MemberwiseClone();

            clone.m_noOfGears = (uint)Utils.Clone(this.m_noOfGears);
            clone.m_manufacterName = (QualifiedName)Utils.Clone(this.m_manufacterName);

            return clone;
        }
        #endregion

        #region Private Fields
        private uint m_noOfGears;
        private QualifiedName m_manufacterName;
        #endregion
    }

    #region BicycleTypeCollection Class
    /// <summary>
    /// A collection of BicycleType objects.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    [CollectionDataContract(Name = "ListOfBicycleType", Namespace = Vehicles.Instances.Namespaces.VehiclesInstances, ItemName = "BicycleType")]
#if !NET_STANDARD
    public partial class BicycleTypeCollection : List<BicycleType>, ICloneable
#else
    public partial class BicycleTypeCollection : List<BicycleType>
#endif
    {
        #region Constructors
        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public BicycleTypeCollection() { }

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public BicycleTypeCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public BicycleTypeCollection(IEnumerable<BicycleType> collection) : base(collection) { }
        #endregion

        #region Static Operators
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator BicycleTypeCollection(BicycleType[] values) {
            if (values != null) {
                return new BicycleTypeCollection(values);
            }

            return new BicycleTypeCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator BicycleType[](BicycleTypeCollection values) {
            if (values != null) {
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
        public object Clone() {
            return (BicycleTypeCollection)this.MemberwiseClone();
        }
        #endregion
#endif

        /// <summary cref="Object.MemberwiseClone" />
        public new object MemberwiseClone() {
            BicycleTypeCollection clone = new BicycleTypeCollection(this.Count);

            for (int ii = 0; ii < this.Count; ii++) {
                clone.Add((BicycleType)Utils.Clone(this[ii]));
            }

            return clone;
        }
    }
    #endregion
#endif
    #endregion
}