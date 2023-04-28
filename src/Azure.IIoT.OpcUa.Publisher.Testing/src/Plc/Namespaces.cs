// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc
{
    /// <summary>
    /// Defines constants for namespaces used by the application.
    /// </summary>
    public static class Namespaces
    {
        /// <summary>
        /// The namespace for the nodes provided by for boiler type.
        /// </summary>
        public const string PlcSimulation = "http://opcfoundation.org/UA/Plc";

        /// <summary>
        /// The namespace for the nodes provided by the for the boiler instance.
        /// </summary>
        public const string PlcInstance = "http://opcfoundation.org/UA/Plc/PlcInstance";

        /// <summary>
        /// The namespace for the nodes provided by the plc server.
        /// </summary>
        public const string PlcApplications = "http://opcfoundation.org/UA/Plc/Applications";
    }
}
