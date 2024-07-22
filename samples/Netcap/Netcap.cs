// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

/// <summary>
/// Netcap exception
/// </summary>
public class NetcapException : Exception
{
    public NetcapException(string message) : base(message)
    {
    }

    public NetcapException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
