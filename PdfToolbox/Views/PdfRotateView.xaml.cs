using System.Windows.Controls;
using PdfToolbox.ViewModels;

namespace PdfToolbox.Views;

public partial class PdfRotateView : UserControl
{
    public PdfRotateView()
    {
        InitializeComponent();
        DataContext = new PdfRotateViewModel();
    }
}
