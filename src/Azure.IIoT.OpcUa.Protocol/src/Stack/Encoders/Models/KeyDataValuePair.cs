// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Encodable Key DataValue Pair
    /// </summary>
    public class KeyDataValuePair : IEncodeable {

        /// <summary>
        /// The default constructor.
        /// </summary>
        public KeyDataValuePair() {}

        /// <summary>
        /// The default constructor.
        /// </summary>
        public KeyDataValuePair(string key, DataValue value) {
            Key = key;
            Value = value;
        }

        /// <remarks />
        [DataMember(Name = "Key", IsRequired = true, Order = 1)]
        public string Key { get; set; }

        /// <remarks />
        [DataMember(Name = "Value", IsRequired = true, Order = 2)]
        public DataValue Value { get; set;}

        /// <summary cref="IEncodeable.TypeId" />
        public virtual ExpandedNodeId TypeId { get; } = null;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public virtual ExpandedNodeId BinaryEncodingId { get; } = null;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public virtual ExpandedNodeId XmlEncodingId { get; } = null;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder) {
            encoder.WriteString("Key", Key);
            encoder.WriteDataValue(Key, Value);
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder) {
            Key = decoder.ReadString("Key");
            Value = decoder.ReadDataValue(Key);
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public virtual bool IsEqual(IEncodeable encodeable) {
            if (Object.ReferenceEquals(this, encodeable)) {
                return true;
            }
            var value = encodeable as KeyDataValuePair;
            if (value == null) {
                return false;
            }
            if (!Utils.IsEqual(Key, value.Key)) return false;
            if (!Utils.IsEqual(Value, value.Value)) return false;
            return true;
        }

        /// <summary cref="object.MemberwiseClone" />
        public new object MemberwiseClone() {
            KeyDataValuePair clone = (KeyDataValuePair)base.MemberwiseClone();
            clone.Key = (string)Utils.Clone(this.Key);
            clone.Value = (DataValue)Utils.Clone(this.Value);
            return clone;
        }
    }

    /// <summary>
    /// A collection of KeyDataValuePair objects.
    /// </summary>
    public partial class KeyDataValuePairCollection : List<KeyDataValuePair>{

        /// <summary>
        /// Initializes the collection with default values.
        /// </summary>
        public KeyDataValuePairCollection() { }

        /// <summary>
        /// Initializes the collection with an initial capacity.
        /// </summary>
        public KeyDataValuePairCollection(int capacity) : base(capacity) { }

        /// <summary>
        /// Initializes the collection with another collection.
        /// </summary>
        public KeyDataValuePairCollection(IEnumerable<KeyDataValuePair> collection) : base(collection) { }
        /// <summary>
        /// Converts an array to a collection.
        /// </summary>
        public static implicit operator KeyDataValuePairCollection(KeyDataValuePair[] values) {
            if (values != null) {
                return new KeyDataValuePairCollection(values);
            }
            return new KeyDataValuePairCollection();
        }

        /// <summary>
        /// Converts a collection to an array.
        /// </summary>
        public static explicit operator KeyDataValuePair[](KeyDataValuePairCollection values) {
            if (values != null) {
                return values.ToArray();
            }
            return null;
        }

        /// <summary cref="object.MemberwiseClone" />
        public new object MemberwiseClone() {
            KeyDataValuePairCollection clone = new KeyDataValuePairCollection(this.Count);
            for (int ii = 0; ii < this.Count; ii++) {
                clone.Add((KeyDataValuePair)Utils.Clone(this[ii]));
            }
            return clone;
        }
    }
}
