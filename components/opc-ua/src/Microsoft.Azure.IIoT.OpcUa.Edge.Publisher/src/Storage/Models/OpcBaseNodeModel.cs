namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing a base entry in a node list
    /// </summary>
    [DataContract]
    public abstract class OpcBaseNodeModel {
        /// <summary> 
        /// Node Identifier 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary> 
        /// Also 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ExpandedNodeId { get; set; }

        /// <summary> 
        /// Publishing interval 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary>
        /// OpcPublishingInterval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? OpcPublishingIntervalTimespan {
            get => OpcPublishingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcPublishingInterval.Value) : (TimeSpan?)null;
            set => OpcPublishingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> 
        /// Display name 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string DisplayName { get; set; }
    }
}
