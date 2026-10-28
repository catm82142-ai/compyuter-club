using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace ComputerClub.Client;

public class ServerClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly string _pcId;

    public string PcId => _pcId;
    public bool Connected { get; private set; }
    public string? LastError { get; private set; }

    public ServerClient(string baseUrl, string pcId)
    {
        _pcId = pcId;
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(5),
        };
    }

    public async Task<bool> RegisterAsync()
    {
        return await TryAsync(async () =>
        {
            using var response = await _http.PostAsJsonAsync("/agent/register", new { pcId = _pcId });
            response.EnsureSuccessStatusCode();
        });
    }

    public async Task<KioskState?> GetKioskStateAsync()
    {
        KioskState? state = null;

        var ok = await TryAsync(async () =>
        {
            using var response = await _http.GetAsync($"/api/kiosk/{_pcId}");
            response.EnsureSuccessStatusCode();
            state = await response.Content.ReadFromJsonAsync<KioskState>(Json);
        });

        return ok ? state : null;
    }

    public async Task<AgentCommand?> GetPendingCommandAsync()
    {
        AgentCommand? command = null;

        var ok = await TryAsync(async () =>
        {
            using var response = await _http.GetAsync($"/agent/commands/{_pcId}");
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<CommandList>(Json);
            command = list?.Pending;
        });

        return ok ? command : null;
    }

    public async Task AckCommandAsync(string commandId)
    {
        await TryAsync(async () =>
        {
            using var response = await _http.PostAsJsonAsync(
                $"/agent/commands/{commandId}/ack",
                new { }
            );
            response.EnsureSuccessStatusCode();
        });
    }

    private async Task<bool> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            Connected = true;
            LastError = null;
            return true;
        }
        catch (Exception error)
        {
            Connected = false;
            LastError = error.Message;
            return false;
        }
    }
}