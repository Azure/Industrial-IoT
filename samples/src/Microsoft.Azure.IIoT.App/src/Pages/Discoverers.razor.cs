// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Pages {
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.App.Data;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;

    public partial class Discoverers {
        [Parameter]
        public string Page { get; set; } = "1";

        public bool IsSearching { get; set; } = false;
        public bool IsOpened { get; set; } = false;
        public DiscovererInfo DiscovererData { get; set; }
        public string Status { get; set; }

        private PagedResult<DiscovererInfo> _discovererList = new PagedResult<DiscovererInfo>();
        private PagedResult<DiscovererInfo> _pagedDiscovererList = new PagedResult<DiscovererInfo>();
        private string _eventResult { get; set; }
        private string _scanResult { get; set; } = "displayNone";
        private string _tableView = "visible";
        private string _tableEmpty = "displayNone";

        private IAsyncDisposable _discovererEvent { get; set; }
        private IAsyncDisposable _discovery { get; set; }
        private bool _isDiscoveryEventSubscribed { get; set; } = false;


        /// <summary>
        /// Notify page change
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public void PagerPageChanged(int page) {
            CommonHelper.Spinner = "loader-big";
            StateHasChanged();
            _discovererList = CommonHelper.UpdatePage(RegistryHelper.GetDiscovererListAsync, page, _discovererList, ref _pagedDiscovererList, CommonHelper.PageLengthSmall);
            NavigationManager.NavigateTo(NavigationManager.BaseUri + "discoverers/" + page);
            CommonHelper.Spinner = string.Empty;
            StateHasChanged();
        }

        /// <summary>
        /// OnInitialized
        /// </summary>
        protected override Task OnInitializedAsync() {
            CommonHelper.Spinner = "loader-big";
            return base.OnInitializedAsync();
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                _discovererList = await RegistryHelper.GetDiscovererListAsync();
                Page = "1";
                _pagedDiscovererList = _discovererList.GetPaged(Int32.Parse(Page), CommonHelper.PageLengthSmall, _discovererList.Error);
                CommonHelper.Spinner = string.Empty;
                CommonHelper.CheckErrorOrEmpty<DiscovererInfo>(_pagedDiscovererList, ref _tableView, ref _tableEmpty);
                StateHasChanged();

                _discovererEvent = await RegistryServiceEvents.SubscribeDiscovererEventsAsync(
                    ev => InvokeAsync(() => DiscovererEvent(ev)));
            }
        }

        /// <summary>
        /// Enable discoverer scan
        /// </summary>
        /// <param name="discoverer"></param>
        private async Task SetScanAsync(DiscovererInfo discoverer, bool checkStatus) {
            try {
                discoverer.ScanStatus = checkStatus;
                _eventResult = string.Empty;

                if (discoverer.ScanStatus == true) {
                    if (!_isDiscoveryEventSubscribed) {
                        _discovery = await RegistryServiceEvents.SubscribeDiscoveryProgressByDiscovererIdAsync(
                            discoverer.DiscovererModel.Id, async data => {
                                await InvokeAsync(() => ScanProgress(data));
                            });
                    }

                    _isDiscoveryEventSubscribed = true;
                    discoverer.IsSearching = true;
                    _scanResult = "displayBlock";
                    DiscovererData = discoverer;
                }
                else {
                    discoverer.IsSearching = false;
                    _scanResult = "displayNone";
                    if (_discovery != null) {
                        await _discovery.DisposeAsync();
                    }
                    _isDiscoveryEventSubscribed = false;
                }
                Status = await RegistryHelper.SetDiscoveryAsync(discoverer);
            }
            catch {
                if (_discovery != null) {
                    await _discovery.DisposeAsync();
                }
                _isDiscoveryEventSubscribed = false;
            }
        }

        /// <summary>
        /// Start ad-hoc scan
        /// </summary>
        /// <param name="discoverer"></param>
        private async Task SetAdHocScanAsync(DiscovererInfo discoverer) {
            if (!_isDiscoveryEventSubscribed) {
                discoverer.DiscoveryRequestId = Guid.NewGuid().ToString();
                _discovery = await RegistryServiceEvents.SubscribeDiscoveryProgressByRequestIdAsync(
                discoverer.DiscoveryRequestId, async data => {
                    await InvokeAsync(() => ScanProgress(data));
                });
                _isDiscoveryEventSubscribed = true;
            }

            try {
                _eventResult = string.Empty;

                discoverer.IsSearching = true;
                _scanResult = "displayBlock";
                DiscovererData = discoverer;
                Status = await RegistryHelper.DiscoverServersAsync(discoverer);
            }
            catch {
                if (_discovery != null) {
                    await _discovery.DisposeAsync();
                }
                _isDiscoveryEventSubscribed = false;
            }
        }

        /// <summary>
        /// Open then Drawer
        /// </summary>
        /// <param name="OpenDrawer"></param>
        private void OpenDrawer(DiscovererInfo discoverer) {
            IsOpened = true;
            DiscovererData = discoverer;
        }

        /// <summary>
        /// Close the Drawer
        /// </summary>
        private void CloseDrawer() {
            IsOpened = false;
            this.StateHasChanged();
        }

        /// <summary>
        /// display discoverers scan events
        /// </summary>
        /// <param name="ev"></param>
        private void ScanProgress(DiscoveryProgressApiModel ev) {
            var ts = ev.TimeStamp.ToLocalTime();
            switch (ev.EventType) {
                case DiscoveryProgressType.Pending:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Total} waiting..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.Started:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: Started." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanStarted:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: Scanning network..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanResult:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} addresses found - NEW: {ev.Result}..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanProgress:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} addresses found" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.NetworkScanFinished:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} addresses found - complete!" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanStarted:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: Scanning ports..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanResult:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} ports found - NEW: {ev.Result}" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanProgress:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} ports found" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.PortScanFinished:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: {ev.Discovered} ports found - complete!" + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.ServerDiscoveryStarted:
                    _eventResult += "==========================================" + System.Environment.NewLine;
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: Finding servers..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryStarted:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: ... {ev.Discovered} servers found - find endpoints on {ev.RequestDetails["url"]}..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.EndpointsDiscoveryFinished:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: ... {ev.Discovered} servers found - {ev.Result} endpoints found on {ev.RequestDetails["url"]}..." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.ServerDiscoveryFinished:
                    _eventResult += $"[{ts}] {ev.DiscovererId}: {ev.Progress}/{ev.Total}: ... {ev.Discovered} servers found." + System.Environment.NewLine;
                    break;
                case DiscoveryProgressType.Cancelled:
                    _eventResult += "==========================================" + System.Environment.NewLine;
                    _eventResult += $"[{ts}] {ev.DiscovererId}: Cancelled." + System.Environment.NewLine;
                    if (DiscovererData != null) {
                        DiscovererData.IsSearching = false;
                    }
                    break;
                case DiscoveryProgressType.Error:
                    _eventResult += "==========================================" + System.Environment.NewLine;
                    _eventResult += $"[{ts}] {ev.DiscovererId}: Failure." + System.Environment.NewLine;
                    if (DiscovererData != null) {
                        DiscovererData.IsSearching = false;
                    }
                    break;
                case DiscoveryProgressType.Finished:
                    _eventResult += "==========================================" + System.Environment.NewLine;
                    _eventResult += $"[{ts}] {ev.DiscovererId}: Completed." + System.Environment.NewLine;
                    if (DiscovererData != null) {
                        DiscovererData.IsSearching = false;
                    }
                    break;
            }
            StateHasChanged();
        }

        /// <summary>
        /// ClickHandler
        /// </summary>
        async Task ClickHandler(DiscovererInfo discoverer) {
            CloseDrawer();
            if (discoverer.isAdHocDiscovery) {
                await SetAdHocScanAsync(discoverer);
            }
            else {
                await this.OnAfterRenderAsync(true);
            }
        }

        /// <summary>
        /// refresh UI on DiscovererEvent
        /// </summary>
        /// <param name="ev"></param>
        private Task DiscovererEvent(DiscovererEventApiModel ev) {
            _discovererList.Results.Update(ev);
            _pagedDiscovererList = _discovererList.GetPaged(Int32.Parse(Page), CommonHelper.PageLength, _discovererList.Error);
            StateHasChanged();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Close the scan result view
        /// </summary>
        public void CloseScanResultView() {
            _scanResult = "displayNone";
        }

        public async void Dispose() {
            if (_discovererEvent != null) {
                await _discovererEvent.DisposeAsync();
            }

            if (_discovery != null) {
                await _discovery.DisposeAsync();
            }
        }
    }
}