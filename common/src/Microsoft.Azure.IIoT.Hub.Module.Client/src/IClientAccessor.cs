// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using System;

    /// <summary>
    /// Interface that grants access to an IClient.
    /// </summary>
    public interface IClientAccessor : IDisposable {

        /// <summary>
        /// The IClient that can be used to access the IoT Hub.
        /// </summary>
        IClient Client { get; }
    }
}
