namespace HLTVDiscordBridge.Requests;

public class GetMatches : ApiRequestBody<GetMatches>
{
    protected override string Endpoint => "GetMatches";
}