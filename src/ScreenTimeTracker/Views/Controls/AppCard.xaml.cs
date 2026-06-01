using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenTimeTracker.Models;

namespace ScreenTimeTracker.Views.Controls;

public partial class AppCard : UserControl
{
    public AppCard()
    {
        InitializeComponent();
    }

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DailyUsageSummary summary)
        {
            var window = Window.GetWindow(this);
            if (window?.DataContext is ViewModels.ShellViewModel shell)
            {
                shell.NavigateToAppDetailCommand.Execute(new TrackedApp
                {
                    Id = summary.TrackedAppId,
                    AppName = summary.AppName,
                    ProcessName = summary.ProcessName,
                    IconBase64 = summary.IconBase64,
                    Category = summary.Category,
                });
            }
        }
    }
}
