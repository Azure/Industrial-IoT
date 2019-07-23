// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Json token extensions
    /// </summary>
    public static class JTokenEx {

        /// <summary>
        /// Apply a patch to the token
        /// </summary>
        /// <returns></returns>
        public static JToken Apply(this JToken to, JToken from) {
            if (from == null) {
                return JValue.CreateNull();
            }

            //
            // If different types, go for the patch token
            //
            if (to == null || to.Type != from.Type) {
                return from;
            }

            //
            // Object is patched by removing all items that have
            // a null in the patch, and adding items that are
            // different
            //
            if (to is JObject o) {
                foreach (var prop in (JObject)from) {
                    if (o.TryGetValue(prop.Key, out var existing)) {
                        o.Remove(prop.Key);
                    }
                    var p = Apply(existing, prop.Value);
                    if (p.Type != JTokenType.Null) {
                        o.Add(prop.Key, p);
                    }
                }
                return o;
            }

            //
            // Array is patched by removing all items with null at
            // a particular index in the original array and filling
            // up the remainder with data from either array.
            //
            if (to is JArray a) {
                var f = (JArray)from;
                var n = new JArray();
                for (var i = 0; i < Math.Max(a.Count, f.Count); i++) {
                    if (i >= f.Count) {
                        n.Add(a[i]);
                        continue;
                    }
                    var p = (i >= a.Count) ? f[i] :
                        a[i].Apply(f[i]);
                    if (p.Type != JTokenType.Null) {
                        n.Add(p);
                    }
                }
                return n;
            }

            //
            // Replace anything else...
            //
            return from;
        }
    }
}
