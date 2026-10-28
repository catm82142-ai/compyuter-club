namespace ComputerClub.Client;

public class SessionInfo
{
    public string SessionId { get; set; } = "";
    public string PcId { get; set; } = "";
    public string? UserPhone { get; set; }
    public string? GameId { get; set; }
    public string? GameName { get; set; }
    public string Status { get; set; } = "";
    public int DurationMinutes { get; set; }
    public long Price { get; set; }
    public string? StartsAt { get; set; }
    public string? EndsAt { get; set; }
}

public class KioskState
{
    public string PcId { get; set; } = "";
    public string Name { get; set; } = "";
    public string RuntimeStatus { get; set; } = "offline";
    public bool Locked { get; set; } = true;
    public SessionInfo? ActiveSession { get; set; }
}

public class AgentCommand
{
    public string CommandId { get; set; } = "";
    public string Type { get; set; } = "";
    public Dictionary<string, object>? Payload { get; set; }
}

public class CommandList
{
    public AgentCommand? Pending { get; set; }
    public List<AgentCommand> History { get; set; } = new();
}