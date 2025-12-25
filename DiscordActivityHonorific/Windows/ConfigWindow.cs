using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Utility;
using DiscordActivityHonorific.Configs;
using DiscordActivityHonorific.Interop;
using DiscordActivityHonorific.Shared;
using DiscordActivityHonorific.Updaters;
using Scriban;
using Scriban.Helpers;
using System;
using System.Linq;
using System.Numerics;

namespace DiscordActivityHonorific.Windows;

public class ConfigWindow : Window
{
    private static readonly string TOKEN_TOOLTIP = """
        Instructions:
        
        1. Create a new discord application (https://discord.com/developers/applications/) or reuse existing one
        2. Check 'Presence Intent' option in 'Bot' tab
        3. Authorize the application with 'Connect' bot permission to logging to your private server using the install link from the 'Installation' tab
        4. Get a token in the 'Bot' tab by clicking on 'Reset token' if you don't have one already
        5. Paste it here
        """;

    private Config Config { get; init; }
    private CustomImGui CustomImGui { get; init; }
    private IDalamudPluginInterface PluginInterface { get; init; }
    private Updater Updater { get; init; }

    public ConfigWindow(Config config, CustomImGui customImGui, IDalamudPluginInterface pluginInterface, Updater updater) : base("Discord Activity Honorific - Config##configWindow")
    {
        SizeConstraints = new()
        {
            MinimumSize = new(820, 520),
            MaximumSize = new(float.MaxValue, float.MaxValue)
        };

        Config = config;
        CustomImGui = customImGui;
        PluginInterface = pluginInterface;
        Updater = updater;
    }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled##enabled", ref enabled))
        {
            Config.Enabled = enabled;
            SaveConfig();
            Updater.Toggle(enabled);
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 150);
        var isHonorificSupporter = Config.IsHonorificSupporter;
        if (ImGui.Checkbox("Honorific Supporter##isHonorificSupporter", ref isHonorificSupporter))
        {
            Config.IsHonorificSupporter = isHonorificSupporter;
            SaveConfig();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Only check if supporting Honorific author via https://ko-fi.com/Caraxi, it gives access to extra features");


        var token = Config.Token;
        var tokenInput = ImGui.InputText("Token##token", ref token, ushort.MaxValue);
        if (tokenInput)
        {
            Config.Token = token;
            SaveConfig();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(TOKEN_TOOLTIP);

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        ImGui.Text($"State: {Updater.State()}");

        if (Config.Enabled)
        {
            ImGui.SameLine(ImGui.GetWindowWidth() - 80);
            if (ImGui.Button("Reconnect##reconnect")) Updater.Restart();
        }

        var username = Config.Username;
        if (ImGui.InputText("Username##username", ref username, ushort.MaxValue))
        {
            Config.Username = username;
            SaveConfig();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Define username to filter out PRESENCE_UPDATE events if there are more than one user in the server");

        if (ImGui.Button("New##newActivityConfig"))
        {
            Config.ActivityConfigs.Add(new());
            SaveConfig();
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 220);
        if (ImGui.Button($"Recreate Defaults (V{ActivityConfig.DEFAULT_VERSION})##recreateDefaultActivityConfigs"))
        {
            Config.ActivityConfigs.AddRange(ActivityConfig.GetDefaults());
            SaveConfig();
        }
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
        {
            if (ImGui.Button("Delete All##deleteAllActivityConfigs"))
            {
                Config.ActivityConfigs.Clear();
                SaveConfig();
            }
        }

        using var tabBar = ImRaii.TabBar("activityConfigsTabBar");
        if (tabBar)
        {
            foreach (var activityConfig in Config.ActivityConfigs)
            {
                var baseId = $"activityConfigs{activityConfig.GetHashCode()}";

                var name = activityConfig.Name;
                using var tabItem = ImRaii.TabItem($"{(name.IsNullOrWhitespace() ? "(Blank)" : name)}###{baseId}TabItem");
                if (tabItem)
                {
                    var activityConfigEnabled = activityConfig.Enabled;
                    if (ImGui.Checkbox($"Enabled###{baseId}enabled", ref activityConfigEnabled))
                    {
                        activityConfig.Enabled = activityConfigEnabled;
                        SaveConfig();
                    }

                    ImGui.SameLine(ImGui.GetWindowWidth() - 60);
                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
                    {
                        if (ImGui.Button($"Delete###{baseId}Delete"))
                        {
                            Config.ActivityConfigs.Remove(activityConfig);
                            SaveConfig();
                        }
                    }

                    if (ImGui.InputText($"Name###{baseId}Name", ref name, ushort.MaxValue))
                    {
                        activityConfig.Name = name;
                        SaveConfig();
                    }

                    var priority = activityConfig.Priority;
                    if (ImGui.InputInt($"Priority###{baseId}Priority", ref priority, 1))
                    {
                        activityConfig.Priority = priority;
                        SaveConfig();
                    }

                    var typeName = activityConfig.TypeName;
                    var typeNames = ActivityConfig.AVAILABLE_TYPES.Select(t => t.Name).Prepend(string.Empty).ToList();
                    var typeIndex = typeNames.IndexOf(typeName);

                    if (ImGui.Combo($"Type###{baseId}Type", ref typeIndex, typeNames.ToArray(), typeNames.Count))
                    {
                        activityConfig.TypeName = typeNames[typeIndex];
                        SaveConfig();
                    };

                    var type = activityConfig.ResolveType();
                    if (type != null)
                    {
                        if (ImGui.CollapsingHeader($"Available Template Variables###{baseId}Properties"))
                        {
                            using var table = ImRaii.Table($"{baseId}Properties", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable, new(ImGui.GetWindowWidth(), 200));
                            if (table)
                            {
                                ImGui.TableSetupColumn($"Name###{baseId}PropertyNames", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn($"Type###{baseId}PropertyTypes", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableHeadersRow();

                                foreach (var property in type.GetProperties())
                                {
                                    if (ImGui.TableNextColumn()) ImGui.Text($"Activity.{property.Name}");
                                    if (ImGui.TableNextColumn()) ImGui.Text(property.PropertyType.ScriptPrettyName());
                                }

                                foreach (var property in typeof(UpdaterContext).GetProperties())
                                {
                                    if (ImGui.TableNextColumn()) ImGui.Text($"Context.{property.Name}");
                                    if (ImGui.TableNextColumn()) ImGui.Text(property.PropertyType.ScriptPrettyName());
                                }
                            }
                        }
                    }

                    var filterTemplate = activityConfig.FilterTemplate;
                    var filterTemplateInput = ImGui.InputTextMultiline($"Filter Template (scriban)###{baseId}FilterTemplate", ref filterTemplate, ushort.MaxValue, new(ImGui.GetWindowWidth() - 170, 50));
                    if (ImGui.IsItemHovered())
                    {
                        if (!TryParseTemplate(filterTemplate, out var logMessageBag))
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed)) ImGui.SetTooltip(string.Join("\n", logMessageBag));
                        }
                        else
                        {
                            ImGui.SetTooltip("Expects parsable boolean as output if provided\nSyntax reference available on https://github.com/scriban/scriban");
                        }
                    }
                    if (filterTemplateInput)
                    {
                        activityConfig.FilterTemplate = filterTemplate;
                        SaveConfig();
                    }

                    var titleTemplate = activityConfig.TitleTemplate;
                    var titleTemplateInput = ImGui.InputTextMultiline($"Title Template (scriban)###{baseId}TitleTemplate", ref titleTemplate, ushort.MaxValue, new(ImGui.GetWindowWidth() - 170, ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 40));

                    if (ImGui.IsItemHovered())
                    {
                        if (!TryParseTemplate(titleTemplate, out var errorMessages))
                        {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed)) ImGui.SetTooltip(string.Join("\n", errorMessages));
                        }
                        else
                        {
                            ImGui.SetTooltip($"Expects single line as output with maximum {Constraint.MaxTitleLength} characters\nScriban syntax reference available on https://github.com/scriban/scriban");
                        }   
                    }
                    if (titleTemplateInput)
                    {
                        activityConfig.TitleTemplate = titleTemplate;
                        SaveConfig();
                    }

                    activityConfig.TitleDataConfig ??= new();
                    DrawSettings(baseId, activityConfig.TitleDataConfig);
                }
            }
        } 
    }

    private void DrawSettings(string baseId, TitleDataConfig titleDataConfig)
    {
        var nestedId = $"{baseId}TitleDataConfig";

        var isPrefix = titleDataConfig.IsPrefix;
        if (ImGui.Checkbox($"Prefix###{nestedId}Prefix", ref isPrefix))
        {
            titleDataConfig.IsPrefix = isPrefix;
            SaveConfig();
        }
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());

        var color = titleDataConfig.Color;
        if (CustomImGui.ColorPicker($"Color###{nestedId}Color", ref color, checkboxSize))
        {
            titleDataConfig.Color = color;
            SaveConfig();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        var glow = titleDataConfig.Glow;
        if (CustomImGui.ColorPicker($"Glow###{nestedId}Glow", ref glow, checkboxSize))
        {
            titleDataConfig.Glow = glow;
            SaveConfig();
        }

        if (!Config.IsHonorificSupporter) return;
        
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var maybeGradientColourSet = titleDataConfig.GradientColourSet;

        var gradientColourSets = Enum.GetValues<GradientColourSet>();
        var selectedGradientColourSetIndex = maybeGradientColourSet.HasValue ? gradientColourSets.IndexOf(maybeGradientColourSet.Value) + 1 : 0;

        var comboWidth = 140;
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo($"Gradient Color Set###{nestedId}GradientColorSet", ref selectedGradientColourSetIndex, gradientColourSets.Select(s => s.GetFancyName()).Prepend("None").ToArray()))
        {
            if (selectedGradientColourSetIndex == 0)
            {
                titleDataConfig.GradientColourSet = null;
                titleDataConfig.GradientAnimationStyle = null;
            }
            else
            {
                titleDataConfig.GradientColourSet = gradientColourSets.ElementAt(selectedGradientColourSetIndex - 1);
            }
            SaveConfig();
        }

        if (!titleDataConfig.GradientColourSet.HasValue) return;

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        var maybeGradientAnimationStyle = titleDataConfig.GradientAnimationStyle;

        var gradientAnimationStyles = Enum.GetValues<GradientAnimationStyle>();
        var selectedGradientAnimationStyleIndex = maybeGradientAnimationStyle.HasValue ? gradientAnimationStyles.IndexOf(maybeGradientAnimationStyle.Value) + 1 : 0;

        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.Combo($"Gradient Animation Style###{nestedId}GradientAnimationStyle", ref selectedGradientAnimationStyleIndex, gradientAnimationStyles.Select(s => s.ToString()).Prepend("None").ToArray()))
        {
            titleDataConfig.GradientAnimationStyle = selectedGradientAnimationStyleIndex == 0 ? null : gradientAnimationStyles.ElementAt(selectedGradientAnimationStyleIndex - 1);
            SaveConfig();
        }
    }

    private static bool TryParseTemplate(string template, out LogMessageBag logMessageBag)
    {
        var parsed = Template.Parse(template);
        logMessageBag = parsed.Messages;
        return !parsed.HasErrors;
    }

    private void SaveConfig() => PluginInterface.SavePluginConfig(Config);
}
