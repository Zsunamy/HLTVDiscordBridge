namespace HLTVDiscordBridge.Requests;

public class GetTeamRanking : ApiRequestBody<GetTeamRanking>
{
    protected override string Endpoint { get; } = "getTeamRanking";
    public int? Year { get; set; }
    public string Month { get; set; }
    public int? Day { get; set; }
    public string Country { get; set; }
}