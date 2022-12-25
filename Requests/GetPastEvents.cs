namespace HLTVDiscordBridge.Requests;

public class GetPastEvents : ApiRequestBody
{
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    protected override string Endpoint => "GetPastEvents";

    public GetPastEvents(string startDate, string endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}