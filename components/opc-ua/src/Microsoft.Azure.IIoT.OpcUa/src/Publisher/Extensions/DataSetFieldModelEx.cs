// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// DataSet Field Model extensions
    /// </summary>
    public static class DataSetFieldModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataSetFieldModel Clone(this DataSetFieldModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetFieldModel {
                NodeId = model.NodeId,
                Configuration = model.Configuration.Clone()
            };
        }
    }
}