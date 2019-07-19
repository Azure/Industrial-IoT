// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Method metadata query model
    /// </summary>
    public class MethodMetadataResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodMetadataResponseApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodMetadataResponseApiModel(MethodMetadataResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
            ObjectId = model.ObjectId;
            InputArguments = model.InputArguments?
                .Select(a => a == null ? null : new MethodMetadataArgumentApiModel(a))
                .ToList();
            OutputArguments = model.OutputArguments?
                .Select(a => a == null ? null : new MethodMetadataArgumentApiModel(a))
                .ToList();
        }

        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        [JsonProperty(PropertyName = "objectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "inputArguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodMetadataArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [JsonProperty(PropertyName = "outputArguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodMetadataArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [JsonProperty(PropertyName = "errorInfo",
            NullValueHandling = NullValueHandling.Ignore)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
