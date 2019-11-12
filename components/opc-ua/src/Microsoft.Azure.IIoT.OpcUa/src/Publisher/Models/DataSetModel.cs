// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Dataset model
    /// </summary>
    public class DataSetModel {

        /// <summary>
        /// Fields of the dataset
        /// </summary>
        public List<DataSetFieldModel> Fields { get; set; }

        /// <summary>
        /// Name of dataset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type id
        /// </summary>
        public string TypeId { get; set; }

        /// <summary>
        /// Dataset major version
        /// </summary>
        public uint? DataSetMajorVersion { get; set; }

        /// <summary>
        /// Dataset minor version
        /// </summary>
        public uint? DataSetMinorVersion { get; set; }
    }
}