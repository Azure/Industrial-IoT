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
            return model == null ? null : (model with
            {
                DataSet = model.DataSet.Clone(),
                MessageSettings = model.MessageSettings.Clone()
            });
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        private static DataSetWriterMessageSettingsModel? Clone(this DataSetWriterMessageSettingsModel? model)
        {
            return model == null ? null : (model with { });
        }
    }
}
