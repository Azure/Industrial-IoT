// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.Models {
    using System.Collections.Generic;

    public static class EndpointModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointModel model, EndpointModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return
                that.Url == model.Url &&
                (that.TokenType ?? TokenType.None) ==
                    (model.TokenType ?? TokenType.None) &&
                that.User == model.User &&
                that.Validation.SequenceEqualsSafe(model.Validation) &&
                that.SecurityPolicy == model.SecurityPolicy &&
                (that.SecurityMode ?? SecurityMode.Best) ==
                    (model.SecurityMode ?? SecurityMode.Best);
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointModel Clone(this EndpointModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointModel {
                Validation = model.Validation,
                SecurityMode = model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
                Token = model.Token?.DeepClone(),
                TokenType = model.TokenType,
                Url = model.Url,
                User = model.User
            };
        }
    }
}
