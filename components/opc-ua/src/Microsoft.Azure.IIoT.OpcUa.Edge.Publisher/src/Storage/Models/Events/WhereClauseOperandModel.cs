namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models.Events {
    using System.Runtime.Serialization;

    /// <summary> 
    /// WhereClauseOperandModel 
    /// </summary>
    [DataContract]
    public class WhereClauseOperandModel {
        /// <summary>
        /// Holds an element value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public uint? Element { get; set; }

        /// <summary>
        /// Holds an Literal value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Literal { get; set; }

        /// <summary>
        /// Holds an AttributeOperand value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public FilterAttributeModel Attribute { get; set; }

        /// <summary>
        /// Holds an SimpleAttributeOperand value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public FilterSimpleAttributeModel SimpleAttribute { get; set; }
    }
}
