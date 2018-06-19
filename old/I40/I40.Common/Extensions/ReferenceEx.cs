// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using System;
    using System.Linq;

    public static class ReferenceEx {

        /// <summary>
        /// Parse as reference, which is an identifier seperated
        /// by /#/ and followed by the sub (browse) path.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Reference AsReference(this string id) {
            if (id == null) {
                return null;
            }
            // Break root from browse path
            var items = id.Split(new string[] { "/#/" }, StringSplitOptions.None);
            var reference = new Reference {
                Target = items[0].AsIdentifier()
            };
            if (items.Length == 2) {
                reference.SubPath = items[1].Split('/').ToList();
            }
            else if (items.Length != 1) {
                throw new FormatException($"Bad reference {id}");
            }
            return reference;
        }

        /// <summary>
        /// Convert a reference to string based on the convention
        /// described in <see cref="AsReference"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AsString(this Reference value) {
            var id = value.Target.AsString();
            if (value.SubPath?.Any() ?? false) {
                id += "/#/" + value.SubPath.Aggregate((x, y) => x + "/" + y);
            }
            return id;
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this Reference value, Reference that) =>
            value?.AsString() == that?.AsString();

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this Reference value, IReferable that) =>
            value?.AsString() == that?.Self.AsString();

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this IReferable value, Reference that) =>
            value?.Self.AsString() == that?.AsString();

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="value"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsEqual(this IReferable value, IReferable that) =>
            that == value || (value?.Self.IsEqual(that?.Self) ?? false);
    }
}