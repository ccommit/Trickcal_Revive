using System.Collections.Generic;
using UnityEngine;

using TrickcalRevive.Data.Account;
using TrickcalRevive.Data.Currency;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    // OpenAdventure()(스테이지 진입)는 03_파티편성 이슈에서 채운다.
    public class LobbyController : MonoBehaviour
    {
        private IPopupService popups;
        [SerializeField] private SettingsController settingsController;
        private IAccountRepository accountRepository;
        private ICurrencyRepository currencyRepository;
        private INavigationService navigationService;
        private LobbyScreenView view;

        public void Configure(
            IAccountRepository accounts,
            ICurrencyRepository currencies,
            IPopupService popups,
            INavigationService navigation = null)
        {
            accountRepository = accounts;
            currencyRepository = currencies;
            this.popups = popups;
            navigationService = navigation;
        }

        public void AttachView(LobbyScreenView lobbyView)
        {
            DetachView();
            view = lobbyView;
            if (view == null)
                return;

            view.SettingsRequested += HandleSettingsRequested;
            view.LogoutRequested += HandleLogoutRequested;
            RefreshView();
        }

        public AccountData RenderProfile()
        {
            return accountRepository.GetAccount();
        }

        public List<PlayerCurrencyData> RenderCurrencies()
        {
            return currencyRepository.GetCurrencies();
        }

        public SettingsController OpenSettings()
        {
            popups.Open("Settings");
            return settingsController;
        }

        public void RefreshView()
        {
            if (view == null)
                return;

            var account = RenderProfile();
            if (account == null)
            {
                view.ShowUnavailable("No active account was found. Return to Login.");
                return;
            }

            view.RenderProfile(account.Nickname, account.PlayerLevel, account.Exp, 0);

            var currencies = RenderCurrencies();
            view.RenderCurrencies(
                AmountFor(currencies, "Gold"),
                AmountFor(currencies, "Elleaf"),
                AmountFor(currencies, "Macaron"),
                (int)AmountFor(currencies, "Stamina"),
                account.StaminaMax);
        }

        private void OnDestroy()
        {
            DetachView();
        }

        private void DetachView()
        {
            if (view == null)
                return;

            view.SettingsRequested -= HandleSettingsRequested;
            view.LogoutRequested -= HandleLogoutRequested;
            view = null;
        }

        private void HandleSettingsRequested()
        {
            OpenSettings();
            view?.SetSettingsOpen(true);
        }

        private void HandleLogoutRequested()
        {
            settingsController.Logout();
            view?.SetSettingsOpen(false);
            navigationService?.Go(SceneIds.Login);
        }

        private static long AmountFor(List<PlayerCurrencyData> currencies, string currencyType)
        {
            if (currencies == null)
                return 0;

            foreach (var currency in currencies)
            {
                if (currency != null && currency.CurrencyType == currencyType)
                    return currency.Amount;
            }

            return 0;
        }
    }
}
