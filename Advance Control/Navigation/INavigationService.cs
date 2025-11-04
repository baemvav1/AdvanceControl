using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advance_Control.Navigation
{
    public interface INavigationService
    {
        /// <summary>
        /// Navigate to a specific view type
        /// </summary>
        void NavigateTo(Type viewType);

        /// <summary>
        /// Navigate to a specific view type with parameter
        /// </summary>
        void NavigateTo(Type viewType, object? parameter);

        /// <summary>
        /// Navigate back to the previous view
        /// </summary>
        bool CanGoBack { get; }
        
        void GoBack();
    }
}
 