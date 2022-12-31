// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// Dataset writer model ex
    /// </summary>
    public static class DataSetWriterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DataSetWriterModel Clone(this DataSetWriterModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetWriterModel {
                DataSet = model.DataSet.Clone(),
                DataSetFieldContentMask = model.DataSetFieldContentMask,
                DataSetMetaDataSendInterval = model.DataSetMetaDataSendInterval,
                DataSetWriterName = model.DataSetWriterName,
                KeyFrameCount = model.KeyFrameCount,
                MessageSettings = model.MessageSettings.Clone()
            };
        }
    }
}