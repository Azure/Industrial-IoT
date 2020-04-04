// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Microsoft.Azure.IIoT.Utils;
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
            _exceptionSerializer = JsonSerializer.Create(
                new JsonSerializerSettings {
                    ContractResolver = new DefaultContractResolver(),
                    Converters = new List<JsonConverter>(),
                    TypeNameHandling = TypeNameHandling.None,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    MaxDepth = 20
                });
        }

        /// <summary>
        /// Handles all exceptions
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType) {
            return typeof(Exception).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Cannot read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.StartObject) {
                return null;
            }
            var o = JToken.ReadFrom(reader);
            return o.ToObject(objectType, _exceptionSerializer);
        }

        /// <summary>
        /// Can write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer) {
            if (!(value is Exception ex)) {
                writer.WriteNull();
            }
            else {
                ToExtendedObject(ex, 3).WriteTo(writer);
            }
        }

        /// <summary>
        /// Convert exception to object
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private JObject ToExtendedObject(Exception ex, int depth) {
            var error = JObject.FromObject(ex, _exceptionSerializer);
            var message = ex.Message.Trim();
            Try.Op(() => {
                //
                // The message might be json e.g. returned from another
                // service - try to parse it as json...
                //
                if (message.StartsWith("{", StringComparison.Ordinal)) {
                    error.AddOrUpdate(nameof(ex.Message), JToken.Parse(message));
                }
            });
            if (!error.ContainsKey(nameof(ex.Message))) {
                error.Add(nameof(ex.Message), message);
            }
            error.AddOrUpdate(nameof(Exception), ex.GetType().Name);
            //error.Remove(nameof(ex.InnerException));
            //if (ex is AggregateException ae) {
            //    error.Remove(nameof(ae.InnerExceptions));
            //}
            if (depth > 0) {
                if (ex is AggregateException exception) {
                    var inner = exception.InnerExceptions
                        .Select(ie => ToExtendedObject(ie, depth - 1)).ToList();
                    if (inner.Count > 0) {
                        error.AddOrUpdate("CausedBy", JToken.FromObject(inner));
                    }
                }
                else if (ex.InnerException != null) {
                    error.Add("CausedBy", JToken.FromObject(
                        ToExtendedObject(ex.InnerException, depth - 1)));
                }
            }
            if (_includeStackTrace) {
                if (ex.StackTrace != null) {
                    error.AddOrUpdate(nameof(ex.StackTrace), JToken.FromObject(
                        ex.StackTrace.Split(new[] { "\r", "\n" },
                        StringSplitOptions.RemoveEmptyEntries)));
                }
            }
            else {
                error.Remove(nameof(ex.StackTrace));
            }
            return error;
        }

        private readonly bool _includeStackTrace;
        private readonly JsonSerializer _exceptionSerializer;
    }
}
