using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordActivityHonorific.Updaters;

public class Updater : IDisposable
{
    private Config Config { get; init; }
    private IFramework Framework { get; init; }
    private IDalamudPluginInterface PluginInterface { get; init; }
    private IPluginLog PluginLog { get; init; }
    private ICallGateSubscriber<int, string, object> SetCharacterTitleSubscriber { get; init; }
    private ICallGateSubscriber<int, object> ClearCharacterTitleSubscriber { get; init; }

    private Action? UpdateTitle { get; set; }
    private string? UpdatedTitleJson { get; set; }
    private UpdaterContext UpdaterContext { get; init; } = new();

    private DiscordSocketClient DiscordSocketClient { get; init; } = new(new()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildPresences,
        LogLevel = LogSeverity.Verbose
    });

    public Updater(Config config, IFramework framwork, IDalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        Config = config;
        Framework = framwork;
        PluginInterface = pluginInterface;
        PluginLog = pluginLog;

        SetCharacterTitleSubscriber = PluginInterface.GetIpcSubscriber<int, string, object>("Honorific.SetCharacterTitle");
        ClearCharacterTitleSubscriber = PluginInterface.GetIpcSubscriber<int, object>("Honorific.ClearCharacterTitle");

        DiscordSocketClient.Log += Log;
        DiscordSocketClient.PresenceUpdated += PresenceUpdated;

        if (Config.Enabled)
        {
            Start();
        }

        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        Framework.RunOnFrameworkThread(() =>
        {
            ClearCharacterTitleSubscriber.InvokeAction(0);
        }); 
        DiscordSocketClient.Dispose();
    }

    public Task Enable(bool value)
    {
        return value ? Start() : Stop();
    }

    public Task Restart()
    {
        return Stop().ContinueWith(t => Start());
    }

    public Task Start()
    {
        if (!Config.Token.IsNullOrWhitespace() && State() != ConnectionState.Connected)
        {
            var task = DiscordSocketClient.LoginAsync(TokenType.Bot, Config.Token);
            DiscordSocketClient.StartAsync();
            return task;
        }
        else
        {
            return Task.CompletedTask;
        }
    }
    public Task Stop()
    {
        return DiscordSocketClient.LogoutAsync().ContinueWith(t =>
        {
            DiscordSocketClient.StopAsync();
            Framework.RunOnFrameworkThread(() =>
            {
                ClearCharacterTitleSubscriber.InvokeAction(0);
            });
        });
    }

    public ConnectionState State()
    {
        return DiscordSocketClient.ConnectionState;
    }

    private Task PresenceUpdated(SocketUser socketUser, SocketPresence oldPresence, SocketPresence newPresence)
    {
        try
        {
            PluginLog.Debug($"PresenceUpdated for user '{socketUser.Username}':\n{JsonConvert.SerializeObject(newPresence, Formatting.Indented)}");
            if (Config.Username.IsNullOrWhitespace() || Config.Username == socketUser.Username)
            {
                foreach (var activityConfig in Config.ActivityConfigs.Where(c => c.Enabled).OrderByDescending(c => c.Priority))
                {
                    var activity = newPresence.Activities.FirstOrDefault(activity => activity.GetType().IsAssignableTo(activityConfig.ResolveType()));
                    if (activity != null)
                    {
                        var matchFilter = true;
                        if (!activityConfig.FilterTemplate.IsNullOrWhitespace())
                        {
                            var filterTemplate = Template.Parse(activityConfig.FilterTemplate);
                            var filter = filterTemplate.Render(new { Activity = activity, Context = UpdaterContext }, member => member.Name);

                            if (bool.TryParse(filter, out var parsedFilter))
                            {
                                matchFilter = parsedFilter;
                            }
                            else
                            {
                                PluginLog.Error($"Unable to parse filter '{filter}' as boolean, skipping result");
                            }
                        }

                        if (matchFilter)
                        {
                            UpdaterContext.SecsElapsed = 0;
                            UpdateTitle = () =>
                            {
                                if (Config.Enabled && activityConfig.Enabled)
                                {
                                    var titleTemplate = Template.Parse(activityConfig.TitleTemplate);
                                    var title = titleTemplate.Render(new { Activity = activity, Context = UpdaterContext }, member => member.Name);

                                    var data = new Dictionary<string, object>() {
                                        {"Title", title},
                                        {"IsPrefix", activityConfig.IsPrefix},
                                        {"Color", activityConfig.Color!},
                                        {"Glow", activityConfig.Glow!}
                                    };

                                    var serializedData = JsonConvert.SerializeObject(data, Formatting.Indented);
                                    if (serializedData != UpdatedTitleJson)
                                    {
                                        PluginLog.Debug($"Call Honorific SetCharacterTitle IPC with:\n{serializedData}");
                                        SetCharacterTitleSubscriber.InvokeAction(0, serializedData);
                                        UpdatedTitleJson = serializedData;
                                    }
                                }
                                else
                                {
                                    ClearTitle();
                                }
                            };
                            return Task.CompletedTask;
                        }
                    }

                }

                if (UpdateTitle != null || UpdatedTitleJson != null)
                {
                    ClearTitle();
                }
            }
            else
            {
                PluginLog.Debug($"Ignored PresenceUpdated for '{socketUser.Username}' since it doesn't match explictely configured username: '{Config.Username}'");
            }
        } 
        catch (Exception e)
        {
            PluginLog.Error(e.ToString());
        }
        return Task.CompletedTask;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (Config.Enabled && UpdateTitle != null)
        {
            try
            {
                UpdateTitle();
            }
            catch (Exception e)
            {
                PluginLog.Error(e.ToString());
            }
            UpdaterContext.SecsElapsed += framework.UpdateDelta.TotalSeconds;
        }
    }

    private Task Log(LogMessage logMessage)
    {
        if (logMessage.Exception != null)
        {
            PluginLog.Error(logMessage.Exception.ToString());
        }
        else
        {
            PluginLog.Debug(logMessage.Message);
        }

        return Task.CompletedTask;
    }

    private void ClearTitle()
    {
        PluginLog.Debug("Call Honorific ClearCharacterTitle IPC");
        Framework.RunOnFrameworkThread(() =>
        {
            ClearCharacterTitleSubscriber.InvokeAction(0);
        });
        UpdaterContext.SecsElapsed = 0;
        UpdateTitle = null;
        UpdatedTitleJson = null;
    }
}
