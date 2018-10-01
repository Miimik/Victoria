using Newtonsoft.Json;
using Victoria.Objects.Enums;
using System.Collections.Generic;

namespace Victoria.Objects
{
    public sealed class LavaResult
    {
        [JsonProperty("loadType")] 
        public LoadResultType LoadResultType { get; internal set; }

        [JsonProperty("playlistInfo")] 
        public PlaylistInfo PlaylistInfo { get; internal set; }

        [JsonIgnore] 
        public IEnumerable<LavaTrack> Tracks { get; internal set; }
    }
}