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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Application with list of endpoints
    /// </summary>
    public partial class ApplicationRegistrationApiModel
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationRegistrationApiModel
        /// class.
        /// </summary>
        public ApplicationRegistrationApiModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ApplicationRegistrationApiModel
        /// class.
        /// </summary>
        /// <param name="application">Application information</param>
        /// <param name="endpoints">List of endpoint twins</param>
        /// <param name="securityAssessment">Application security assessment.
        /// Possible values include: 'Unknown', 'Low', 'Medium', 'High'</param>
        public ApplicationRegistrationApiModel(ApplicationInfoApiModel application, IList<EndpointRegistrationApiModel> endpoints = default(IList<EndpointRegistrationApiModel>), SecurityAssessment? securityAssessment = default(SecurityAssessment?))
        {
            Application = application;
            Endpoints = endpoints;
            SecurityAssessment = securityAssessment;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets application information
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        public ApplicationInfoApiModel Application { get; set; }

        /// <summary>
        /// Gets or sets list of endpoint twins
        /// </summary>
        [JsonProperty(PropertyName = "endpoints")]
        public IList<EndpointRegistrationApiModel> Endpoints { get; set; }

        /// <summary>
        /// Gets or sets application security assessment. Possible values
        /// include: 'Unknown', 'Low', 'Medium', 'High'
        /// </summary>
        [JsonProperty(PropertyName = "securityAssessment")]
        public SecurityAssessment? SecurityAssessment { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Application == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Application");
            }
            if (Application != null)
            {
                Application.Validate();
            }
            if (Endpoints != null)
            {
                foreach (var element in Endpoints)
                {
                    if (element != null)
                    {
                        element.Validate();
                    }
                }
            }
        }
    }
}
