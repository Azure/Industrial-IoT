// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using global::Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.App.Extensions;
    using Microsoft.Azure.IIoT.App.Models;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    public sealed partial class Gateways
    {
        [Parameter]
        public string Page { get; set; } = "1";

        private PagedResult<GatewayModel> GatewayList { get; set; } = new PagedResult<GatewayModel>();
        private PagedResult<GatewayModel> _pagedGatewayList = new();
        private IAsyncDisposable _gatewayEvent;
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        public async Task PagerPageChangedAsync(int page)
        {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            GatewayList = CommonHelper.UpdatePage(RegistryHelper.GetGatewayListAsync, page, GatewayList, ref _pagedGatewayList, CommonHelper.PageLength);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "gateways/" + page);
            for (var i = 0; i < _pagedGatewayList.Results.Count; i++)
            {
                _pagedGatewayList.Results[i] = (await RegistryService.GetGatewayAsync(_pagedGatewayList.Results[i].Id).ConfigureAwait(false)).Gateway;
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            CommonHelper.Spinner = "loader-big";
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                GatewayList = await RegistryHelper.GetGatewayListAsync().ConfigureAwait(false);
                Page = "1";
                _pagedGatewayList = GatewayList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                    CommonHelper.PageLength, GatewayList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(_pagedGatewayList, ref _tableView, ref _tableEmpty);
                StateHasChanged();
                _gatewayEvent = await RegistryServiceEvents.SubscribeGatewayEventsAsync(
                    ev => InvokeAsync(() => GatewayEventAsync(ev))).ConfigureAwait(false);
            }
        }

        private Task GatewayEventAsync(GatewayEventModel ev)
        {
            GatewayList.Results.Update(ev);
            _pagedGatewayList = GatewayList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                CommonHelper.PageLength, GatewayList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_gatewayEvent != null)
            {
                await _gatewayEvent.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
