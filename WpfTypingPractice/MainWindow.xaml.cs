using System.Windows;
using WpfTypingPractice.Helpers;
using WpfTypingPractice.ViewModels;

namespace WpfTypingPractice
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel();
        }
    }
}
