using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Prism.ViewModels;

namespace Prism.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var input = this.FindControl<TextBox>("InputBox");
        if (input is not null)
        {
            // Tunnel so we intercept Enter before the TextBox inserts a newline.
            input.AddHandler(InputElement.KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        }

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Messages.CollectionChanged += OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Follow new messages, and keep following while their text streams in.
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is MessageViewModel mvm)
                    mvm.PropertyChanged += OnMessagePropertyChanged;
            }
        }

        ScrollChatToEnd();
    }

    private void OnMessagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MessageViewModel.Content))
            ScrollChatToEnd();
    }

    private void ScrollChatToEnd()
    {
        // Defer to after the layout pass so the ScrollViewer's extent reflects
        // the newly added / grown content before we scroll. Background priority
        // runs after measure/arrange, so ScrollToEnd targets the real bottom.
        Dispatcher.UIThread.Post(() =>
        {
            var scroll = this.FindControl<ScrollViewer>("ChatScroll");
            scroll?.ScrollToEnd();
        }, DispatcherPriority.Background);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter sends; Shift+Enter inserts a newline.
        if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Shift) == 0)
        {
            e.Handled = true;
            if (DataContext is MainWindowViewModel vm && vm.SendCommand.CanExecute(null))
                vm.SendCommand.Execute(null);
        }
    }
}
