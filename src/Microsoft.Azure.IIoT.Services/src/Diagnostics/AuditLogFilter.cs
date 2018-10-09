// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Diagnostics {
    using Microsoft.Azure.IIoT.Storage.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using System;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Create audit log entries for every action invocation
    /// </summary>
    public class AuditLogFilter : IAsyncActionFilter {

        /// <summary>
        /// Create filter
        /// </summary>
        /// <param name="auditor"></param>
        /// <param name="logger"></param>
        public AuditLogFilter(IAuditLogWriter auditor = null, ILogger logger = null) {
            _logger = logger ?? new SimpleLogger();
            _auditor = auditor ?? new AuditLogLogger(_logger);
            if (auditor == null) {
                _logger.Error("No audit log registered, output audit log " +
                    "to passed in logger. Register an audit log service.");
            }
        }

        /// <inheritdoc/>
        public async Task OnActionExecutionAsync(ActionExecutingContext context,
            ActionExecutionDelegate next) {

            // Create new audit log id
            context.HttpContext.TraceIdentifier = Guid.NewGuid().ToString();

            // Get session id if present
            string sessionId = null;
            if (context.HttpContext.Request.Headers.TryGetValue(
                HttpHeader.TrackingId, out var id)) {
                sessionId = id;
            }

            // Create entry
            var entry = new AuditLogEntryModel {
                Id = context.HttpContext.TraceIdentifier,
                User = context.HttpContext.User?.Identity?.Name,
                Claims = context.HttpContext.User?.Claims?
                    .ToDictionary(c => c.Type, c => c.Value),
                SessionId = sessionId,
                OperationId = context.ActionDescriptor.Id,
                OperationName =
        $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                TimeStamp = DateTime.UtcNow,
                Parameters = context.ActionArguments
            };

            // Invoke action
            context.HttpContext.Items.Add(HttpContextEx.kEntryKey, entry);
            var result = await next.Invoke();

            // Convert result
            if (result.Canceled) {
                entry.Result = null;
                entry.Type = AuditLogEntryType.Cancellation;
            }
            else if (result.Exception != null) {
                entry.Result = result.Exception;
                entry.Type = AuditLogEntryType.Exception;
            }
            else {
                switch (result.Result) {
                    case ObjectResult obj:
                        entry.Result = obj.Value;
                        break;
                    case JsonResult json:
                        entry.Result = json.Value;
                        break;
                    case EmptyResult empty:
                        entry.Result = null;
                        break;
                    default:
                        _logger.Error(
                            $"Unknown result type {result.Result.GetType()}");
                        entry.Result = result.Result;
                        break;
                }
            }
            entry.Completed = DateTime.UtcNow;
            await _auditor.WriteAsync(entry);

            // Let user know of the activity / audit id and return session id
            context.HttpContext.Response.Headers.Add(HttpHeader.ActivityId, id);
            if (sessionId != null) {
                context.HttpContext.Response.Headers.Add(HttpHeader.TrackingId,
                    sessionId);
            }
        }

        private readonly IAuditLogWriter _auditor;
        private readonly ILogger _logger;
    }
}
