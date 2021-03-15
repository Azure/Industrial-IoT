namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models.Events {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Describing an event entry in the configuration.
    /// </summary>
    [DataContract]
    public class OpcEventNodeModel : OpcBaseNodeModel {
        /// <summary>
        /// The SelectClauses used to select the fields which should be published for an event.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<SelectClauseModel> SelectClauses { get; set; }

        /// <summary>
        /// The WhereClause to specify which events are of interest.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<WhereClauseElementModel> WhereClauses { get; set; }
    }
}
