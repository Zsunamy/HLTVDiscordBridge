namespace HLTVDiscordBridge.Requests;

public class GetMatch : ApiRequestBody
{
    public int Id;
    protected override string Endpoint => "GetMatch";

    public GetMatch(int id)
    {
        Id = id;
    }
}