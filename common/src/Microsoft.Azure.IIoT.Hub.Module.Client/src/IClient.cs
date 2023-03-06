// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client
{
    using System;

    /// <summary>
    /// The sdk client
    /// </summary>
    public interface IClient : IMethodCallClient, IEventClient,
        ITwinClient, IAsyncDisposable, IDisposable
    {
    }
}
