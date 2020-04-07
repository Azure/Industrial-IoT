// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Services {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Route the settings to the right controller implementations
    /// </summary>
    public interface ISettingsRouter {

        /// <summary>
        /// Process desired and return reported
        /// </summary>
        /// <param name="desired"></param>
        /// <returns></returns>
        Task<IDictionary<string, VariantValue>> ProcessSettingsAsync(
            IDictionary<string, VariantValue> desired);

        /// <summary>
        /// Get all settings to report.
        /// </summary>
        /// <returns></returns>
        Task<IDictionary<string, VariantValue>> GetSettingsStateAsync();
    }
}
