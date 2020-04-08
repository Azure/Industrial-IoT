// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Microsoft.Azure.IIoT.Services.Common.Auth {

    /// <summary>
    /// Device auth
    /// </summary>
    public class DeviceAuthorizationViewModel : ConsentViewModel {

        /// <summary>
        /// Code
        /// </summary>
        public string UserCode { get; set; }

        /// <summary>
        /// Confirmation
        /// </summary>
        public bool ConfirmUserCode { get; set; }
    }
}