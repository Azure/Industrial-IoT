namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models.Data {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing a data item entry in the configuration.
    /// </summary>
    [DataContract]
    public class OpcDataNodeModel : OpcBaseNodeModel {
        /// <summary> 
        /// Sampling interval 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary>
        /// OpcSamplingInterval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? OpcSamplingIntervalTimespan {
            get => OpcSamplingInterval.HasValue ?
                TimeSpan.FromMilliseconds(OpcSamplingInterval.Value) : (TimeSpan?)null;
            set => OpcSamplingInterval = value != null ?
                (int)value.Value.TotalMilliseconds : (int?)null;
        }

        /// <summary> 
        /// Heartbeat 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? HeartbeatInterval { get; set; }

        /// <summary>
        /// Heartbeat interval as TimeSpan.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan? HeartbeatIntervalTimespan {
            get => HeartbeatInterval.HasValue ?
                TimeSpan.FromSeconds(HeartbeatInterval.Value) : (TimeSpan?)null;
            set => HeartbeatInterval = value != null ?
                (int)value.Value.TotalSeconds : (int?)null;
        }

        /// <summary> 
        /// Skip first value 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }
    }
}
