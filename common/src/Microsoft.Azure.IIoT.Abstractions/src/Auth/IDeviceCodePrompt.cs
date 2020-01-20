//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System;

    /// <summary>
    /// Device code prompt
    /// </summary>
    public interface IDeviceCodePrompt {

        /// <summary>
        /// Prompt device code and message
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="expiresOn"></param>
        /// <param name="message"></param>
        void Prompt(string deviceCode,
            DateTimeOffset expiresOn, string message);
    }
}
