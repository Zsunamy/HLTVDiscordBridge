namespace HLTVDiscordBridge.Requests;

public class GetEvent : ApiRequestBody
{
    public int Id { get; set; }

    protected override string Endpoint => "getEvent";

    public GetEvent(int id)
    {
        Id = id;
    }
}