using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Navigation
{
    public interface INavigationService
    {
        void Initialize(Frame frame);
        void Configure(string tag, Type pageType);
        void Configure<TPage>(string tag) where TPage : Page;
        void ConfigureFactory(string tag, Func<object> factory);
        bool Navigate(string tag, object parameter = null);
        bool CanGoBack { get; }
        void GoBack();
        string GetCurrentTag();
        bool Reload();
    }
}
 