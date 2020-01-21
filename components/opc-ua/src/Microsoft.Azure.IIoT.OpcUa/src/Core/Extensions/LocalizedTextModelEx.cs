// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Localized text extensions
    /// </summary>
    public static class LocalizedTextModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static LocalizedTextModel Clone(this LocalizedTextModel model) {
            if (model == null) {
                return null;
            }
            return new LocalizedTextModel {
                Locale = model.Locale,
                Text = model.Text
            };
        }
    }
}