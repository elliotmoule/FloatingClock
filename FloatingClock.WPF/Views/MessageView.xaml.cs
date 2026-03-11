using FloatingClock.WPF.ViewModels;
using System.Windows;

namespace FloatingClock.WPF.Views;

/// <summary>
/// Interaction logic for MessageView.xaml
/// </summary>
public partial class MessageView : Window
{
    private readonly MessageViewModel _messageViewModel;
    public MessageView(MessageViewModel viewModel)
    {
        InitializeComponent();
        _messageViewModel = viewModel;
        DataContext = _messageViewModel;

        viewModel.OnOK += () => { DialogResult = true; Close(); };
        viewModel.OnCancel += () => { DialogResult = false; Close(); };

        cmbHours.SelectedIndex = 0;
        cmbMinutes.SelectedIndex = 0;
        cmbSeconds.SelectedIndex = 0;

        btnCancel.Visibility = viewModel.IsInputMessage ? Visibility.Visible : Visibility.Collapsed;
    }

    public bool? ShowMessage()
    {
        _messageViewModel.Result = ShowDialog();

        return _messageViewModel.Result;
    }
}
