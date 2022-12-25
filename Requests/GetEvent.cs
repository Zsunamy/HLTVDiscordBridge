namespace HLTVDiscordBridge.Requests;

public class GetEvent : ApiRequestBody
{
    public int Id { get; set; }

    public GetEvent(int id)
    {
        Id = id;
    }
}