// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Browser.Controllers {
    using Microsoft.Azure.IoTSolutions.Browser.Filters;
    using Microsoft.Azure.IoTSolutions.Browser.Models;
    using Microsoft.Azure.IoTSolutions.Browser.Properties;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    [Route("/[Controller]")]
    [ExceptionsFilter]
    // [Authorize(Policy = Policy.BrowseOpcServer)]
    public class EndpointsController : Controller {

        /// <summary>
        /// Create browser controller
        /// </summary>
        /// <param name="twin"></param>
        public EndpointsController(IOpcTwinService twin, ILogger logger) {
            _twin = twin;
            _logger = logger;
        }

        /// <summary>
        /// Default action of the controller.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Index() {

            // See if service is working
            var status = await _twin.GetServiceStatusAsync();
            if (!status.Status.StartsWith("OK:", StringComparison.Ordinal)) {
                return View("Error", new ErrorViewModel { ErrorMessage = status.Status });
            }

            // If existing session, switch to browse
            var endpointId = GetFromSession("endpoint");
            if (!string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Browse");
            }

            // Retrieve all endpoints to display (TODO: should page here)
            var registrations = await _twin.ListAllTwinsAsync();
            return View("Index", new EndpointsViewModel {
                PrepopulatedEndpoints = registrations.Select(e => new SelectListItem {
                    Text = e.Endpoint.Url, Value = e.Id
                }).ToList()
            });
        }

        /// <summary>
        /// Action to show a browser related  error message.
        /// </summary>
        [HttpGet("Error")]
        public ActionResult Error(string errorMessage) {
            return View("Error", new ErrorViewModel {
                ErrorHeader = Resources.BrowserOpcErrorHeader,
                EndpointId = GetFromSession("endpoint"),
                ErrorMessage = HttpUtility.HtmlDecode(errorMessage)
            });
        }

        /// <summary>
        /// Post form method to connect to an OPC UA server.
        /// </summary>
        [HttpPost("Connect")]
        [ValidateAntiForgeryToken(Order = 1)]
        public ActionResult Connect(string endpointId) {

            if (HttpContext.Session.IsAvailable) {
                HttpContext.Session.Set("endpoint", Encoding.UTF8.GetBytes(endpointId));
                return RedirectToAction("Index", "Browse");
            }
            return View("Error", new ErrorViewModel { ErrorMessage = "session in progress" });
        }

        /// <summary>
        /// Get method to disconnect from the currently connected OPC UA server.
        /// </summary>
        [HttpGet("Disconnect")]
        public Task<ActionResult> DisconnectAsync() {
            if (HttpContext.Session.IsAvailable) {
                HttpContext.Session.Clear();
            }
            return Index();
        }

        /// <summary>
        /// Return the endpoint id from the current session.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetFromSession(string key) {
            if (HttpContext.Session.IsAvailable &&
                HttpContext.Session.TryGetValue(key, out var id)) {
                return Encoding.UTF8.GetString(id);
            }
            return null;
        }

        private readonly IOpcTwinService _twin;
        private ILogger _logger;
    }
}
