// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using System;
    using RestSharp;
    using RestSharp.Authenticators;
    using Xunit;

    internal static class TestHelper {

        /// <summary>
        /// Read the base URL of Industrial IoT Platform from environment varaibles
        /// </summary>
        /// <returns></returns>
        public static string GetBaseUrl() {
            var env = Environment.GetEnvironmentVariables();
            return env["PCS-SERVICE-URL"] as string;
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <returns>Return content of request token or empty string</returns>
        public static string GetToken() {
            var env = Environment.GetEnvironmentVariables();

            return GetToken(
                env["PCS-AUTH-TENANT"] as string,
                env["PCS-AUTH-CLIENT-APPID"] as string,
                env["PCS-AUTH-CLIENT-SECRET"] as string,
                env["ApplicationName"] as string
            );
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="clientId">User name for HTTP basic authentication</param>
        /// <param name="clientSecret">Password for HTTP basic authentication</param>
        /// <param name="applicationName">Name of deployed Industrial IoT</param>
        /// <returns>Return content of request token or empty string</returns>
        public static string GetToken(string tenantId, string clientId, string clientSecret, string applicationName) {
            
            Assert.NotEmpty(tenantId);
            Assert.NotEmpty(clientId);
            Assert.NotEmpty(clientSecret);
            Assert.NotEmpty(applicationName);

            var client = new RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token") {
                Timeout = 30, 
                Authenticator = new HttpBasicAuthenticator(clientId, clientSecret)
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", $"https://{tenantId}/{applicationName}-service/.default");
            
            var response = client.Execute(request);
            Assert.True(response.IsSuccessful);
            return response?.Content ?? string.Empty;
        }
    }
}
