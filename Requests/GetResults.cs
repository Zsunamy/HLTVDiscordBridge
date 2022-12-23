using System.Collections.Generic;

namespace HLTVDiscordBridge.Requests;

public class GetResults : ApiRequestBody
{
    public string StartDate;
    public string EndDate;
    public List<int> TeamIds;
    public List<int> EventIds;
    public GetResults(string startDate = null, string endDate = null, List<int> teamIds = null, List<int> eventIds = null)
    {
        StartDate = startDate;
        EndDate = endDate;
        TeamIds = teamIds;
        EventIds = eventIds;
    }
}