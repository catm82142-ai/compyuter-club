using System.IO;
using System.Text.Json;
using System.Windows;

namespace ComputerClub.Client;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var config = LoadConfig();
        var pcId = config.PcId ?? Environment.MachineName.ToUpperInvariant();
        var serverUrl = config.ServerUrl ?? "http://localhost:4000";

        var server = new ServerClient(serverUrl, pcId);
        var qrUrl = $"{serverUrl.TrimEnd('/')}/api/qr?pcId={Uri.EscapeDataString(pcId)}";

        var app = new System.Windows.Application
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose,
        };

        app.DispatcherUnhandledException += (_, _) => { };

        app.Run(new MainWindow(server, qrUrl, config.Lockdown, config.Games));
    }

    private static ClientConfig LoadConfig()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "client.json");
            if (!File.Exists(path))
            {
                return new ClientConfig();
            }

            return JsonSerializer.Deserialize<ClientConfig>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new ClientConfig();
        }
        catch (Exception)
        {
            return new ClientConfig();
        }
    }
}

public class ClientConfig
{
    public string? ServerUrl { get; set; }
    public string? PcId { get; set; }
    public bool Lockdown { get; set; }
    public Dictionary<string, string> Games { get; set; } = new();
}