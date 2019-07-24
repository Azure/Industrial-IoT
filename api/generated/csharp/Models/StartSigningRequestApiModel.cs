// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.IIoT.Opc.Vault.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Signing request
    /// </summary>
    public partial class StartSigningRequestApiModel
    {
        /// <summary>
        /// Initializes a new instance of the StartSigningRequestApiModel
        /// class.
        /// </summary>
        public StartSigningRequestApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the StartSigningRequestApiModel
        /// class.
        /// </summary>
        /// <param name="entityId">Id of entity to sign a certificate
        /// for</param>
        /// <param name="groupId">Certificate group id</param>
        /// <param name="certificateRequest">Request</param>
        public StartSigningRequestApiModel(string entityId, string groupId, object certificateRequest)
        {
            EntityId = entityId;
            GroupId = groupId;
            CertificateRequest = certificateRequest;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets id of entity to sign a certificate for
        /// </summary>
        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; }

        /// <summary>
        /// Gets or sets certificate group id
        /// </summary>
        [JsonProperty(PropertyName = "groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets request
        /// </summary>
        [JsonProperty(PropertyName = "certificateRequest")]
        public object CertificateRequest { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (EntityId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "EntityId");
            }
            if (GroupId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "GroupId");
            }
            if (CertificateRequest == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "CertificateRequest");
            }
        }
    }
}
