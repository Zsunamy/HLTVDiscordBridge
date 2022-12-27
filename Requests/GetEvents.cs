namespace HLTVDiscordBridge.Requests;

public class GetEvents : ApiRequestBody<GetEvents>
{
    protected override string Endpoint => "GetEvents";
}