using System.Windows;
using LiveTranslator.ViewModels;

namespace LiveTranslator.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += () => this.Close();
        }
    }
}
