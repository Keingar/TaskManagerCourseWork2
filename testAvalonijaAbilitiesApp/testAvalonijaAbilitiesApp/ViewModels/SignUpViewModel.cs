﻿using testAvalonijaAbilitiesApp.ViewModels;
using ReactiveUI;
using taskManager.Services;
using System.Threading.Tasks;

namespace ToDoList.ViewModels
{
    public class SignUpViewModel : ViewModelBase
    {

        public SignUpViewModel()
        {
        }

        private string _usernameLogin = string.Empty;
        public string UsernameLogin
        {
            get => _usernameLogin;
            set => this.RaiseAndSetIfChanged(ref _usernameLogin, value);
        }

        private string _usernamePassword = string.Empty;
        public string UsernamePassword
        {
            get => _usernamePassword;
            set => this.RaiseAndSetIfChanged(ref _usernamePassword, value);
        }

        private string _usernameConfirmPassword = string.Empty;
        public string UserNameConfirmPassword
        {
            get => _usernameConfirmPassword;
            set => this.RaiseAndSetIfChanged(ref _usernameConfirmPassword, value);
        }

        private bool _showErrorMessage = false;
        public bool ShowErrorMessage
        {
            get => _showErrorMessage;
            set => this.RaiseAndSetIfChanged(ref _showErrorMessage, value);
        }

        private string _errorMessageText = string.Empty;
        public string ErrorMessageText
        {
            get => _errorMessageText;
            set => this.RaiseAndSetIfChanged(ref _errorMessageText, value);
        }

        public async Task<string> TrySignUp()
        {
            if (UsernamePassword != UserNameConfirmPassword)
            {
                ErrorMessageText = "Passwords do not match";
                ShowErrorMessage = true;
                return string.Empty;
            }

            int status = await SupabaseService.SignUpNewUser(UsernameLogin, UsernamePassword);

            if (status == -1)
            {
                ErrorMessageText = "User already exists";
                ShowErrorMessage = true;
                return string.Empty;
            }

            ErrorMessageText = string.Empty;
            ShowErrorMessage = false;

            return UsernameLogin;
        }



    }
}
