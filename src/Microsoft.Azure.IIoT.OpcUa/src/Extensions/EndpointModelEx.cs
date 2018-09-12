// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint model extensions
    /// </summary>
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
                that.Authentication.IsSameAs(that.Authentication) &&
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
                Authentication = model.Authentication.Clone(),
                Url = model.Url
            };
        }
    }
}
