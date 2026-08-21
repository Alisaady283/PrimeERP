using System;
using System.Windows.Controls;

namespace PrimeERP.Services
{
    public interface INavigationService
    {
        UserControl CurrentPage { get; }
        event EventHandler CurrentPageChanged;

        void RegisterPage(string key, Func<UserControl> factory);
        void NavigateTo(string key);
        bool CanNavigateTo(string key);
    }
}
