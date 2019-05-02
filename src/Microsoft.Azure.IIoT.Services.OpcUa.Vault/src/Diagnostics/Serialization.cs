// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Diagnostics
{
    public class Serialization
    {
        // Save memory avoiding serializations that go too deep
        private static readonly JsonSerializerSettings _serializationSettings =
            new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                MaxDepth = 4,
                Converters = new List<JsonConverter>
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                        AllowIntegerValues = true
                    }
                }
            };

        public static string Serialize(object o)
        {
            var logdata = new Dictionary<string, object>();

            // To avoid flooding the logs and logging exceptions, filter
            // exceptions' data and log only what's useful
            foreach (PropertyInfo data in o.GetType().GetRuntimeProperties())
            {
                var name = data.Name;
                var value = data.GetValue(o, index: null);

                if (value is Exception)
                {
                    var e = value as Exception;
                    logdata.Add(name, SerializeException(e));
                }
                else
                {
                    logdata.Add(name, value);
                }
            }

            return JsonConvert.SerializeObject(logdata, _serializationSettings);
        }

        private static object SerializeException(Exception e, int depth = 3)
        {
            if (e == null)
            {
                return null;
            }

            if (depth == 0)
            {
                return "-max serialization depth reached-";
            }

            if (e is AggregateException exception)
            {
                var innerExceptions = exception.InnerExceptions
                    .Select(ie => SerializeException(ie, depth - 1)).ToList();

                return new
                {
                    ExceptionFullName = exception.GetType().FullName,
                    ExceptionMessage = exception.Message,
                    exception.StackTrace,
                    exception.Source,
                    exception.Data,
                    InnerExceptions = innerExceptions
                };
            }

            return new
            {
                ExceptionFullName = e.GetType().FullName,
                ExceptionMessage = e.Message,
                e.StackTrace,
                e.Source,
                e.Data,
                InnerException = SerializeException(e.InnerException, depth - 1)
            };
        }
    }
}
