// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// DataSet Model extensions
    /// </summary>
    public static class DataSetModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataSetModel Clone(this DataSetModel model) {
            if (model?.Fields == null) {
                return null;
            }
            return new DataSetModel {
                DataSetMajorVersion = model.DataSetMajorVersion,
                DataSetMinorVersion = model.DataSetMinorVersion,
                Fields = model.Fields.Select(f => f.Clone()).ToList(),
                Name = model.Name,
                TypeId = model.TypeId
            };
        }
    }
}