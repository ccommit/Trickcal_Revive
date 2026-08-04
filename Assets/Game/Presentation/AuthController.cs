using UnityEngine;

using TrickcalRevive.Domain.Account;
using TrickcalRevive.Infra;

namespace TrickcalRevive.Presentation
{
    public class AuthController : MonoBehaviour
    {
        private IAccountAuthRepository accountAuthRepository;
        private ISessionService sessionService;

        public void Configure(IAccountAuthRepository authRepository, ISessionService session)
        {
            accountAuthRepository = authRepository;
            sessionService = session;
        }

        public AuthResult Login(string loginId, string password)
        {
            if (!accountAuthRepository.IsLoginIdTaken(loginId))
                return AuthResult.NotFound;
            if (!accountAuthRepository.VerifyPassword(loginId, password))
                return AuthResult.WrongPassword;

            sessionService.SaveSession(loginId);
            return AuthResult.Success;
        }

        public AuthResult SignUp(string loginId, string password, string nickname)
        {
            if (accountAuthRepository.IsLoginIdTaken(loginId))
                return AuthResult.LoginIdTaken;

            accountAuthRepository.CreateAccount(loginId, password, nickname);
            sessionService.SaveSession(loginId);
            return AuthResult.Success;
        }
    }
}
