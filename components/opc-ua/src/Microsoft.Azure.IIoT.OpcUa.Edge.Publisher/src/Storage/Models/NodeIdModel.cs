namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Node id serialized as object
    /// </summary>
    [DataContract]
    public class NodeIdModel {
        /// <summary> Identifier </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Identifier { get; set; }
    }
}
