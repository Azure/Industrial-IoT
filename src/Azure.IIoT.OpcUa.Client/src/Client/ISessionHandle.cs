﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using System;

/// <summary>
/// A reference to a session with a particular lifetime
/// Basis of the fluent api surface the client exposes
/// </summary>
public interface ISessionHandle : IAsyncDisposable
{
    /// <summary>
    /// The session
    /// </summary>
    ISession Session { get; }
}
