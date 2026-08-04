using System.Collections.Generic;
using UnityEngine;

using TrickcalRevive.Data.Account;
using TrickcalRevive.Data.Currency;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Inventory;

namespace TrickcalRevive.Presentation
{
    // OpenAdventure()(스테이지 진입)는 03_파티편성 이슈에서 채운다.
    public class LobbyController : MonoBehaviour
    {
        private IPopupService popups;
        [SerializeField] private SettingsController settingsController;
        private IAccountRepository accountRepository;
        private ICurrencyRepository currencyRepository;

        public void Configure(IAccountRepository accounts, ICurrencyRepository currencies, IPopupService popups)
        {
            accountRepository = accounts;
            currencyRepository = currencies;
            this.popups = popups;
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
    }
}
