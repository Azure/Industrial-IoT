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

        private string Response { get; set; } = string.Empty;
        private string Value { get; set; } = string.Empty;
        private string[] Values { get; set; }
        private ActionType TypeOfAction { get; set; } = ActionType.Nothing;
        private MethodMetadataResponseApiModel _parameters { get; set; }
        private string ResponseClass { get; set; } = "list-group-item text-left margin body-action-content hidden";

        private async Task SelectActionAsync(string nodeId, ChangeEventArgs action) {
            switch (action.Value) {
                case "Read":
                    TypeOfAction = ActionType.Read;
                    await ReadAsync(nodeId);
                    break;
                case "Write":
                    TypeOfAction = ActionType.Write;
                    break;
                case "Call":
                    TypeOfAction = ActionType.Call;
                    await ParameterAsync();
                    break;
                default:
                    break;
            }
        }

        private async Task ReadAsync(string nodeId) {
            Response = await BrowseManager.ReadValueAsync(EndpointId, nodeId, Credential);
            ResponseClass = "list-group-item text-left margin body-action-content visible";
        }

        private async Task WriteAsync(string nodeId, string value) {
            Response = await BrowseManager.WriteValueAsync(EndpointId, nodeId, value, Credential);

            var newValue = await BrowseManager.ReadValueAsync(EndpointId, nodeId, Credential);
            var index = PagedNodeList.Results.IndexOf(PagedNodeList.Results.SingleOrDefault(x => x.Id == nodeId));
            PagedNodeList.Results[index].Value = newValue;
            ResponseClass = "list-group-item margin body-action-content visible";
        }

        private async Task ParameterAsync() {
            Response = await BrowseManager.GetParameterAsync(EndpointId, NodeData.Id, Credential);
            _parameters = BrowseManager.Parameter;
            if (_parameters.InputArguments != null) {
                Values = new string[_parameters.InputArguments.Count];
            }
        }

        private async Task CallAsync(string nodeId, string[] values) {
            Response = await BrowseManager.MethodCallAsync(_parameters, values, EndpointId, NodeData.Id, Credential);
            ResponseClass = "list-group-item margin body-action-content visible";
        }
    }
}