namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Data {
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

        /// <summary> Heartbeat </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? HeartbeatInterval {
            get => HeartbeatIntervalTimespan.HasValue ? (int)HeartbeatIntervalTimespan.Value.TotalSeconds : default(int?);
            set => HeartbeatIntervalTimespan = value.HasValue ? TimeSpan.FromSeconds(value.Value) : default(TimeSpan?);
        }

        /// <summary>
        /// Heartbeat interval as TimeSpan.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }

        /// <summary> 
        /// Skip first value 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? SkipFirst { get; set; }
    }
}