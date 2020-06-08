// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Shared {
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.AspNetCore.Components.Routing;

    public partial class NavMenu {
        bool collapseNavMenu = true;

        string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
        public CredentialModel Credential = new CredentialModel();
        private string _subMenuDisplay = "displayNone";
        private string _subMenuIcon = "oi-expand-down";

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                Credential = await GetSecureItemAsync<CredentialModel>(CommonHelper.CredentialKey);
                StateHasChanged();
            }
        }

        protected override void OnInitialized() {
            NavigationManager.LocationChanged += HandleLocationChanged;
        }

        private async void HandleLocationChanged(object sender, LocationChangedEventArgs e) {
            Credential = await GetSecureItemAsync<CredentialModel>(CommonHelper.CredentialKey);
            StateHasChanged();
        }

        public void Dispose() {
            NavigationManager.LocationChanged -= HandleLocationChanged;
        }

        void ToggleNavMenu() {
            collapseNavMenu = !collapseNavMenu;
        }

        private async Task<T> GetSecureItemAsync<T>(string key) {
            try {
                var serializedProtectedData = await sessionStorage.GetItemAsync<string>(key);
                return secureData.UnprotectDeserialize<T>(serializedProtectedData);
            }
            catch {
                return default;
            }
        }

        private void SubMenu() {
            if (_subMenuDisplay == "displayNone") {
                _subMenuDisplay = "displayFlex";
                _subMenuIcon = "oi-collapse-up";
            }
            else {
                _subMenuDisplay = "displayNone";
                _subMenuIcon = "oi-expand-down";
            }
        }
    }
}