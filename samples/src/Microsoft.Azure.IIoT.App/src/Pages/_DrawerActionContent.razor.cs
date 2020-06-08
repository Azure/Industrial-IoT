// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.App.Models;
    using System.Threading.Tasks;
    using System.Linq;

    public partial class _DrawerActionContent {
        [Parameter]
        public ListNode NodeData { get; set; }

        [Parameter]
        public string EndpointId { get; set; }

        [Parameter]
        public PagedResult<ListNode> PagedNodeList { get; set; } = new PagedResult<ListNode>();

        [Parameter]
        public CredentialModel Credential { get; set; }

        public enum ActionType { Nothing, Read, Write, Call, Publish };

        private string _response { get; set; } = string.Empty;
        private string _value { get; set; } = string.Empty;
        private string[] _valueArray { get; set; }
        private ActionType _typeOfAction { get; set; } = ActionType.Nothing;
        private MethodMetadataResponseApiModel _parameters;
        private string _responseClass = "list-group-item text-left margin body-action-content hidden";

        private async Task SelectActionAsync(string nodeId, ChangeEventArgs action) {
            switch (action.Value) {
                case "Read":
                    _typeOfAction = ActionType.Read;
                    await ReadAsync(nodeId);
                    break;
                case "Write":
                    _typeOfAction = ActionType.Write;
                    break;
                case "Call":
                    _typeOfAction = ActionType.Call;
                    await ParameterAsync();
                    break;
                default:
                    break;
            }
        }

        private async Task ReadAsync(string nodeId) {
            _response = await BrowseManager.ReadValueAsync(EndpointId, nodeId, Credential);
            _responseClass = "list-group-item text-left margin body-action-content visible";
        }

        private async Task WriteAsync(string nodeId, string value) {
            _response = await BrowseManager.WriteValueAsync(EndpointId, nodeId, value, Credential);

            var newValue = await BrowseManager.ReadValueAsync(EndpointId, nodeId, Credential);
            var index = PagedNodeList.Results.IndexOf(PagedNodeList.Results.SingleOrDefault(x => x.Id == nodeId));
            PagedNodeList.Results[index].Value = newValue;
            _responseClass = "list-group-item margin body-action-content visible";
        }

        private async Task ParameterAsync() {
            _response = await BrowseManager.GetParameterAsync(EndpointId, NodeData.Id, Credential);
            _parameters = BrowseManager.Parameter;
            if (_parameters.InputArguments != null) {
                _valueArray = new string[_parameters.InputArguments.Count];
            }
        }

        private async Task CallAsync(string nodeId, string[] values) {
            _response = await BrowseManager.MethodCallAsync(_parameters, values, EndpointId, NodeData.Id, Credential);
            _responseClass = "list-group-item margin body-action-content visible";
        }
    }
}