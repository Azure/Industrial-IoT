namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class describing select clauses for an event filter.
    /// </summary>
    [DataContract]
    public class SelectClauseModel {
        /// <summary>
        /// The NodeId of the SimpleAttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string TypeId { get; set; }

        /// <summary>
        /// A list of QualifiedName's describing the field to be published.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<string> BrowsePaths { get; set; }

        /// <summary>
        /// The Attribute of the identified node to be published. This is Value by default.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string AttributeId { get; set; }

        /// <summary>
        /// The index range of the node values to be published.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string IndexRange { get; set; }
    }
}
