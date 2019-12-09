// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;

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
                ConfigurationVersion = model.ConfigurationVersion.Clone(),
                Name = model.Name,
                DataSetClassId = model.DataSetClassId,
                Description = model.Description.Clone(),
                EnumDataTypes = model.EnumDataTypes?.Select(d => d.Clone()).ToList(),
                StructureDataTypes = model.StructureDataTypes?.Select(d => d.Clone()).ToList(),
                SimpleDataTypes = model.SimpleDataTypes?.Select(d => d.Clone()).ToList(),
                Namespaces = model.Namespaces?.ToList(),
                Fields = model.Fields?.Select(d => d.Clone()).ToList()
            };
        }
    }
}