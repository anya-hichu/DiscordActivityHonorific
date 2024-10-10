using Dalamud.Configuration;
using DiscordActivityHonorific.Activities;
using System;
using System.Collections.Generic;

namespace DiscordActivityHonorific;

[Serializable]
public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<ActivityConfig> ActivityConfigs { get; set; } = [];

    public Config() { }

    public Config(List<ActivityConfig> activityConfigs)
    {
        ActivityConfigs = activityConfigs;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
