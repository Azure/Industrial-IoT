
namespace Microsoft.Azure.IIoT.Cdm.Models {    
    using System;

    /// <summary>
    /// Model for the Publisher Sample model
    /// </summary>
    public class SubscriberCdmSampleModel {

        /// <summary>
        /// Subscription id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Dataset id
        /// </summary>
        public string DataSetId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Sent time stamp
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        //public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }

        //public ushort? ServerPicoseconds { get; set; }
    }
}
