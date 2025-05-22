using System.Windows;
using System.Windows.Media.Animation;

namespace LibraryManagementSystem
{
    public partial class App : Application
    {
        public void StartStoryboard(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Storyboard storyboard)
            {
                storyboard.Begin();
            }
        }
    }
}