// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages
{
    using Microsoft.Azure.IIoT.App.Extensions;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.AspNetCore.Components;
    using global::Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    public sealed partial class Discoverers
    {
        [Parameter]
        public string Page { get; set; } = "1";

        public bool IsOpen { get; set; }
        public DiscovererInfo DiscovererData { get; set; }
        public string Status { get; set; }

        private PagedResult<DiscovererInfo> DiscovererList { get; set; } = new PagedResult<DiscovererInfo>();
        private PagedResult<DiscovererInfo> _pagedDiscovererList = new();
        private string EventResult { get; set; }
        private string ScanResult { get; set; } = "displayNone";
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        private IAsyncDisposable _discovererEvent;
        private IAsyncDisposable Discovery { get; set; }
        private bool IsDiscoveryEventSubscribed { get; set; }

        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task PagerPageChangedAsync(int page)
        {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            DiscovererList = CommonHelper.UpdatePage(RegistryHelper.GetDiscovererListAsync, page, DiscovererList, ref _pagedDiscovererList, CommonHelper.PageLengthSmall);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "discoverers/" + page);
            foreach (var discoverer in _pagedDiscovererList.Results)
            {
                discoverer.DiscovererModel = await RegistryService.GetDiscovererAsync(discoverer.DiscovererModel.Id).ConfigureAwait(false);
                discoverer.ScanStatus = discoverer.DiscovererModel.Discovery is not DiscoveryMode.Off and not null;
                var applicationModel = new ApplicationRegistrationQueryModel { DiscovererId = discoverer.DiscovererModel.Id };
                var applications = await RegistryService.QueryApplicationsAsync(applicationModel, 1).ConfigureAwait(false);
                if (applications != null)
                {
                    discoverer.HasApplication = true;
                }
            }
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override Task OnInitializedAsync()
        {
            CommonHelper.Spinner = "loader-big";
            return base.OnInitializedAsync();
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                DiscovererList = await RegistryHelper.GetDiscovererListAsync().ConfigureAwait(false);
                Page = "1";
                _pagedDiscovererList = DiscovererList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                    CommonHelper.PageLengthSmall, DiscovererList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty(_pagedDiscovererList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _discovererEvent = await RegistryServiceEvents.SubscribeDiscovererEventsAsync(
                    ev => InvokeAsync(() => DiscovererEventAsync(ev))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Enable discoverer scan
        /// </summary>
        /// <param name="discoverer"></param>
        /// <param name="checkStatus"></param>
        private async Task SetScanAsync(DiscovererInfo discoverer, bool checkStatus)
        {
            try
            {
                discoverer.ScanStatus = checkStatus;
                EventResult = string.Empty;

                if (discoverer.ScanStatus)
                {
                    if (!IsDiscoveryEventSubscribed)
                    {
                        Discovery = await RegistryServiceEvents.SubscribeDiscoveryProgressByDiscovererIdAsync(
                            discoverer.DiscovererModel.Id, async data => await InvokeAsync(() => ScanProgress(data)).ConfigureAwait(false)).ConfigureAwait(false);
                    }

                    IsDiscoveryEventSubscribed = true;
                    discoverer.IsSearching = true;
                    ScanResult = "displayBlock";
                    DiscovererData = discoverer;
                }
                else
                {
                    discoverer.IsSearching = false;
                    ScanResult = "displayNone";
                    if (Discovery != null)
                    {
                        await Discovery.DisposeAsync().ConfigureAwait(false);
                    }
                    IsDiscoveryEventSubscribed = false;
                }
                Status = await RegistryHelper.SetDiscoveryAsync(discoverer).ConfigureAwait(false);
            }
            catch
            {
                if (Discovery != null)
                {
                    await Discovery.DisposeAsync().ConfigureAwait(false);
                }
                IsDiscoveryEventSubscribed = false;
            }
        }

        /// <summary>
        /// Start ad-hoc scan
        /// </summary>
        /// <param name="discoverer"></param>
        private async Task SetAdHocScanAsync(DiscovererInfo discoverer)
        {
            if (!IsDiscoveryEventSubscribed)
            {
                discoverer.DiscoveryRequestId = Guid.NewGuid().ToString();
                Discovery = await RegistryServiceEvents.SubscribeDiscoveryProgressByRequestIdAsync(
                discoverer.DiscoveryRequestId, async data => await InvokeAsync(() => ScanProgress(data)).ConfigureAwait(false)).ConfigureAwait(false);
                IsDiscoveryEventSubscribed = true;
            }

            try
            {
                EventResult = string.Empty;

                discoverer.IsSearching = true;
                ScanResult = "displayBlock";
                DiscovererData = discoverer;
                Status = await RegistryHelper.DiscoverServersAsync(discoverer).ConfigureAwait(false);
            }
            catch
            {
                if (Discovery != null)
                {
                    await Discovery.DisposeAsync().ConfigureAwait(false);
                }
                IsDiscoveryEventSubscribed = false;
            }
        }

        /// <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        /// <param name="discoverer"></param>
        private void OpenDrawer(DiscovererInfo discoverer)
        {
            IsOpen = true;
            DiscovererData = discoverer;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer()
        {
            IsOpen = false;
            StateHasChanged();
        }

        /// <summary>
        /// display discoverers scan events
        /// </summary>
        /// <param name="ev"></param>
        private void ScanProgress(DiscoveryProgressModel ev)
        {
            var ts = ev.TimeStamp.ToLocalTime();
            switch (ev.EventType)
            {
                case DiscoveryProgressType.Pending:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Total} waiting..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.Started:
                    EventResult += $"[{ts}] {ev.DiscovererId}: Started." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    EventResult += $"[{ts}] {ev.DiscovererId}: Scanning network..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} addresses found - NEW: {ev.Result}..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} addresses found" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} addresses found - complete!" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    EventResult += $"[{ts}] {ev.DiscovererId}: Scanning ports..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanResult:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} ports found - NEW: {ev.Result}" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} ports found" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} ports found - complete!" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    EventResult += "==========================================" + System.Environment.NewLine;
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: Finding servers..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: ... {ev.Discovered} servers found - find endpoints on {ev.RequestDetails["url"]}..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: ... {ev.Discovered} servers found - {ev.Result} endpoints found on {ev.RequestDetails["url"]}..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    EventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: ... {ev.Discovered} servers found." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.Cancelled:
                    EventResult += "==========================================" + System.Environment.NewLine;
                    EventResult += $"[{ts}] {ev.DiscovererId}: Cancelled." + System.Environment.NewLine;
                    if (DiscovererData != null)
                    {
                        DiscovererData.IsSearching = false;
                    }
                    break;
                case DiscoveryProgressType.Error:
                    EventResult += "==========================================" + System.Environment.NewLine;
                    EventResult += $"[{ts}] {ev.DiscovererId}: Failure." + System.Environment.NewLine;
                    if (DiscovererData != null)
                    {
                        DiscovererData.IsSearching = false;
                    }
                    break;
                case DiscoveryProgressType.Finished:
                    EventResult += "==========================================" + System.Environment.NewLine;
                    EventResult += $"[{ts}] {ev.DiscovererId}: Completed." + System.Environment.NewLine;
                    if (DiscovererData != null)
                    {
                        DiscovererData.IsSearching = false;
                    }
                    break;
            }
            StateHasChanged();
        }

        /// <summary>
        /// ClickHandler
        /// </summary>
        /// <param name="discoverer"></param>
        private async Task ClickHandlerAsync(DiscovererInfo discoverer)
        {
            CloseDrawer();
            if (discoverer.isAdHocDiscovery)
            {
                await SetAdHocScanAsync(discoverer).ConfigureAwait(false);
            }
            else
            {
                await OnAfterRenderAsync(true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// refresh UI on DiscovererEvent
        /// </summary>
        /// <param name="ev"></param>
        private Task DiscovererEventAsync(DiscovererEventModel ev)
        {
            DiscovererList.Results.Update(ev);
            _pagedDiscovererList = DiscovererList.GetPaged(int.Parse(Page, CultureInfo.InvariantCulture),
                CommonHelper.PageLength, DiscovererList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close the scan result view
        /// </summary>
        public void CloseScanResultView()
        {
            ScanResult = "displayNone";
        }

        public async ValueTask DisposeAsync()
        {
            if (_discovererEvent != null)
            {
                await _discovererEvent.DisposeAsync().ConfigureAwait(false);
            }

            if (Discovery != null)
            {
                await Discovery.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
