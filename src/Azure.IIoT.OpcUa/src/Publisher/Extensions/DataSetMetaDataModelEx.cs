// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Dataset metadata extensions
    /// </summary>
    public static class DataSetMetaDataModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static DataSetMetaDataModel? Clone(this DataSetMetaDataModel? model)
        {
            if (model == null)
            {
                return null;
            }
            return new DataSetMetaDataModel
            {
                Name = model.Name,
                DataSetClassId = model.DataSetClassId,
                Description = model.Description
            };
        }
    }
}
