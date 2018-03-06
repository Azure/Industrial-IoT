// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Client {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Opc.Ua;

    public static class OpcUaTypesEx {

        /// <summary>
        /// Convert security mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static SecurityMode? ToServiceType(this MessageSecurityMode mode) {
            switch(mode) {
                case MessageSecurityMode.None:
                    return SecurityMode.None;
                case MessageSecurityMode.Sign:
                    return SecurityMode.Sign;
                case MessageSecurityMode.SignAndEncrypt:
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
        public static TokenType? ToServiceType(this UserTokenType type) {
            switch (type) {
                case UserTokenType.Anonymous:
                    return TokenType.None;
                case UserTokenType.Certificate:
                    return TokenType.X509Certificate;
                case UserTokenType.UserName:
                case UserTokenType.IssuedToken:
                    return TokenType.UserNamePassword;
                default:
                    return null;
            }
        }
    }
}