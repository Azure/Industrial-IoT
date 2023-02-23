// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.AspNetCore.Components.Routing;
    using System.Threading.Tasks;

    public partial class NavMenu {
        private bool _collapseNavMenu = true;

        private string NavMenuCssClass => _collapseNavMenu ? "collapse" : null;
        public UsernamePassword Credential { get; set; } = new UsernamePassword();
        private string SubMenuDisplay { get; set; } = "displayNone";
        private string SubMenuIcon { get; set; } = "oi-expand-down";

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                Credential = await GetSecureItemAsync<UsernamePassword>(CommonHelper.CredentialKey).ConfigureAwait(false);
                StateHasChanged();
            }
        }

        protected override void OnInitialized() {
            NavigationManager.LocationChanged += HandleLocationChangedAsync;
        }

        private async void HandleLocationChangedAsync(object sender, LocationChangedEventArgs e) {
            Credential = await GetSecureItemAsync<UsernamePassword>(CommonHelper.CredentialKey).ConfigureAwait(false);
            StateHasChanged();
        }

        public void Dispose() {
            NavigationManager.LocationChanged -= HandleLocationChangedAsync;
        }

        private void ToggleNavMenu() {
            _collapseNavMenu = !_collapseNavMenu;
        }

        private async Task<T> GetSecureItemAsync<T>(string key) {
            try {
                var serializedProtectedData = await sessionStorage.GetItemAsync<string>(key).ConfigureAwait(false);
                return secureData.UnprotectDeserialize<T>(serializedProtectedData);
            }
            catch {
                return default;
            }
        }

        private void SubMenu() {
            if (SubMenuDisplay == "displayNone") {
                SubMenuDisplay = "displayFlex";
                SubMenuIcon = "oi-collapse-up";
            }
            else {
                SubMenuDisplay = "displayNone";
                SubMenuIcon = "oi-expand-down";
            }
        }
    }
}
