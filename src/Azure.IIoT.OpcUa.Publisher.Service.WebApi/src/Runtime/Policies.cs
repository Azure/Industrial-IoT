// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Auth
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using System;

    /// <summary>
    /// Defines publisher service api policies.
    /// </summary>
    public static class Policies
    {
        /// <summary>
        /// Allowed to read
        /// </summary>
        public const string CanRead =
            nameof(CanRead);

        /// <summary>
        /// Allowed to update or delete
        /// </summary>
        public const string CanWrite =
            nameof(CanWrite);

        /// <summary>
        /// Allowed to request publish
        /// </summary>
        public const string CanPublish =
            nameof(CanPublish);
    }
}
