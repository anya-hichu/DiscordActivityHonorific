using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DiscordActivityHonorific.Windows;
using DiscordActivityHonorific.Updaters;
using DiscordActivityHonorific.Configs;

namespace DiscordActivityHonorific;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] private static IChatGui ChatGui { get; set; } = null!;
    [PluginService] private static IPluginLog PluginLog { get; set; } = null!;
    [PluginService] private static IFramework Framework { get; set; } = null!;

    private const string CommandName = "/discordactivityhonorific";
    private const string CommandHelpMessage = $"Available subcommands for {CommandName} are config, enable and disable";

    private Config Config { get; init; }

    private WindowSystem WindowSystem { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private Updater Updater { get; init; }
    

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new() { ActivityConfigs = ActivityConfig.GetDefaults() };
        #region Deprecated
        new ConfigMigrator(PluginInterface).MaybeMigrate(Config);
        #endregion
        Updater = new(ChatGui, Config, Framework, PluginInterface, PluginLog);
        ConfigWindow = new(Config, new(), PluginInterface, Updater);

        WindowSystem = new(nameof(DiscordActivityHonorific));
        WindowSystem.AddWindow(ConfigWindow);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = CommandHelpMessage
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        Updater.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var subcommand = args.Split(" ", 2)[0];
        if (subcommand == "config")
        {
            ToggleConfigUI();
        }
        else if (subcommand == "enable")
        {
            Config.Enabled = true;
            SaveConfig();
            Updater.Start();
        }
        else if (subcommand == "disable")
        {
            Config.Enabled = false;
            SaveConfig();
            Updater.Stop();
        }
        else
        {
            ChatGui.Print(CommandHelpMessage);
        }
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private void SaveConfig() => PluginInterface.SavePluginConfig(Config);
}
