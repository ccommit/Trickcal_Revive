namespace TrickcalRevive.Infra
{
    /// <summary>
    /// 플레이어 저장 데이터는 전부 "지금 로그인한 계정" 아래에 있다. 계정을 세션에서
    /// 꺼내는 이 공통 부분만 나눠 갖고, 무엇을 어떻게 저장할지는 각 Repository가 따로 맡는다.
    /// </summary>
    public abstract class PlayerScopedRepository
    {
        private readonly ISessionService sessionService;

        protected PlayerScopedRepository(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }

        /// <summary>로그인 세션이 없으면 null이다. 호출부는 이 경우를 빈 값으로 처리한다.</summary>
        protected string CurrentAccountId => sessionService.GetSession();
    }
}
