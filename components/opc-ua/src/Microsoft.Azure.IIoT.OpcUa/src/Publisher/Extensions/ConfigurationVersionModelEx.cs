// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// DataSet Model extensions
    /// </summary>
    public static class ConfigurationVersionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ConfigurationVersionModel Clone(this ConfigurationVersionModel model) {
            if (model == null) {
                return null;
            }
            return new ConfigurationVersionModel {
                MajorVersion = model.MajorVersion,
                MinorVersion = model.MinorVersion
            };
        }
    }
}