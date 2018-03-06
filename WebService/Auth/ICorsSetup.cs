// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Auth {

    /// <summary>
    /// Cors setup
    /// </summary>
    public interface ICorsSetup
    {
        /// <summary>
        /// Configure cors on app
        /// </summary>
        /// <param name="app"></param>
        void UseMiddleware(IApplicationBuilder app);
    }
}
