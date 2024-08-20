/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

#nullable enable

namespace Asset
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Opc.Ua.Export;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Text;

    internal sealed class UACloudLibraryClient : IDisposable
    {
        public Dictionary<string, string> NamespacesInCloudLibrary { get; } = new();
        public List<string> NodeSetFilenames { get; } = new List<string>();

        public UACloudLibraryClient(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public void Login(string uaCloudLibraryUrl, string? clientId, string? secret)
        {
            try
            {
                if ((NamespacesInCloudLibrary.Count == 0) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(secret))
                {
                    _client.DefaultRequestHeaders.Remove("Authorization");
                    _client.DefaultRequestHeaders.Add("Authorization", "basic " +
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId + ":" + secret)));

                    if (!uaCloudLibraryUrl.EndsWith('/'))
                    {
                        uaCloudLibraryUrl += "/";
                    }

                    // get namespaces
                    var address = uaCloudLibraryUrl + "infomodel/namespaces";
                    using var request = new HttpRequestMessage(HttpMethod.Get, address);
                    var response = _client.Send(request);
                    var identifiers = JsonConvert.DeserializeObject<string[]>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                    if (identifiers != null)
                    {
                        foreach (var nodeset in identifiers)
                        {
                            var tuple = nodeset.Split(",");
                            if (tuple.Length == 2)
                            {
                                NamespacesInCloudLibrary[tuple[0]] = tuple[1];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging into cloud library");
                return;
            }
        }

        public bool DownloadNamespace(string uaCloudLibraryUrl, string namespaceUrl)
        {
            if (!string.IsNullOrEmpty(uaCloudLibraryUrl) && !string.IsNullOrEmpty(namespaceUrl) &&
                NamespacesInCloudLibrary.TryGetValue(namespaceUrl, out var value))
            {
                if (!uaCloudLibraryUrl.EndsWith('/'))
                {
                    uaCloudLibraryUrl += "/";
                }

                var address = uaCloudLibraryUrl + "infomodel/download/" + Uri.EscapeDataString(value);
                using var request = new HttpRequestMessage(HttpMethod.Get, address);
                var response = _client.Send(request);

                try
                {
                    var nameSpace = JsonConvert.DeserializeObject<UANameSpace>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    if (nameSpace != null && !string.IsNullOrEmpty(nameSpace.Nodeset.NodesetXml))
                    {
                        // store the file locally
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameSpace.Title + ".nodeset2.xml");
                        File.WriteAllText(filePath, nameSpace.Nodeset.NodesetXml);

                        NodeSetFilenames.Add(filePath);

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading from cloud library");
                    return false;
                }
            }

            return false;
        }

        public string ValidateNamespacesAndModels(string uaCloudLibraryUrl, bool autodownloadreferences)
        {
            // Collect all models as well as all required/referenced model namespace URIs listed in each file
            var models = new List<string>();
            var modelreferences = new List<string>();
            foreach (var nodesetFile in NodeSetFilenames)
            {
                using (Stream stream = new FileStream(nodesetFile, FileMode.Open))
                {
                    var nodeSet = UANodeSet.Read(stream);

                    // validate namespace URIs
                    if (nodeSet.NamespaceUris?.Length > 0)
                    {
                        foreach (var ns in nodeSet.NamespaceUris)
                        {
                            if (string.IsNullOrEmpty(ns) || !Uri.IsWellFormedUriString(ns, UriKind.Absolute))
                            {
                                return "Nodeset file " + nodesetFile + " contains an invalid Namespace URI: \"" + ns + "\"";
                            }
                        }
                    }
                    else
                    {
                        return "'NamespaceUris' entry missing in " + nodesetFile + ". Please add it!";
                    }

                    // validate model URIs
                    if (nodeSet.Models?.Length > 0)
                    {
                        foreach (var model in nodeSet.Models)
                        {
                            if (model != null)
                            {
                                if (Uri.IsWellFormedUriString(model.ModelUri, UriKind.Absolute))
                                {
                                    // ignore the default namespace which is always present and don't add duplicates
                                    if ((model.ModelUri != "http://opcfoundation.org/UA/") && !models.Contains(model.ModelUri))
                                    {
                                        models.Add(model.ModelUri);
                                    }
                                }
                                else
                                {
                                    return "Nodeset file " + nodesetFile + " contains an invalid Model Namespace URI: \"" + model.ModelUri + "\"";
                                }

                                if (model.RequiredModel?.Length > 0)
                                {
                                    foreach (var requiredModel in model.RequiredModel)
                                    {
                                        if (requiredModel != null)
                                        {
                                            if (Uri.IsWellFormedUriString(requiredModel.ModelUri, UriKind.Absolute))
                                            {
                                                // ignore the default namespace which is always required and don't add duplicates
                                                if ((requiredModel.ModelUri != "http://opcfoundation.org/UA/") && !modelreferences.Contains(requiredModel.ModelUri))
                                                {
                                                    modelreferences.Add(requiredModel.ModelUri);
                                                }
                                            }
                                            else
                                            {
                                                return "Nodeset file " + nodesetFile + " contains an invalid referenced Model Namespace URI: \"" + requiredModel.ModelUri + "\"";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        return "'Model' entry missing in " + nodesetFile + ". Please add it!";
                    }
                }
            }

            // now check if we have all references for each model we want to load
            foreach (var modelreference in modelreferences)
            {
                if (!models.Contains(modelreference))
                {
                    if (!autodownloadreferences)
                    {
                        return "Referenced OPC UA model " + modelreference + " is missing from selected list of nodeset files, please add the corresponding nodeset file to the list of loaded files!";
                    }

                    try
                    {
                        // try to auto-download the missing references from the UA Cloud Library
                        if (!uaCloudLibraryUrl.EndsWith('/'))
                        {
                            uaCloudLibraryUrl += "/";
                        }

                        var address = uaCloudLibraryUrl + "infomodel/download/" + Uri.EscapeDataString(NamespacesInCloudLibrary[modelreference]);
                        using var request = new HttpRequestMessage(HttpMethod.Get, address);
                        var response = _client.Send(request);
                        var nameSpace = JsonConvert.DeserializeObject<UANameSpace>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        if (nameSpace != null)
                        {
                            // store the file
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), nameSpace.Category.Name + ".nodeset2.xml");
                            File.WriteAllText(filePath, nameSpace.Nodeset.NodesetXml);
                            NodeSetFilenames.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        return "Could not download referenced nodeset " + modelreference + ": " + ex.Message;
                    }
                }
            }

            return string.Empty; // no error
        }

        private readonly HttpClient _client = new HttpClient();
        private readonly ILogger _logger;
    }
}
