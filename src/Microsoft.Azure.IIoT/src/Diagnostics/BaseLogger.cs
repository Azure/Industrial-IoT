// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Logger base implementation
    /// </summary>
    public abstract class BaseLogger : ILogger {

        /// <summary>
        /// Create logger
        /// </summary>
        /// <param name="processId"></param>
        protected BaseLogger(string processId) {
            _processId = processId;
        }

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Debug(string message, Action context) =>
            Debug(() => Write(context.GetMethodInfo(), message));

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Info(string message, Action context) =>
            Info(() => Write(context.GetMethodInfo(), message));

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Warn(string message, Action context) =>
            Warn(() => Write(context.GetMethodInfo(), message));

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Error(string message, Action context) =>
            Error(() => Write(context.GetMethodInfo(), message));

        /// <summary>
        /// Log debug
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Debug(string message, Func<object> context) =>
            Debug(() => Write(context.GetMethodInfo(), message + " " +
                JsonConvertEx.SerializeObject(context.Invoke())));

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Info(string message, Func<object> context) =>
            Info(() => Write(context.GetMethodInfo(), message + " " +
                JsonConvertEx.SerializeObject(context.Invoke())));

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Warn(string message, Func<object> context) =>
            Warn(() => Write(context.GetMethodInfo(), message + " " +
                JsonConvertEx.SerializeObject(context.Invoke())));

        /// <summary>
        /// Log error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Error(string message, Func<object> context) =>
            Error(() => Write(context.GetMethodInfo(), message + " " +
                JsonConvertEx.SerializeObject(context.Invoke())));

        protected abstract void Debug(Func<string> message);
        protected abstract void Info(Func<string> message);
        protected abstract void Warn(Func<string> message);
        protected abstract void Error(Func<string> message);

        /// <summary>
        /// Log the message and information about the context, cleaning up
        /// and shortening the class name and method name (e.g. removing
        /// symbols specific to .NET internal implementation)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="text"></param>
        protected virtual string Write(MethodInfo context, string text) {
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
            return $"[{_processId}][{time}][{classname}:{methodname}] {text}";
        }

        protected readonly string _processId;
    }
}
