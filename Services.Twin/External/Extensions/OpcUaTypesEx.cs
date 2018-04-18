// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using UaApplicationType = Opc.Ua.ApplicationType;
    using UaSecurityMode = Opc.Ua.MessageSecurityMode;
    using UaTokenType = Opc.Ua.UserTokenType;

    public static class OpcUaTypesEx {

        /// <summary>
        /// Convert security mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static SecurityMode? ToServiceType(this UaSecurityMode mode) {
            switch(mode) {
                case UaSecurityMode.None:
                    return SecurityMode.None;
                case UaSecurityMode.Sign:
                    return SecurityMode.Sign;
                case UaSecurityMode.SignAndEncrypt:
                    return SecurityMode.SignAndEncrypt;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert token type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TokenType? ToServiceType(this UaTokenType type) {
            switch (type) {
                case UaTokenType.Anonymous:
                    return TokenType.None;
                case UaTokenType.Certificate:
                    return TokenType.X509Certificate;
                case UaTokenType.UserName:
                case UaTokenType.IssuedToken:
                    return TokenType.UserNamePassword;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert application type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Models.ApplicationType? ToServiceType(this UaApplicationType type) {
            switch (type) {
                case UaApplicationType.Client:
                    return Models.ApplicationType.Client;
                case UaApplicationType.DiscoveryServer:
                    return Models.ApplicationType.Server;
                case UaApplicationType.Server:
                    return Models.ApplicationType.Server;
                case UaApplicationType.ClientAndServer:
                    return Models.ApplicationType.ClientAndServer;
                default:
                    return null;
            }
        }
    }
}