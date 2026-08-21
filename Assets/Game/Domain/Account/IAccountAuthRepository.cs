using TrickcalRevive.Data.Account;

namespace TrickcalRevive.Domain.Account
{
    // 로그인·가입·계정 삭제. 게임 진행 데이터와는 다른 관심사라 별도로 둔다.
    public interface IAccountAuthRepository
    {
        bool IsLoginIdTaken(string loginId);
        AccountData CreateAccount(string loginId, string password, string nickname);
        bool VerifyPassword(string loginId, string password);
        void DeleteAccount(string accountId);
    }
}
