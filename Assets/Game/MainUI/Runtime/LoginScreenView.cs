using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    public sealed class LoginScreenView : MonoBehaviour
    {
        [Header("Login")]
        [SerializeField] private TMP_InputField loginIdInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;

        [Header("Sign up")]
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button cancelSignUpButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusLabel;

        public event Action<string, string> LoginRequested;
        public event Action<string, string, string> SignUpRequested;

        public bool IsReady =>
            loginIdInput != null
            && passwordInput != null
            && loginButton != null
            && signUpPanel != null
            && nicknameInput != null
            && signUpButton != null
            && cancelSignUpButton != null
            && statusLabel != null;

        public bool IsSignUpVisible => signUpPanel != null && signUpPanel.activeSelf;
        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;

        public void Configure(
            TMP_InputField loginId,
            TMP_InputField password,
            Button login,
            GameObject signUp,
            TMP_InputField nickname,
            Button createAccount,
            Button cancelSignUp,
            TMP_Text status)
        {
            loginIdInput = loginId;
            passwordInput = password;
            loginButton = login;
            signUpPanel = signUp;
            nicknameInput = nickname;
            signUpButton = createAccount;
            cancelSignUpButton = cancelSignUp;
            statusLabel = status;
        }

        private void Awake()
        {
            SetSignUpVisible(false);
            ShowStatus("Enter your account ID and password.", false);
        }

        private void OnEnable()
        {
            loginButton?.onClick.AddListener(RaiseLoginRequested);
            signUpButton?.onClick.AddListener(RaiseSignUpRequested);
            cancelSignUpButton?.onClick.AddListener(CancelSignUp);
        }

        private void OnDisable()
        {
            loginButton?.onClick.RemoveListener(RaiseLoginRequested);
            signUpButton?.onClick.RemoveListener(RaiseSignUpRequested);
            cancelSignUpButton?.onClick.RemoveListener(CancelSignUp);
        }

        public void ShowAccountNotFound()
        {
            SetSignUpVisible(true);
            ShowStatus("Account not found. Choose a nickname to create it.", true);
            nicknameInput?.ActivateInputField();
        }

        public void ShowWrongPassword()
        {
            SetSignUpVisible(false);
            ShowStatus("The password is incorrect.", true);
            passwordInput?.ActivateInputField();
        }

        public void ShowLoginIdTaken()
        {
            SetSignUpVisible(true);
            ShowStatus("That account ID is already in use.", true);
        }

        public void ShowValidationError(string message)
        {
            ShowStatus(message, true);
        }

        public void ShowSuccess(string message)
        {
            SetSignUpVisible(false);
            ShowStatus(message, false);
        }

        public void SetInputEnabled(bool enabled)
        {
            if (loginIdInput != null)
                loginIdInput.interactable = enabled;
            if (passwordInput != null)
                passwordInput.interactable = enabled;
            if (nicknameInput != null)
                nicknameInput.interactable = enabled;
            if (loginButton != null)
                loginButton.interactable = enabled;
            if (signUpButton != null)
                signUpButton.interactable = enabled;
            if (cancelSignUpButton != null)
                cancelSignUpButton.interactable = enabled;
        }

        public void SetSignUpVisible(bool visible)
        {
            signUpPanel?.SetActive(visible);
        }

        private void RaiseLoginRequested()
        {
            LoginRequested?.Invoke(
                loginIdInput != null ? loginIdInput.text.Trim() : string.Empty,
                passwordInput != null ? passwordInput.text : string.Empty);
        }

        private void RaiseSignUpRequested()
        {
            SignUpRequested?.Invoke(
                loginIdInput != null ? loginIdInput.text.Trim() : string.Empty,
                passwordInput != null ? passwordInput.text : string.Empty,
                nicknameInput != null ? nicknameInput.text.Trim() : string.Empty);
        }

        private void CancelSignUp()
        {
            SetSignUpVisible(false);
            ShowStatus("Enter your account ID and password.", false);
        }

        private void ShowStatus(string message, bool error)
        {
            if (statusLabel == null)
                return;
            statusLabel.text = message;
            statusLabel.color = error
                ? new Color32(255, 174, 174, 255)
                : new Color32(239, 234, 220, 255);
        }
    }
}
