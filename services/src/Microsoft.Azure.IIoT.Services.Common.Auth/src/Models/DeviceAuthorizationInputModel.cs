// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Microsoft.Azure.IIoT.Services.Common.Auth {

    /// <summary>
    /// Input
    /// </summary>
    public class DeviceAuthorizationInputModel : ConsentInputModel {

        /// <summary>
        /// Provided user code
        /// </summary>
        public string UserCode { get; set; }
    }
}