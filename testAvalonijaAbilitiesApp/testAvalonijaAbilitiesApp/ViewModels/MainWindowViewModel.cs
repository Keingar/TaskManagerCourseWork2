using ToDoList.ViewModels;
using ReactiveUI;
using testAvalonijaAbilitiesApp.DataModel;
using System.Reactive;
using Supabase;
using System;
using System.Threading.Tasks;
using Supabase.Gotrue;
using taskManager.Services;
using ToDoList.DataModel;

namespace testAvalonijaAbilitiesApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private ViewModelBase _contentViewModel;

        //  private ToDoListService toDoListService = new ToDoListService();
        public ReactiveCommand<Unit, Unit> TryLoginCommand { get; }

        public MainWindowViewModel()
        {
            SupabaseService.InitializeSupabase();

            TryLoginCommand = ReactiveCommand.CreateFromTask(TryLogin); // Use CreateFromTask for async methods
            _contentViewModel = new SignInViewModel();
        }

        public ViewModelBase ContentViewModel
        {
            get => _contentViewModel;
            private set => this.RaiseAndSetIfChanged(ref _contentViewModel, value);
        }

        public void ChangeWindowToRegister()
        {
            SignUpViewModel RegisterViewModel = new();

            ContentViewModel = RegisterViewModel;
        }

        public void ChangeWindowToLogin()
        {
            SignInViewModel LoginViewModel = new();

            ContentViewModel = LoginViewModel;
        }

        public async Task TryLogin()
        {
            SignInViewModel signInViewModel = (SignInViewModel)ContentViewModel;

            User? user = await signInViewModel.TryLogin(); // Await the async call

            if (user == null)
            {
                return; // Not a successful login
            }

            ChnageWindowToDoList(); // Use user.Id to get user-specific data
        }



        public async Task TrySignUp()
        {
            SignUpViewModel signUpViewModel = (SignUpViewModel)ContentViewModel;

            string username = await signUpViewModel.TrySignUp();

            if (string.IsNullOrEmpty(username))
            {
                return; // Not a successful sign-up
            }

            // TODO: Do whatever comes after successful sign-up
            await ChnageWindowToDoList();
        }


        public async Task ChnageWindowToDoList()
        {
            ContentViewModel = new ToDoListViewModel(SupabaseService.CurrentUser);
            await ((ToDoListViewModel)ContentViewModel).InitializeAsync();
        }


    }
}

