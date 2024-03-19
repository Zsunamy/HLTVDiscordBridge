namespace HLTVDiscordBridge.Requests;

public class GetRssNews : ApiRequestBody<GetRssNews>
{
    protected override string Endpoint { get; } = "getRssNews";
}