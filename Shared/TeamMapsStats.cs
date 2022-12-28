using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HLTVDiscordBridge.Shared;

public class TeamMapsStats
{
    [JsonPropertyName("de_dust2")]
    public TeamMapStats DeDust2 { get; set; }
    
    [JsonPropertyName("de_mirage")]
    public TeamMapStats DeMirage { get; set; }
    
    [JsonPropertyName("de_inferno")]
    public TeamMapStats DeInferno { get; set; }
    
    [JsonPropertyName("de_nuke")]
    public TeamMapStats DeNuke { get; set; }
    
    [JsonPropertyName("de_overpass")]
    public TeamMapStats DeOverpass { get; set; }
    
    [JsonPropertyName("de_train")]
    public TeamMapStats DeTrain { get; set; }
    
    [JsonPropertyName("de_cache")]
    public TeamMapStats DeCache { get; set; }
    
    [JsonPropertyName("de_cbble")]
    public TeamMapStats DeCbble { get; set; }
    
    [JsonPropertyName("de_ancient")]
    public TeamMapStats DeAncient { get; set; }
    
    [JsonPropertyName("de_tuscan")]
    public TeamMapStats DeTuscan { get; set; }

    public IEnumerable<(string, TeamMapStats)> GetMostPlayedMaps()
    {
        return new[] { ("de_dust2", DeDust2), ("de_mirage", DeMirage), ("de_inferno", DeInferno),
            ("de_nuke", DeNuke), ("de_overpass", DeOverpass), ("de_train", DeTrain), ("de_cache", DeCache), ("de_cbble", DeCbble),
            ("de_ancient", DeAncient), ("de_tuscan", DeTuscan)}.Where(elem => elem.Item2 != null)
            .OrderByDescending(elem => elem.Item2.Wins);
    }
}