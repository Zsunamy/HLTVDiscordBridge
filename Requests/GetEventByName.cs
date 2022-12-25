namespace HLTVDiscordBridge.Requests;

public class GetEventByName : ApiRequestBody
{
    public string Name { get; set; }

    public GetEventByName(string name)
    {
        Name = name;
    }
}