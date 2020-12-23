// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;

    /// <summary>
    /// Field metadata extensions
    /// </summary>
    public static class FieldMetaDataModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static FieldMetaDataModel Clone(this FieldMetaDataModel model) {
            if (model == null) {
                return null;
            }
            return new FieldMetaDataModel {
                BuiltInType = model.BuiltInType,
                Description = model.Description.Clone(),
                ArrayDimensions = model.ArrayDimensions?.ToList(),
                DataSetFieldId = model.DataSetFieldId,
                DataTypeId = model.DataTypeId,
                FieldFlags = model.FieldFlags,
                MaxStringLength = model.MaxStringLength,
                Name = model.Name,
                Properties = model.Properties?
                    .ToDictionary(k => k.Key, v => v.Value),
                ValueRank = model.ValueRank
            };
        }
    }
}