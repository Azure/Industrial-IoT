// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Dataset writer model ex
    /// </summary>
    public static class DataSetWriterModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static DataSetWriterModel? Clone(this DataSetWriterModel? model)
        {
            if (model == null)
            {
                return null;
            }
            return new DataSetWriterModel
            {
                DataSet = model.DataSet.Clone(),
                DataSetFieldContentMask = model.DataSetFieldContentMask,
                MetaDataUpdateTime = model.MetaDataUpdateTime,
                DataSetWriterName = model.DataSetWriterName,
                KeyFrameCount = model.KeyFrameCount,
                MessageSettings = model.MessageSettings.Clone()
            };
        }
    }
}
