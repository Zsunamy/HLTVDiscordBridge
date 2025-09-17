namespace HLTVDiscordBridge.Shared;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Link => $"https://www.hltv.org/player/{Id}/{Name.Replace(' ', '-').ToLower()}";
}