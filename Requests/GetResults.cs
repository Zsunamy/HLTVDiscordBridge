using System.Collections.Generic;

namespace HLTVDiscordBridge.Requests;

public class GetResults : ApiRequestBody<GetResults>
{
    protected override string Endpoint => "GetResults";
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public List<int> TeamIds { get; set; }
    public List<int> EventIds { get; set; }
}