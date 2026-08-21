using UnityEngine;

using TrickcalRevive.Domain.Account;
using TrickcalRevive.Infra;
using TrickcalRevive.MainUI;

namespace TrickcalRevive.Presentation
{
    public class AuthController : MonoBehaviour
    {
        private IAccountAuthRepository accountAuthRepository;
        private ISessionService sessionService;
        private INavigationService navigationService;
        private LoginScreenView view;

        public void Configure(
            IAccountAuthRepository authRepository,
            ISessionService session,
            INavigationService navigation = null)
        {
            accountAuthRepository = authRepository;
            sessionService = session;
            navigationService = navigation;
        }

        public void AttachView(LoginScreenView loginView)
        {
            DetachView();
            view = loginView;
            if (view == null)
                return;

            view.LoginRequested += HandleLoginRequested;
            view.SignUpRequested += HandleSignUpRequested;
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

        private void OnDestroy()
        {
            DetachView();
        }

        private void DetachView()
        {
            if (view == null)
                return;

            view.LoginRequested -= HandleLoginRequested;
            view.SignUpRequested -= HandleSignUpRequested;
            view = null;
        }

        private void HandleLoginRequested(string loginId, string password)
        {
            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrEmpty(password))
            {
                view?.ShowValidationError("Enter both the account ID and password.");
                return;
            }

            switch (Login(loginId, password))
            {
                case AuthResult.Success:
                    CompleteAuthentication();
                    break;
                case AuthResult.NotFound:
                    view?.ShowAccountNotFound();
                    break;
                case AuthResult.WrongPassword:
                    view?.ShowWrongPassword();
                    break;
                default:
                    view?.ShowValidationError("Login could not be completed.");
                    break;
            }
        }

        private void HandleSignUpRequested(string loginId, string password, string nickname)
        {
            if (string.IsNullOrWhiteSpace(loginId)
                || string.IsNullOrEmpty(password)
                || string.IsNullOrWhiteSpace(nickname))
            {
                view?.ShowValidationError("Enter an account ID, password, and nickname.");
                return;
            }

            switch (SignUp(loginId, password, nickname))
            {
                case AuthResult.Success:
                    CompleteAuthentication();
                    break;
                case AuthResult.LoginIdTaken:
                    view?.ShowLoginIdTaken();
                    break;
                default:
                    view?.ShowValidationError("Account creation could not be completed.");
                    break;
            }
        }

        private void CompleteAuthentication()
        {
            if (navigationService == null)
            {
                view?.ShowValidationError("Navigation service is unavailable.");
                return;
            }

            view?.SetInputEnabled(false);
            view?.ShowSuccess("Login complete. Opening the lobby...");
            navigationService.Go(SceneIds.Main);
        }
    }
}
