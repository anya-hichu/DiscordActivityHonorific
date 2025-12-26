using Discord;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DiscordActivityHonorific.Configs;

[Serializable]
public class ActivityConfig
{
    public static readonly int DEFAULT_VERSION = 2;

    public static List<ActivityConfig> GetDefaults() => [
        new() {
            Name = $"Game (V{DEFAULT_VERSION})",
            TypeName = typeof(Game).Name,
            FilterTemplate = """
            {{ Activity.Name != "FINAL FANTASY XIV Online" && Activity.Name != "FINAL FANTASY XIV" && Activity.Name != "Custom Status" }}
            """,
            TitleTemplate = """
            {{- if (Context.SecsElapsed % 20) < 10 -}}
                 Playing Game 
            {{- else -}}
                {{ Activity.Name |  string.truncate 32 }}
            {{- end -}}
            """
        },
        new() {
            Name = $"Spotify (V{DEFAULT_VERSION})",
            Priority = 1,
            TypeName = typeof(SpotifyGame).Name,
            TitleTemplate = """
            ♪{{- if (Context.SecsElapsed % 30) < 10 -}}
                Listening to Spotify
            {{- else if (Context.SecsElapsed % 30) < 20 -}}
                {{ Activity.TrackTitle | string.truncate 30 }}
            {{- else -}}
                {{ Activity.Artists[0] | string.truncate 30 }}
            {{- end -}}♪ 
            """
        }
    ];

    // https://github.com/discord-net/Discord.Net/tree/de8da0d3b998d32e248e5e438039d266139e4776/src/Discord.Net.Core/Entities/Activities
    public static readonly List<Type> AVAILABLE_TYPES = [
        typeof(CustomStatusGame), 
        typeof(Game), 
        typeof(RichGame), 
        typeof(SpotifyGame), 
        typeof(StreamingGame)
    ];

    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true; 
    public int Priority { get; set; } = 0;

    public string TypeName { get; set; } = string.Empty;

    public string FilterTemplate { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;

    #region Deprecated
    [Obsolete("Moved to TitleDataConfig in version 1")]
    public bool IsPrefix { get; set; } = false;
    [Obsolete("Moved to TitleDataConfig in version 1")]
    public Vector3? Color { get; set; }
    [Obsolete("Moved to TitleDataConfig in version 1")]
    public Vector3? Glow { get; set; }
    #endregion

    public TitleDataConfig? TitleDataConfig { get; set; }

    public Type? ResolveType() => AVAILABLE_TYPES.Find(x => x.Name == TypeName);
}
