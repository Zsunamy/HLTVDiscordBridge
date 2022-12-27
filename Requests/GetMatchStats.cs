namespace HLTVDiscordBridge.Requests;

public class GetMatchStats : ApiRequestBody<GetMatchStats>
{
    protected override string Endpoint => "GetMatchStats";
    public int Id { get; set; }
}