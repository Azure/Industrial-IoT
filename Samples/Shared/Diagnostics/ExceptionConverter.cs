// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.Diagnostics {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Writes exception as json
    /// </summary>
    public class ExceptionConverter : JsonConverter {

        /// <summary>
        /// Create converter
        /// </summary>
        /// <param name="includeStackTrace"></param>
        public ExceptionConverter(bool includeStackTrace = false) {
            _includeStackTrace = includeStackTrace;
        }

        /// <summary>
        /// Handles all exceptions
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) =>
            typeof(Exception).IsAssignableFrom(objectType);

        public override bool CanRead => false;

        /// <summary>
        /// Cannot read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanWrite => true;

        /// <summary>
        /// Can write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer) {
            var o = JToken.FromObject(ToObject(value as Exception));
            o.WriteTo(writer);
        }

        /// <summary>
        /// Convert exception to object
        /// </summary>
        /// <param name="e"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private Dictionary<string, JToken> ToObject(Exception e, int depth = 3) {
            if (e == null) {
                return new Dictionary<string, JToken>();
            }
            var message = e.Message.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            JToken token;
            if (message.Length > 1) {
                token = JToken.FromObject(message);
            }
            else {
                try {
                    // The message might be json e.g. returned from another service - try to parse it...
                    token = JToken.Parse(e.Message);
                }
                catch {
                    token = JToken.FromObject(e.Message);
                }
            }
            var error = new Dictionary<string, JToken> {
                { "Exception",  e.GetType().Name },
                { "Message", token }
            };
            if (_includeStackTrace) {
                error.Add("FullType", e.GetType().FullName);
            }
            if (depth > 0) {
                if (e is AggregateException exception) {
                    var inner = exception.InnerExceptions
                        .Select(ie => ToObject(ie, depth - 1)).ToList();
                    if (inner.Count > 0) {
                        error.Add("CausedBy", JToken.FromObject(inner));
                    }
                }
                else if (e.InnerException != null) {
                    error.Add("CausedBy", JToken.FromObject(
                        ToObject(e.InnerException, depth - 1)));
                }
            }
            if (_includeStackTrace) {
                error["StackTrace"] = JToken.FromObject(
                      e.StackTrace.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));
            }
            return error;
        }

        private readonly bool _includeStackTrace;
    }
}
