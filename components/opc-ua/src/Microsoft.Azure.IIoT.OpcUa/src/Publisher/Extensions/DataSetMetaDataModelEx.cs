// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Dataset metadata extensions
    /// </summary>
    public static class DataSetMetaDataModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataSetMetaDataModel Clone(this DataSetMetaDataModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetMetaDataModel {
                Name = model.Name,
                DataSetClassId = model.DataSetClassId,
                Description = model.Description,
            };
        }


        /// <summary>
        /// Compare items
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this DataSetMetaDataModel model,
            DataSetMetaDataModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            if (model.Name != other.Name) {
                return false;
            }
            if (model.DataSetClassId != other.DataSetClassId) {
                return false;
            }
            if (model.Description != other.Description) {
                return false;
            }
            return true;
        }
    }
}