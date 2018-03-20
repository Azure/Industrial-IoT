// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Diagnostics {
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Logger implementation
    /// </summary>
    public class Logger : ILogger {

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="loggingLevel"></param>
        public Logger(string processId, LogLevel loggingLevel) {
            _processId = processId;
            _logLevel = loggingLevel;
        }

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Debug(string message, Action context) {
            if (_logLevel > LogLevel.Debug) {
                return;
            }
            Write("DEBUG", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Info(string message, Action context) {
            if (_logLevel > LogLevel.Info) {
                return;
            }
            Write("INFO", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Warn(string message, Action context) {
            if (_logLevel > LogLevel.Warn) {
                return;
            }
            Write("WARN", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Error(string message, Action context) {
            if (_logLevel > LogLevel.Error) {
                return;
            }
            Write("ERROR", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Debug(string message, Func<object> context) {
            if (_logLevel > LogLevel.Debug) {
                return;
            }
            if (!string.IsNullOrEmpty(message)) {
                message += ", ";
            }
            message += JsonConvertEx.SerializeObjectPretty(context.Invoke());
            Write("DEBUG", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Info(string message, Func<object> context) {
            if (_logLevel > LogLevel.Info) {
                return;
            }
            if (!string.IsNullOrEmpty(message)) {
                message += ", ";
            }
            message += JsonConvertEx.SerializeObjectPretty(context.Invoke());
            Write("INFO", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Warn(string message, Func<object> context) {
            if (_logLevel > LogLevel.Warn) {
                return;
            }
            if (!string.IsNullOrEmpty(message)) {
                message += ", ";
            }
            message += JsonConvertEx.SerializeObjectPretty(context.Invoke());
            Write("WARN", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Error(string message, Func<object> context) {
            if (_logLevel > LogLevel.Error) {
                return;
            }
            if (!string.IsNullOrEmpty(message)) {
                message += ", ";
            }
            message += JsonConvertEx.SerializeObjectPretty(context.Invoke());
            Write("ERROR", context.GetMethodInfo(), message);
        }

        /// <summary>
        /// Log the message and information about the context, cleaning up
        /// and shortening the class name and method name (e.g. removing
        /// symbols specific to .NET internal implementation)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="context"></param>
        /// <param name="text"></param>
        private void Write(string level, MethodInfo context, string text) {
            // Extract the Class Name from the context
            var classname = "";
            if (context.DeclaringType != null) {
                classname = context.DeclaringType.FullName;
            }
            classname = classname.Split(new[] { '+' }, 2).First();
            classname = classname.Split('.').LastOrDefault();

            // Extract the Method Name from the context
            var methodname = context.Name;
            methodname = methodname.Split(new[] { '>' }, 2).First();
            methodname = methodname.Split(new[] { '<' }, 2).Last();

            var time = DateTimeOffset.UtcNow.ToString("u");
            var str = $"[{_processId}][{time}][{level}][{classname}:{methodname}] {text}";
            Console.WriteLine(str);
        }

        private readonly string _processId;
        private readonly LogLevel _logLevel;
    }
}
