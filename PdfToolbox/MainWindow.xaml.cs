using System.Windows;
using System.Windows.Media.Animation;
using PdfToolbox.ViewModels;

namespace PdfToolbox;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (DataContext is MainViewModel vm)
            vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
        {
            if (FindResource("PageTransitionIn") is Storyboard sb)
                sb.Begin(PageContent);
        }
    }
}