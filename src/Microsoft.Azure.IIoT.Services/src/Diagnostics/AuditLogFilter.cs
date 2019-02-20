// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Diagnostics {
    using Microsoft.Azure.IIoT.Diagnostics.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Serilog;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Create audit log entries for every action invocation
    /// </summary>
    public sealed class AuditLogFilter : IAsyncActionFilter {

        /// <summary>
        /// Create filter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="auditor"></param>
        public AuditLogFilter(ILogger logger, IAuditLog auditor = null) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditor = auditor;
            if (auditor == null) {
                _logger.Information("No audit log registered, will log audit events " +
                    "to application log.");
                _auditor = new AuditLogLogger(_logger);
            }
            _writer = _auditor.OpenAsync(null).Result; // TODO
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
                        _logger.Error("Unknown result type {type}",
                            result.Result.GetType());
                        entry.Result = result.Result;
                        break;
                }
            }
            entry.Completed = DateTime.UtcNow;
            try {
                await _writer.WriteAsync(entry);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to write audit log for activity {id}", id);
            }
            // Let user know of the activity / audit id and return session id
            context.HttpContext.Response.Headers.Add(HttpHeader.ActivityId, id);
            if (sessionId != null) {
                context.HttpContext.Response.Headers.Add(HttpHeader.TrackingId,
                    sessionId);
            }
        }

        private readonly IAuditLog _auditor;
        private readonly ILogger _logger;
        private readonly IAuditLogWriter _writer;
    }
}
