namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum that defines the authentication method
    /// </summary>
    [DataContract]
    public enum OpcAuthenticationMode {
        /// <summary> 
        /// Anonymous authentication 
        /// </summary>
        [EnumMember]
        Anonymous,

        /// <summary> 
        /// Username/Password authentication 
        /// </summary>
        [EnumMember]
        UsernamePassword
    }
}
