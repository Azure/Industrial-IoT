// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Browser.Controllers {
    using Microsoft.Azure.IoTSolutions.Browser.Properties;
    using Microsoft.Azure.IoTSolutions.Browser.Helpers;
    using Microsoft.Azure.IoTSolutions.Browser.Filters;
    using Microsoft.Azure.IoTSolutions.Browser.Models;
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client;
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mime;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.AspNetCore.Mvc.Rendering;

    [Route("/[Controller]")]
    [ExceptionsFilter]
    // [Authorize(Policy = Policy.BrowseOpcServer)]
    public class BrowseController : Controller {

        /// <summary>
        /// Create browser controller
        /// </summary>
        /// <param name="twin"></param>
        public BrowseController(IOpcTwinService twin, ILogger logger) {
            _twin = twin;
            _logger = logger;
        }


        /// <summary>
        /// Default action of the controller.
        /// </summary>
        [HttpGet("Index")]
        public ActionResult Index() {
            // If not existing session, switch to endpoints
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }
            return View("Index", new BrowseViewModel { EndpointId = endpointId });
        }

        /// <summary>
        /// Post method to read information of the root node of connected OPC UA server.
        /// </summary>
        [HttpPost("GetRootNode")]
        [ValidateAntiForgeryToken(Order = 1)]
        public async Task<ActionResult> GetRootNodeAsync() {
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }
            var result = await _twin.NodeBrowseAsync(endpointId, new BrowseRequestApiModel());
            return Json(new List<object> {
                new {
                    id = result.Node.Id,
                    text = Resources.BrowserRootNodeName,
                    children = result.Node.HasChildren
                }
            });
        }

        /// <summary>
        /// Post method to read information on the children of the given node in connected OPC UA server.
        /// </summary>
        [HttpPost("GetChildren")]
        [ValidateAntiForgeryToken(Order = 1)]
        public async Task<ActionResult> GetChildren(string jstreeNode) {
            var nodeId = NodeParseHelper.Parse(jstreeNode, out var parent);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }

            var result = await _twin.NodeBrowseAsync(endpointId, new BrowseRequestApiModel {
                IncludePublishingStatus = true,
                NodeId = nodeId,
                Parent = parent
            });

            if (result.Node.HasChildren ?? false) {
                return Json(result.References.Select(nodeReference =>
                    new {
                        id = NodeParseHelper.Format(nodeReference.Target.Id,
                            nodeReference.Target.ParentNode),
                        text = nodeReference.Text,
                        nodeClass = nodeReference.Target.NodeClass,
                        accessLevel = nodeReference.Target.AccessLevel,
                        eventNotifier = nodeReference.Target.EventNotifier,
                        executable = nodeReference.Target.Executable,
                        children = nodeReference.Target.HasChildren ?? false,
                        publishedNode = nodeReference.Target.IsPublished,
                        relevantNode = IsRelevant(nodeReference.Target.Id)
                    }).ToList());
            }

            // Only the node info was requested, not the children...
            return Json(new List<object> {
                new {
                    id = jstreeNode,
                    text = result.Node.Text,
                    nodeClass = result.Node.NodeClass,
                    accessLevel = result.Node.AccessLevel,
                    eventNotifier = result.Node.EventNotifier,
                    executable = result.Node.Executable.ToString(),
                    children = result.Node.HasChildren ?? false
                }
            });
        }

        /// <summary>
        /// Post method to read the value of a variable identified by the given node.
        /// </summary>
        [HttpPost("VariableRead")]
        [ValidateAntiForgeryToken(Order = 1)]
        public async Task<ActionResult> VariableRead(string jstreeNode) {
            var node = NodeParseHelper.Parse(jstreeNode, out var parent);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }

            var result = await _twin.NodeValueReadAsync(endpointId,
                new ValueReadRequestApiModel {
                    NodeId = node
                });

            var value = "";
            if (!string.IsNullOrEmpty(result.Value)) {
                if (result.Value.Length > 40) {
                    value = result.Value.Substring(0, 40);
                    value += "...";
                }
                else {
                    value = result.Value;
                }
            }
            // We return the HTML formatted content, which is shown in the context panel.
            var actionResult =
                Resources.BrowserOpcDataValueLabel + ": " + value + @"<br/>" +
                Resources.BrowserOpcDataSourceTimestampLabel + ": " + result.SourceTimestamp + @"<br/>" +
                Resources.BrowserOpcDataServerTimestampLabel + ": " + result.ServerTimestamp;
            return Content(actionResult);
        }

        /// <summary>
        /// Post method to publish or unpublish the value of a variable identified by
        /// the given node.
        /// </summary>
        [HttpPost("VariablePublishUnpublish")]
        [ValidateAntiForgeryToken(Order = 1)]
        // [Authorize(Policy = Policy.PublishOpcNode)]
        public async Task<ActionResult> VariablePublishUnpublish(string jstreeNode, string method) {
            var node = NodeParseHelper.Parse(jstreeNode, out var parent);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }
            var result = await _twin.NodePublishAsync(endpointId,
                new PublishRequestApiModel {
                    NodeId = node,
                    Enabled = method != "unpublish"
                });
            var actionResult = Resources.BrowserOpcMethodCallSucceeded;
            if (result.Diagnostics != null) {
                actionResult =
                    Resources.BrowserOpcMethodCallFailed + @"<br/><br/>" +
                    Resources.BrowserOpcDataDiagnosticInfoLabel + ": " + result.Diagnostics;
            }
            return Content(actionResult);
        }

        /// <summary>
        /// Post method to fetch the value of a variable identified by the given node,
        /// which should be updated.
        /// </summary>
        [HttpPost("VariableWriteFetch")]
        [ValidateAntiForgeryToken(Order = 1)]
        // [Authorize(Policy = Policy.ControlOpcServer)]
        public async Task<ActionResult> VariableWriteFetch(string jstreeNode) {
            var node = NodeParseHelper.Parse(jstreeNode, out var parent);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }
            var result = await _twin.NodeValueReadAsync(endpointId,
                new ValueReadRequestApiModel {
                    NodeId = node
                });
            var actionResult = "";
            if (!string.IsNullOrEmpty(result.Value)) {
                if (result.Value.Length > 30) {
                    actionResult = result.Value.Substring(0, 30);
                    actionResult += "...";
                }
                else {
                    actionResult = result.Value;
                }
            }
            return Content(actionResult);
        }

        /// <summary>
        /// Update the values of a variable identified by the given node with the given value.
        /// </summary>
        [HttpPost("VariableWriteUpdate")]
        [ValidateAntiForgeryToken(Order = 1)]
        // [Authorize(Policy = Policy.ControlOpcServer)]
        public async Task<ActionResult> VariableWriteUpdateAsync(
            string jstreeNode, string newValue) {

            var nodeId = NodeParseHelper.Parse(jstreeNode, out var parent);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }

            // First read node properties through browse to get meta data
            var browseresult = await _twin.NodeBrowseAsync(endpointId,
                new BrowseRequestApiModel {
                    NodeId = nodeId,
                    Parent = parent,
                    ExcludeReferences = true
                });
            var diagnostics = browseresult.Diagnostics;
            if (browseresult.Node != null) {
                // Then write using the meta data
                var result = await _twin.NodeValueWriteAsync(endpointId,
                    new ValueWriteRequestApiModel {
                        Node = browseresult.Node,
                        Value = newValue
                    });
                diagnostics = result.Diagnostics;
                if (diagnostics != null) {
                    return Content("");
                }
            }
            return Content(
                Resources.BrowserOpcMethodCallFailed + @"<br/><br/>" +
                Resources.BrowserOpcDataDiagnosticInfoLabel + ": " + diagnostics);
        }

        /// <summary>
        /// Get the parameters of a method call identified by the given node.
        /// </summary>
        [HttpPost("MethodCallGetParameter")]
        [ValidateAntiForgeryToken(Order = 1)]
        // [Authorize(Policy = Policy.ControlOpcServer)]
        public async Task<ActionResult> MethodCallGetParameterAsync(
            string jstreeNode) {

            var node = NodeParseHelper.Parse(jstreeNode, out var parent);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }
            var result = await _twin.NodeMethodGetMetadataAsync(endpointId,
                new MethodMetadataRequestApiModel {
                    MethodId = node
                });
            if (result.InputArguments != null) {
                return Json(new {
                    count = result.InputArguments.Count,
                    parameter = result.InputArguments.Select(argument => new {
                        name = argument.Name,
                        value = argument.Value,
                        valuerank = argument.ValueRank ?? -1,
                        arraydimentions = argument.ArrayDimensions ?? new uint[0],
                        description = argument.Description,
                        datatype = argument.TypeId,
                        typename = argument.TypeName
                    }).ToList()
                });
            }
            return Json(new { count = 0, parameter = new object[] { } });
        }

        /// <summary>
        /// Post method to call an OPC UA method in the server.
        /// </summary>
        [HttpPost("MethodCall")]
        [ValidateAntiForgeryToken(Order = 1)]
        // [Authorize(Policy = Policy.ControlOpcServer)]
        public async Task<ActionResult> MethodCallAsync(string jstreeNode,
            string parameterData, string parameterValues) {
            var node = NodeParseHelper.Parse(jstreeNode, out var parentNode);
            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }

            var originalData = JsonConvert.DeserializeObject<List<dynamic>>(parameterData);
            var values = JsonConvert.DeserializeObject<List<dynamic>>(parameterValues);
            if (values.Count != originalData.Count) {
                throw new ArgumentException("Count must be the same");
            }
            var count = values.Count;
            var arguments = new List<MethodArgumentApiModel>();
            for (var i = 0; i < values.Count; i++) {
                arguments.Add(new MethodArgumentApiModel {
                    TypeName = originalData[i].typename,
                    TypeId = originalData[i].datatype,
                    ArrayDimensions = originalData[i].arraydimentions,
                    ValueRank = originalData[i].valueRank,
                    Value = values[i].Value
                });
            }
            var result = await _twin.NodeMethodCallAsync(endpointId,
                new MethodCallRequestApiModel {
                    ObjectId = parentNode,
                    MethodId = node,
                    Arguments = arguments
                });

            if (result.Diagnostics != null) {
                return Content(
                    Resources.BrowserOpcMethodCallFailed + @"<br/><br/>" +
                    Resources.BrowserOpcDataDiagnosticInfoLabel + ": " + result.Diagnostics);
            }
            if ((result.Results?.Count ?? 0) == 0) {
                return Content(Resources.BrowserOpcMethodCallSucceeded);
            }
            var actionResult = Resources.BrowserOpcMethodCallSucceededWithResults + @"<br/><br/>";
            foreach (var output in result.Results) {
                actionResult += output + "@<br/>";
            }
            return Content(actionResult);
        }

        /// <summary>
        /// Downloads the web app's UA client application certificate for use in
        /// UA servers the user wants to connect to
        /// </summary>
        [HttpGet("Download")]
        public async Task<ActionResult> Download() {

            var endpointId = GetFromSession("endpoint");
            if (string.IsNullOrEmpty(endpointId)) {
                return RedirectToAction("Index", "Endpoints");
            }
            var certString = string.Empty; await Task.Delay(0);// TOOD await _twin.GetClientCertificateAsync(endpointId);
            var certificate = new X509Certificate2(Convert.FromBase64String(certString));
            return File(certificate.GetRawCertData(),
                MediaTypeNames.Application.Octet, certificate.FriendlyName + ".der");
        }


        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool IsRelevant(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentException(nameof(id));
            }
            // TODO:
            return false;
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
