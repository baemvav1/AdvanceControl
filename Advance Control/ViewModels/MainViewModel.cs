using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _title = "Advance Control";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}
