namespace HLTVDiscordBridge.Requests;

public class GetPastEvents : ApiRequestBody<GetPastEvents>
{
    protected override string Endpoint => "GetPastEvents";
    public string StartDate { get; set; }
    public string EndDate { get; set; }
}