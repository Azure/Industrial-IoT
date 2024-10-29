// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Published dataset extensions
    /// </summary>
    public static class PublishedDataSetModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetModel? Clone(this PublishedDataSetModel? model)
        {
            return model == null ? null : (model with
            {
                DataSetMetaData = model.DataSetMetaData.Clone(),
                DataSetSource = model.DataSetSource.Clone(),
            });
        }
    }
}
