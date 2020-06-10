// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;

    public partial class Gateways {
        [Parameter]
        public string Page { get; set; } = "1";

        private PagedResult<GatewayApiModel> GatewayList { get; set; } = new PagedResult<GatewayApiModel>();
        private PagedResult<GatewayApiModel> _pagedGatewayList = new PagedResult<GatewayApiModel>();
        private IAsyncDisposable _gatewayEvent;
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            GatewayList = CommonHelper.UpdatePage(RegistryHelper.GetGatewayListAsync, page, GatewayList, ref _pagedGatewayList, CommonHelper.PageLength);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "gateways/" + page);
            for (int i = 0; i < _pagedGatewayList.Results.Count; i++) {
                _pagedGatewayList.Results[i] = (await RegistryService.GetGatewayAsync(_pagedGatewayList.Results[i].Id)).Gateway;
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized() {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                GatewayList = await RegistryHelper.GetGatewayListAsync();
                Page = "1";
                _pagedGatewayList = GatewayList.GetPaged(int.Parse(Page), CommonHelper.PageLength, GatewayList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(_pagedGatewayList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
                _gatewayEvent = await RegistryServiceEvents.SubscribeGatewayEventsAsync(
                    ev => InvokeAsync(() => GatewayEvent(ev)));
            }
        }

        private Task GatewayEvent(GatewayEventApiModel ev) {
            GatewayList.Results.Update(ev);
            _pagedGatewayList = GatewayList.GetPaged(int.Parse(Page), CommonHelper.PageLength, GatewayList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public async void Dispose() {
            if (_gatewayEvent != null) {
                await _gatewayEvent.DisposeAsync();
            }
        }
    }
}