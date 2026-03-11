using FloatingClock.WPF.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace FloatingClock.WPF.ViewModels;

public class MessageViewModel : BaseViewModel
{
    public bool? Result { get; set; } = false;

    private string _caption = string.Empty;
    public string Caption
    {
        get { return _caption; }
        set { SetProperty(ref _caption, value); }
    }

    private string _message = string.Empty;
    public string Message
    {
        get { return _message; }
        set { SetProperty(ref _message, value); }
    }

    private bool _isInputMessage = false;
    public bool IsInputMessage
    {
        get { return _isInputMessage; }
        set
        {
            SetProperty(ref _isInputMessage, value);
            OnPropertyChanged(nameof(IsSimpleMessageVisible));
            OnPropertyChanged(nameof(IsInputMessageVisible));
        }
    }

    internal int MinimumNumber { get; set; } = 0;
    internal int MaximumNumber { get; set; } = 100; // Exclusive

    private ObservableCollection<int> _numbersList = [];
    public ObservableCollection<int> NumbersList
    {
        get { return _numbersList; }
        set { SetProperty(ref _numbersList, value); }
    }

    private string _selectedHour = "00";
    public string SelectedHour
    {
        get { return _selectedHour; }
        set { SetProperty(ref _selectedHour, value); }
    }

    private string _selectedMinute = "00";
    public string SelectedMinute
    {
        get { return _selectedMinute; }
        set { SetProperty(ref _selectedMinute, value); }
    }

    private string _selectedSeconds = "00";
    public string SelectedSeconds
    {
        get { return _selectedSeconds; }
        set { SetProperty(ref _selectedSeconds, value); }
    }

    public ICommand OnOKCommand { get; set; }
    public ICommand OnCancelCommand { get; set; }

    public event Action? OnOK;
    public event Action? OnCancel;

    public Visibility IsSimpleMessageVisible => IsInputMessage ? Visibility.Collapsed : Visibility.Visible;
    public Visibility IsInputMessageVisible => IsInputMessage ? Visibility.Visible : Visibility.Collapsed;

    public MessageViewModel(string caption, string message, bool isInput)
    {
        Caption = caption;
        Message = message;
        IsInputMessage = isInput;

        OnOKCommand = new RelayCommand(_ => OnOK?.Invoke());
        OnCancelCommand = new RelayCommand(_ => OnCancel?.Invoke());

        NumbersList = new ObservableCollection<int>(Enumerable.Range(MinimumNumber, MaximumNumber));
    }
}
