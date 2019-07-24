// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.IIoT.Opc.Registry.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Application registration request
    /// </summary>
    public partial class ServerRegistrationRequestApiModel
    {
        /// <summary>
        /// Initializes a new instance of the ServerRegistrationRequestApiModel
        /// class.
        /// </summary>
        public ServerRegistrationRequestApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ServerRegistrationRequestApiModel
        /// class.
        /// </summary>
        /// <param name="discoveryUrl">Discovery url to use for
        /// registration</param>
        /// <param name="id">Registration id</param>
        /// <param name="callback">An optional callback hook to
        /// register.</param>
        /// <param name="activationFilter">Upon discovery, activate all
        /// endpoints with this filter.</param>
        public ServerRegistrationRequestApiModel(string discoveryUrl, string id = default(string), CallbackApiModel callback = default(CallbackApiModel), EndpointActivationFilterApiModel activationFilter = default(EndpointActivationFilterApiModel))
        {
            DiscoveryUrl = discoveryUrl;
            Id = id;
            Callback = callback;
            ActivationFilter = activationFilter;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets discovery url to use for registration
        /// </summary>
        [JsonProperty(PropertyName = "discoveryUrl")]
        public string DiscoveryUrl { get; set; }

        /// <summary>
        /// Gets or sets registration id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets an optional callback hook to register.
        /// </summary>
        [JsonProperty(PropertyName = "callback")]
        public CallbackApiModel Callback { get; set; }

        /// <summary>
        /// Gets or sets upon discovery, activate all endpoints with this
        /// filter.
        /// </summary>
        [JsonProperty(PropertyName = "activationFilter")]
        public EndpointActivationFilterApiModel ActivationFilter { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (DiscoveryUrl == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "DiscoveryUrl");
            }
        }
    }
}
