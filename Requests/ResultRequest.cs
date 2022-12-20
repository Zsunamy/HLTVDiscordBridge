namespace HLTVDiscordBridge.Requests;

public class ResultRequest : ApiRequestBody
{
    public string StartDate;
    public string EndDate;

    public ResultRequest(string startDate, string endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}