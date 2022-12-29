using System.Collections.Generic;

namespace HLTVDiscordBridge.Requests;

public class GetResults : ApiRequestBody<GetResults>
{
    protected override string Endpoint { get; } = "getResults";
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public int[] TeamIds { get; set; }
    public int[] EventIds { get; set; }
}