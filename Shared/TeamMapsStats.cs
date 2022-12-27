using System.Collections.Generic;
using System.Linq;

namespace HLTVDiscordBridge.Shared;

public class TeamMapsStats
{
    public TeamMapStats DeDust2 { get; set; }
    public TeamMapStats DeMirage { get; set; }
    public TeamMapStats DeInferno { get; set; }
    public TeamMapStats DeNuke { get; set; }
    public TeamMapStats DeOverpass { get; set; }
    public TeamMapStats DeTrain { get; set; }
    public TeamMapStats DeCache { get; set; }
    public TeamMapStats DeCbble { get; set; }
    public TeamMapStats DeAncient { get; set; }
    public TeamMapStats DeTuscan { get; set; }

    public IEnumerable<(string, TeamMapStats)> GetMostPlayedMaps()
    {
        return new[] { ("DeDust2", DeDust2), ("DeMirage", DeMirage), ("DeInferno", DeInferno),
            ("DeNuke", DeNuke), ("DeOverpass", DeOverpass), ("DeTrain", DeTrain), ("DeCache", DeCache), ("DeCbble", DeCbble),
            ("DeAncient", DeAncient), ("DeTuscan", DeTuscan)}.Where(elem => elem.Item2 != null)
            .OrderBy(elem => elem.Item2.Wins);
    }
}