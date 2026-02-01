using System.Windows;
using MacroRecorder.ViewModels;

namespace MacroRecorder
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        
        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            Closing += (s, e) =>
            {
                _viewModel.Dispose();
            };
        }
    }
}
