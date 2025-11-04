using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.ViewModels
{
    public class CustomersViewModel : ViewModelBase
    {
        private ObservableCollection<CustomerDto> _customers;
        private bool _isLoading;

        public CustomersViewModel()
        {
            _customers = new ObservableCollection<CustomerDto>();
        }

        public ObservableCollection<CustomerDto> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
    }
}
