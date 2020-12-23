// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Result of method metadata query
    /// </summary>
    public class MethodMetadataResultModel {

        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Input arguments
        /// </summary>
        public List<MethodMetadataArgumentModel> InputArguments { get; set; }

        /// <summary>
        /// Output arguments
        /// </summary>
        public List<MethodMetadataArgumentModel> OutputArguments { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        public ServiceResultModel ErrorInfo { get; set; }
    }
}
