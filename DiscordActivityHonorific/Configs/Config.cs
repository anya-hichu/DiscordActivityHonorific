using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace DiscordActivityHonorific.Configs;

[Serializable]
public class Config : IPluginConfiguration
{
    public static readonly int CURRENT_VERSION = 1;

    public int Version { get; set; } = CURRENT_VERSION;

    public bool Enabled { get; set; } = true;
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
 
    public bool IsHonorificSupporter { get; set; } = false;

    public List<ActivityConfig> ActivityConfigs { get; set; } = [];
}
