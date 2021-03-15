namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary> 
    /// WhereClauseElementModel 
    /// </summary>
    [DataContract]
    public class WhereClauseElementModel {
        /// <summary>
        /// The Operator of the WhereClauseElement.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Operator { get; set; }

        /// <summary>
        /// The Operands of the WhereClauseElement.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<WhereClauseOperandModel> Operands { get; set; }
    }
}
