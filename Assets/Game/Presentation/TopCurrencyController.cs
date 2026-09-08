using System;
using System.Collections.Generic;
using UnityEngine;

using TrickcalRevive.Data.Currency;
using TrickcalRevive.Domain.Account;
using TrickcalRevive.Domain.Inventory;
using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    /// <summary>
    /// 상단 통화 패널의 값과 화면별 표시 조합을 한곳에서 관리한다.
    /// 각 화면 Controller는 통화 UI나 저장소를 알 필요가 없다.
    /// </summary>
    public sealed class TopCurrencyController : MonoBehaviour
    {
        private ICurrencyRepository currencyRepository;
        private IAccountRepository accountRepository;
        private TopCurrencyPanelView view;
        private Action openMenu;
        private Action returnHome;
        private Action goBack;

        public void Configure(
            ICurrencyRepository currencies,
            IAccountRepository accounts,
            Action openMenuAction,
            Action returnHomeAction,
            Action goBackAction)
        {
            currencyRepository = currencies;
            accountRepository = accounts;
            openMenu = openMenuAction;
            returnHome = returnHomeAction;
            goBack = goBackAction;
        }

        public void AttachView(TopCurrencyPanelView currencyView)
        {
            DetachView();
            view = currencyView;
            if (view != null)
            {
                view.MenuRequested += HandleMenuRequested;
                view.HomeRequested += HandleHomeRequested;
                view.BackRequested += HandleBackRequested;
            }
            Refresh();
        }

        public void Show(TopCurrencyVisibility visibleCurrencies, bool isLobby, string pageTitle)
        {
            view?.Show(visibleCurrencies, isLobby, pageTitle);
            Refresh();
        }

        public void Refresh()
        {
            if (view == null || currencyRepository == null)
                return;

            var currencies = currencyRepository.GetCurrencies();
            var account = accountRepository?.GetAccount();
            view.RenderProfile(
                account?.Nickname,
                account?.PlayerLevel ?? 1,
                account?.Exp ?? 0,
                0);
            view.Render(
                AmountFor(currencies, "Stamina"),
                account?.StaminaMax ?? 0,
                AmountFor(currencies, "Gold"),
                AmountFor(currencies, "Elleaf"),
                AmountFor(currencies, "Macaron"));
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

        private void OnDestroy()
        {
            DetachView();
        }

        private void DetachView()
        {
            if (view == null)
                return;
            view.MenuRequested -= HandleMenuRequested;
            view.HomeRequested -= HandleHomeRequested;
            view.BackRequested -= HandleBackRequested;
            view = null;
        }

        private void HandleMenuRequested()
        {
            openMenu?.Invoke();
        }

        private void HandleHomeRequested()
        {
            returnHome?.Invoke();
        }

        private void HandleBackRequested()
        {
            goBack?.Invoke();
        }
    }
}
