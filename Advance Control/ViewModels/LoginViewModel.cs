using Advance_Control.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private ObservableCollection<LogInDto> _login;
        private bool _isLoading;

        public LoginViewModel()
        {
            _login = new ObservableCollection<LogInDto>();
        }

        public ObservableCollection<LogInDto> Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
    }
}
