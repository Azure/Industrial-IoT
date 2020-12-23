// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Config {

    public interface IIIoTPlatformConfig {
        /// <summary>
        /// Base URL of Industrial IoT Platform
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// Tenant id for HTTP basic authentication
        /// </summary>
        string AuthTenant { get; }

        /// <summary>
        /// User name for HTTP basic authentication for Industrial IoT Platform
        /// </summary>
        string AuthClientId { get; }

        /// <summary>
        /// Password for HTTP basic authentication for Industrial IoT Platform
        /// </summary>
        string AuthClientSecret { get; }

        /// <summary>
        /// Name of deployed Industrial IoT
        /// </summary>
        string ApplicationName { get; }
    }
}
