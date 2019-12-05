// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Configuration {

    /// <summary>
    /// Configuration service configuration
    /// </summary>
    public interface IConfigurationConfig {

        /// <summary>
        /// Configuration service url
        /// </summary>
        string ConfigurationServiceResourceId { get; }

        /// <summary>
        /// Resource id of configuration service
        /// </summary>
        string ConfigurationServiceUrl { get; }
    }
}