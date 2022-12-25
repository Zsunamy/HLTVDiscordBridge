namespace HLTVDiscordBridge.Requests;

public class GetEventByName : ApiRequestBody
{
    public string Name { get; set; }
    protected override string Endpoint => "GetEventByName";

    public GetEventByName(string name)
    {
        Name = name;
    }
}