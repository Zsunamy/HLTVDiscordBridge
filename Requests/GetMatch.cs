namespace HLTVDiscordBridge.Requests;

public class GetMatch : ApiRequestBody<GetMatch>
{
    protected override string Endpoint => "GetMatch";
    public int Id { get; set; }
}