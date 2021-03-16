namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an event entry in the configuration.
    /// </summary>
    [DataContract]
    public class OpcEventNodeModel : OpcBaseNodeModel {
        /// <summary> 
        /// EventFilter containing the select and where clauses
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public EventFilterModel EventFilter { get; set; }
    }
}
