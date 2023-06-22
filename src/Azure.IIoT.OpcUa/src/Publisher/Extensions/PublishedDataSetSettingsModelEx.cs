// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    /// <summary>
    /// Settings extensions
    /// </summary>
    public static class PublishedDataSetSettingsModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataSetSettingsModel? Clone(this PublishedDataSetSettingsModel? model)
        {
            return model == null ? null : (model with { });
        }
    }
}
