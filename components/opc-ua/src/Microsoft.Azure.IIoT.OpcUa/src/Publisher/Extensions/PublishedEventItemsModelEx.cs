// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// Data items extensions
    /// </summary>
    public static class PublishedEventItemsModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedEventItemsModel Clone(this PublishedEventItemsModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedEventItemsModel {
                PublishedData = model.PublishedData?.Select(d => d.Clone()).ToList()
            };
        }
    }
}