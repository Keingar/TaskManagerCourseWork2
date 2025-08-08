using testAvalonijaAbilitiesApp.ViewModels;
using ReactiveUI;
using Supabase.Gotrue; // Ensure correct import for User
using taskManager.Services;
using System.Threading.Tasks;

namespace ToDoList.ViewModels
{
    public class SignInViewModel : ViewModelBase
    {

        public SignInViewModel()
        {

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

        public async Task<User?> TryLogin()
        {
            User? user = await SupabaseService.TryLogin(UsernameLogin, UsernamePassword);

            if (user != null)
            {
                return user; // Login successful, return the User object
            }
            else
            {
                ErrorMessageText = "Invalid username or password";
                ShowErrorMessage = true;
                return null; // Login failed
            }
        }



    }
}
