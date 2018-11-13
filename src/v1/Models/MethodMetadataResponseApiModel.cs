// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Method metadata query model for twin module
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
            ErrorInfo = model.ErrorInfo == null ? null :
                new ServiceResultApiModel(model.ErrorInfo);
            ObjectId = model.ObjectId;
            if (model.InputArguments == null) {
                InputArguments = new List<MethodMetadataArgumentApiModel>();
            }
            else {
                InputArguments = model.InputArguments
                    .Select(a => new MethodMetadataArgumentApiModel(a))
                    .ToList();
            }
            if (model.OutputArguments == null) {
                OutputArguments = new List<MethodMetadataArgumentApiModel>();
            }
            else {
                OutputArguments = model.OutputArguments
                    .Select(a => new MethodMetadataArgumentApiModel(a))
                    .ToList();
            }
        }

        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        public List<MethodMetadataArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        public List<MethodMetadataArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
