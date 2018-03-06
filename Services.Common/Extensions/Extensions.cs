// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common {
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public static class Extensions {

        /// <summary>
        /// Convert to base 16
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase16String(this byte[] value) {
            if (value == null) {
                return null;
            }
            const string hexAlphabet = @"0123456789abcdef";
            var chars = new char[checked(value.Length * 2)];
            unchecked {
                for (var i = 0; i < value.Length; i++) {
                    chars[i * 2] = hexAlphabet[value[i] >> 4];
                    chars[i * 2 + 1] = hexAlphabet[value[i] & 0xF];
                }
            }
            return new string(chars);
        }


        /// <summary>
        /// Convert to base 64
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToBase64String(this byte[] value) {
            if (value == null) {
                return null;
            }
            return Convert.ToBase64String(value);
        }

        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="bytestr">string to hash</param>
        /// <returns></returns>
        public static string ToSha1Hash(this byte[] bytestr) {
            using (var sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(bytestr);
                return hash.ToBase16String();
            }
        }

        /// <summary>
        /// Hashes the string
        /// </summary>
        /// <param name="str">string to hash</param>
        /// <returns></returns>
        public static string ToSha1Hash(this string str) =>
            Encoding.UTF8.GetBytes(str).ToSha1Hash();

        /// <summary>
        /// hashes a json object
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ToSha1Hash(this JToken token) =>
            token.ToStringMinified().ToSha1Hash();

        /// <summary>
        /// Append byte array to string builder
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        public static string ToString(this byte[] bytes, int size) {
            var truncate = bytes.Length > size;
            var length = truncate ? size : bytes.Length;
            var ascii = IsAscii(bytes, length);
            var content = ascii ? Encoding.ASCII.GetString(bytes, 0, length) :
                BitConverter.ToString(bytes, 0, length);
            length = content.IndexOf('\n');
            if (length > 0) {
                return content.Substring(0, length);
            }
            return content;
        }

        /// <summary>
        /// Check whether bytes are ascii
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IsAscii(this byte[] bytes, int length) =>
            bytes.Take(length).All(x => x > 32 || x <= 127);

        /// <summary>
        /// Combine messages
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        public static string GetCombinedExceptionMessage(this AggregateException ae) {
            var sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions) {
                sb.AppendLine(string.Concat("E: ", e.Message));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Combine stack trace
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        public static string GetCombinedExceptionStackTrace(this AggregateException ae) {
            var sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions) {
                sb.AppendLine(string.Concat("StackTrace: ", e.StackTrace));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns first exception of specified type in exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static T GetFirstOf<T>(this Exception ex) where T : Exception {
            if (ex is T) {
                return (T)ex;
            }
            if (ex is AggregateException) {
                var ae = ((AggregateException)ex).Flatten();
                foreach (var e in ae.InnerExceptions) {
                    var found = GetFirstOf<T>(e);
                    if (found != null) {
                        return found;
                    }
                }
            }
            return null;
        }
    }
}
