namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models.Events {
    using System.Runtime.Serialization;

    /// <summary>
    /// Class to describe the AttributeOperand.
    /// </summary>
    [DataContract]
    public class FilterAttributeModel {
        /// <summary>
        /// The NodeId of the AttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string NodeId { get; set; }

        /// <summary>
        /// The Alias of the AttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Alias { get; set; }

        /// <summary>
        /// A RelativePath describing the browse path from NodeId of the AttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string BrowsePath { get; set; }

        /// <summary>
        /// The AttibuteId of the AttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string AttributeId { get; set; }

        /// <summary>
        /// The IndexRange of the AttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string IndexRange { get; set; }
    }
}
