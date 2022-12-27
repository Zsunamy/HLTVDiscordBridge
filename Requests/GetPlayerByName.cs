namespace HLTVDiscordBridge.Requests;

public class GetPlayerByName : ApiRequestBody
{
    protected override string Endpoint => "GetPlayerByName";
    public string Name { get; set; }

    public GetPlayerByName(string name)
    {
        Name = name;
    }
}