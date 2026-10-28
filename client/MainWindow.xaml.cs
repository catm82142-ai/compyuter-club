using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ComputerClub.Client;

namespace ComputerClub.Client;

public partial class MainWindow : Window
{
    private readonly ServerClient _server;
    private readonly LockGuard _guard = new();
    private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    private string _qrUrl;
    private readonly bool _lockdown;
    private readonly Dictionary<string, string> _games;
    private DateTimeOffset? _endsAt;
    private bool _inSession;
    private string? _gameError;
    private bool _locked = true;

    public MainWindow(
        ServerClient server,
        string qrUrl,
        bool lockdown,
        Dictionary<string, string> games)
    {
        _server = server;
        _qrUrl = qrUrl;
        _lockdown = lockdown;
        _games = games;

        InitializeComponent();

        PcName.Text = server.PcId;
        Loaded += OnLoaded;
        KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape && !_lockdown)
            {
                Close();
            }
        };
        Closing += (_, e) =>
        {
            if (_inSession)
            {
                e.Cancel = true;
                return;
            }

            if (_lockdown)
            {
                RegistryLock.Revert();
            }
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        QrImage.Source = new BitmapImage(new Uri(_qrUrl));

        _pollTimer.Tick += async (_, _) => await PollAsync();
        _clockTimer.Tick += (_, _) => UpdateTimer();

        _pollTimer.Start();
        _clockTimer.Start();

        if (_lockdown)
        {
            RegistryLock.Apply();
        }

        EnterLockScreen();

        await _server.RegisterAsync();
        await PollAsync();
    }

    private async Task PollAsync()
    {
        var state = await _server.GetKioskStateAsync();

        if (state is null)
        {
            SetStatus(false, "Serverga ulanmadi — qayta urinilmoqda");
            return;
        }

        SetStatus(true, state.RuntimeStatus == "available"
            ? "Bo'sh — to'lov kutilmoqda"
            : "Band");

        await HandleCommandsAsync();

        ApplyLock(state.Locked);

        var session = state.ActiveSession;
        var shouldBeInSession = session is not null && session.Status == "active";

        if (shouldBeInSession && !_inSession)
        {
            BeginSession(session!);
        }
        else if (!shouldBeInSession && _inSession)
        {
            EndSession();
        }
    }

    private async Task HandleCommandsAsync()
    {
        var command = await _server.GetPendingCommandAsync();
        if (command is null)
        {
            return;
        }

        switch (command.Type)
        {
            case "lock":
            case "setLock":
                if (command.Payload is not null
                    && command.Payload.TryGetValue("locked", out var lockedValue))
                {
                    ApplyLock(lockedValue?.ToString() is "true");
                }

                break;

            case "stopSession":
                EndSession();
                break;

            case "startSession":
                if (command.Payload is null)
                {
                    break;
                }

                if (command.Payload.TryGetValue("endsAt", out var endsAtValue))
                {
                    _endsAt = DateTimeOffset.Parse(endsAtValue.ToString() ?? "");
                }

                if (command.Payload.TryGetValue("gameId", out var gameValue))
                {
                    StartGame(gameValue?.ToString());
                }

                break;
        }

        await _server.AckCommandAsync(command.CommandId);
    }

    private void StartGame(string? gameId)
    {
        _gameError = null;

        if (string.IsNullOrWhiteSpace(gameId))
        {
            return;
        }

        if (!_games.TryGetValue(gameId, out var path) || !File.Exists(path))
        {
            _gameError = "O'yin topilmadi";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception)
        {
            _gameError = "O'yinni ishga tushirib bo'lmadi";
        }
    }

    private void BeginSession(SessionInfo session)
    {
        _inSession = true;
        _endsAt = string.IsNullOrEmpty(session.EndsAt)
            ? null
            : DateTimeOffset.Parse(session.EndsAt);

        _guard.Stop();

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Width = 260;
        Height = 190;
        Topmost = true;
        Left = SystemParameters.VirtualScreenWidth - 300;
        Top = 24;

        LockView.Visibility = Visibility.Collapsed;
        SessionView.Visibility = Visibility.Visible;
        PhoneText.Text = session.UserPhone ?? "";

        if (_gameError is not null)
        {
            GameText.Text = _gameError;
            GameText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x48, 0x4D));
        }
        else if (session.GameName is not null)
        {
            GameText.Text = session.GameName;
            GameText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0xDE, 0x80));
        }
        else
        {
            GameText.Text = "";
        }

        UpdateTimer();
    }

    private void EndSession()
    {
        _inSession = false;
        _endsAt = null;
        _gameError = null;

        EnterLockScreen();
    }

    private void ApplyLock(bool locked)
    {
        if (locked == _locked)
        {
            return;
        }

        _locked = locked;

        if (locked)
        {
            if (_lockdown)
            {
                RegistryLock.Apply();
            }

            Show();
            WindowState = WindowState.Normal;
            Topmost = true;
            Activate();
        }
        else
        {
            if (_lockdown)
            {
                RegistryLock.Revert();
            }

            Topmost = false;
            Hide();
        }
    }

    private void EnterLockScreen()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Width = 1080;
        Height = 760;
        Left = 0;
        Top = 0;
        Topmost = true;

        SessionView.Visibility = Visibility.Collapsed;
        LockView.Visibility = Visibility.Visible;

        WindowState = WindowState.Maximized;

        if (_lockdown)
        {
            _guard.Start();
        }

        Activate();
        Focus();
    }

    private void UpdateTimer()
    {
        if (_endsAt is null)
        {
            TimerText.Text = "--:--";
            return;
        }

        var remaining = _endsAt.Value - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            EndSession();
            return;
        }

        TimerText.Text = remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
    }

    private void SetStatus(bool connected, string text)
    {
        StatusText.Text = text;
        StatusDot.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter
            .ConvertFromString(connected ? "#4ADE80" : "#E5484D"));
    }
}