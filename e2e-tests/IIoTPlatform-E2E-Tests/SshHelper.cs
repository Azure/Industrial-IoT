// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace IIoTPlatform_E2E_Tests {
    using System;
    using Xunit;

    public static class SshHelper {

        /// <summary>
        /// Username
        /// </summary>
        public static string Username { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public static string Password { get; set; }

        /// <summary>
        /// Host name
        /// </summary>
        public static string Host { get; set; }

        /// <summary>
        /// Check if ssh credentials and hostname are not null or empty
        /// </summary>
        public static void Validate() {
            Username = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_USER);
            Password = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_PASSWORD);
            Host = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_DNS_NAME);

            Assert.True(!string.IsNullOrWhiteSpace(Username), "username string is null");
            Assert.True(!string.IsNullOrWhiteSpace(Password), "password string is null");
            Assert.True(!string.IsNullOrWhiteSpace(Host), "host string is null");
        }
    }
}
