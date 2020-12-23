// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// Data items extensions
    /// </summary>
    public static class PublishedDataItemsModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataItemsModel Clone(this PublishedDataItemsModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataItemsModel {
                PublishedData = model.PublishedData?.Select(d => d.Clone()).ToList()
            };
        }
    }
}