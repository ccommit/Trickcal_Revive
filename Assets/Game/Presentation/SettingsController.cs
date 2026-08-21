using UnityEngine;

using TrickcalRevive.Infra;

namespace TrickcalRevive.Presentation
{
    // ResetAccount(계정 초기화)는 다음 이슈에서 채운다 — ISaveManager.DeleteAll이
    // 6개 세이브 핸들러를 다 요구하는데 지금은 AccountSaveHandler만 있다.
    public class SettingsController : MonoBehaviour
    {
        private ISessionService sessionService;

        public void Configure(ISessionService session)
        {
            sessionService = session;
        }

        public void Logout()
        {
            sessionService.ClearSession();
        }
    }
}
