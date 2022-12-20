namespace HLTVDiscordBridge.Requests;

public class GetMatch : ApiRequestBody
{
    public int Id;

    public GetMatch(int id)
    {
        Id = id;
    }
}