using Dalamud.Plugin;

namespace DiscordActivityHonorific.Configs;

public class ConfigMigrator(IDalamudPluginInterface pluginInterface)
{
    public void MaybeMigrate(Config config)
    {
        var currentVersion = Config.CURRENT_VERSION;
        if (config.Version == currentVersion) return;

        if (config.Version < 1)
        {
            config.ActivityConfigs.ForEach(ac =>
            {
                ac.TitleDataConfig = new()
                {
                    IsPrefix = ac.IsPrefix,
                    Color = ac.Color,
                    Glow = ac.Glow
                };
            });
        }

        config.Version = currentVersion;
        pluginInterface.SavePluginConfig(config);
    }
}
