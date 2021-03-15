namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models.Events {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class to describe the SimpleAttributeOperand.
    /// </summary>
    [DataContract]
    public class FilterSimpleAttributeModel {
        /// <summary>
        /// The TypeId of the SimpleAttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string TypeId { get; set; }

        /// <summary>
        /// The browse path as a list of QualifiedName's of the SimpleAttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<string> BrowsePaths { get; set; }

        /// <summary>
        /// The AttributeId of the SimpleAttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string AttributeId { get; set; }

        /// <summary>
        /// The IndexRange of the SimpleAttributeOperand.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string IndexRange { get; set; }
    }
}
