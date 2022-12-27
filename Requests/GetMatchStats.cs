namespace HLTVDiscordBridge.Requests;

public class GetMatchStats : ApiRequestBody
{
    protected override string Endpoint => "GetMatchStats";
    public int Id { get; set; }

    public GetMatchStats(int id)
    {
        Id = id;
    }
}