using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cpu6502.Apple1.Avalonia.Controls;
using Cpu6502.Apple1.Avalonia.Terminal;
using Cpu6502.System.Apple1;

namespace Cpu6502.Apple1.Avalonia;

public sealed class MainWindow : Window
{
    private readonly AvaloniaTerminalLink _terminal = new();
    private readonly TerminalByteScreenSource _screenSource;
    private Apple1Host _host;
    private readonly Apple1ScreenPanel _screen = new();
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _status = new();
    private readonly ComboBox _profileComboBox = new();
    private bool _running = true;

    public MainWindow()
    {
        Title = "Apple-1";
        Width = 980;
        Height = 720;
        MinWidth = 720;
        MinHeight = 540;
        Background = new SolidColorBrush(Color.Parse("#020402"));

        // Setup profile dropdown
        _profileComboBox.ItemsSource = new[] { "WOZ Monitor", "BASIC" };
        _profileComboBox.SelectedIndex = 0;
        _profileComboBox.Width = 120;
        _profileComboBox.Height = 28;
        _profileComboBox.Foreground = new SolidColorBrush(Color.Parse("#7cff7a"));
        _profileComboBox.Background = new SolidColorBrush(Color.Parse("#071007"));
        _profileComboBox.BorderBrush = new SolidColorBrush(Color.Parse("#193b1f"));
        ToolTip.SetTip(_profileComboBox, "Select computer profile");
        _profileComboBox.SelectionChanged += OnProfileChanged;

        _host = CreateHost(Apple1Options.WozMonitor);
        _screenSource = new TerminalByteScreenSource(_terminal);
        _screen.Attach(_terminal, _screenSource);

        var reset = CreateToolbarButton("⟲");
        ToolTip.SetTip(reset, "Reset");
        reset.Click += (_, _) =>
        {
            _screen.Clear();
            _host.Reset();
            _running = true;
            _host.Run(2_000);
            UpdateStatus();
            RefocusScreen();
        };

        var runPause = CreateToolbarButton("⏸");
        ToolTip.SetTip(runPause, "Pause or run");
        runPause.Click += (_, _) =>
        {
            _running = !_running;
            runPause.Content = _running ? "⏸" : "▶";
            UpdateStatus();
            RefocusScreen();
        };

        var step = CreateToolbarButton("⏭");
        ToolTip.SetTip(step, "Step");
        step.Click += (_, _) =>
        {
            _host.Step();
            UpdateStatus();
            RefocusScreen();
        };

        var clear = CreateToolbarButton("⌫");
        ToolTip.SetTip(clear, "Clear screen");
        clear.Click += (_, _) =>
        {
            _screen.Clear();
            UpdateStatus();
            RefocusScreen();
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { _profileComboBox, reset, runPause, step, clear, _status }
        };
        _status.Foreground = new SolidColorBrush(Color.Parse("#7cff7a"));
        _status.VerticalAlignment = VerticalAlignment.Center;

        Content = new DockPanel
        {
            Background = new SolidColorBrush(Color.Parse("#020402")),
            LastChildFill = true,
            Children =
            {
                new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#020402")),
                    Padding = new global::Avalonia.Thickness(8, 4),
                    Child = toolbar
                },
                _screen
            }
        };
        DockPanel.SetDock((Control)((DockPanel)Content).Children[0], Dock.Top);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) =>
        {
            if (_running)
                _host.Run(2_000);
            UpdateStatus();
        };
        _timer.Start();
        _host.Run(2_000);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var snapshot = _host.Computer.GetCpuSnapshot();
        _status.Text = $"  {(_running ? "RUN" : "PAUSE")}  PC:${snapshot.ProgramCounter:X4}  SRC:{_screen.ScreenSourceName}  FOCUS:{(_screen.IsFocused ? "Y" : "N")}";
    }

    private void RefocusScreen() => Dispatcher.UIThread.Post(() => _screen.Focus());

    private void OnProfileChanged(object? sender, SelectionChangedEventArgs e)
    {
        string profileName = _profileComboBox.SelectedIndex == 0 ? "WOZ Monitor" : "BASIC";
        Debug.WriteLine($"[PROFILE] Switching to {profileName}");
        
        // Clear any pending terminal input before switching profiles
        _terminal.ClearInput();
        
        Debug.WriteLine("[PROFILE] Creating new host...");
        if (_profileComboBox.SelectedIndex == 0)
        {
            _host = CreateHost(Apple1Options.WozMonitor);
        }
        else
        {
            _host = CreateHost(Apple1Options.Basic);
        }
        
        Debug.WriteLine("[PROFILE] Resetting computer...");
        _host.Reset();
        _screen.Clear();
        
        // For BASIC profile, send the command to start BASIC after reset
        if (_profileComboBox.SelectedIndex == 1)
        {
            Debug.WriteLine("[PROFILE] Enqueuing 'E000R' command for BASIC...");
            _terminal.EnqueueText("E000R\r");
            Debug.WriteLine("[PROFILE] Running 5000 instructions to process input...");
            _host.Run(5_000);
            Debug.WriteLine("[PROFILE] Done processing input");
        }
        
        _running = true;
        UpdateStatus();
        Debug.WriteLine($"[PROFILE] Switched to {profileName}, running={_running}");
        RefocusScreen();
    }

    private Apple1Host CreateHost(Apple1Options options)
    {
        return new Apple1Host(_terminal, options);
    }

    private static Button CreateToolbarButton(string content)
    {
        return new Button
        {
            Content = content,
            Width = 32,
            Height = 28,
            Focusable = false,
            Background = new SolidColorBrush(Color.Parse("#071007")),
            Foreground = new SolidColorBrush(Color.Parse("#7cff7a")),
            BorderBrush = new SolidColorBrush(Color.Parse("#193b1f"))
        };
    }
}
