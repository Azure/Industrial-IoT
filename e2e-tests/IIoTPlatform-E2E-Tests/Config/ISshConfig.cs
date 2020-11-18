// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Config {

    public interface ISshConfig {

        /// <summary>
        /// Username used for ssh authentication
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Password used for ssh authentication
        /// </summary>
        string Password { get; }

        /// <summary>
        /// DNS Host name of machine to ssh into
        /// </summary>
        string Host { get; }
    }
}
