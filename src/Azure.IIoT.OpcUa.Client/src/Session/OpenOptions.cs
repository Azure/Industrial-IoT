// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// Session options
    /// </summary>
    public class OpenOptions
    {
        /// <summary>
        /// Session name
        /// </summary>
        public IUserIdentity? Identity { get; set; }

        /// <summary>
        /// Preferred locales
        /// </summary>
        public IReadOnlyList<string>? PreferredLocales { get; set; }
    }
}
