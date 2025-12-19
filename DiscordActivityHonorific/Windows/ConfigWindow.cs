using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using DiscordActivityHonorific.Activities;
using DiscordActivityHonorific.Updaters;
using DiscordActivityHonorific.Utils;
using Dalamud.Bindings.ImGui;
using Scriban;
using System.Linq;
using System.Numerics;
using Scriban.Helpers;

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
    private ImGuiHelper ImGuiHelper { get; init; }
    private Updater Updater { get; init; }

    public ConfigWindow(Config config, ImGuiHelper imGuiHelper, Updater updater) : base("Discord Activity Honorific - Config##configWindow")
    {
        SizeConstraints = new()
        {
            MinimumSize = new(760, 420),
            MaximumSize = new(float.MaxValue, float.MaxValue)
        };

        Config = config;
        ImGuiHelper = imGuiHelper;
        Updater = updater;
    }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled##enabled", ref enabled))
        {
            Config.Enabled = enabled;
            Config.Save();
            Updater.Toggle(enabled);
        }

        var token = Config.Token;
        var tokenInput = ImGui.InputText("Token##token", ref token, ushort.MaxValue);
        if (tokenInput)
        {
            Config.Token = token;
            Config.Save();
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
            Config.Save();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Define username to filter out PRESENCE_UPDATE events if there are more than one user in the server");

        if (ImGui.Button("New##newActivityConfig"))
        {
            Config.ActivityConfigs.Add(new());
            Config.Save();
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 220);
        if (ImGui.Button($"Recreate Defaults (V{ActivityConfig.DEFAULT_VERSION})##recreateDefaultActivityConfigs"))
        {
            Config.ActivityConfigs.AddRange(ActivityConfig.GetDefaults());
            Config.Save();
        }
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
        {
            if (ImGui.Button("Delete All##deleteAllActivityConfigs"))
            {
                Config.ActivityConfigs.Clear();
                Config.Save();
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
                        Config.Save();
                    }

                    ImGui.SameLine(ImGui.GetWindowWidth() - 60);
                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.DalamudRed))
                    {
                        if (ImGui.Button($"Delete###{baseId}Delete"))
                        {
                            Config.ActivityConfigs.Remove(activityConfig);
                            Config.Save();
                        }
                    }

                    if (ImGui.InputText($"Name###{baseId}Name", ref name, ushort.MaxValue))
                    {
                        activityConfig.Name = name;
                        Config.Save();
                    }

                    var priority = activityConfig.Priority;
                    if (ImGui.InputInt($"Priority###{baseId}Priority", ref priority, 1))
                    {
                        activityConfig.Priority = priority;
                        Config.Save();
                    }

                    var typeName = activityConfig.TypeName;
                    var typeNames = ActivityConfig.AVAILABLE_TYPES.Select(t => t.Name).Prepend(string.Empty).ToList();
                    var typeIndex = typeNames.IndexOf(typeName);

                    if (ImGui.Combo($"Type###{baseId}Type", ref typeIndex, typeNames.ToArray(), typeNames.Count))
                    {
                        activityConfig.TypeName = typeNames[typeIndex];
                        Config.Save();
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
                        Config.Save();
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
                            ImGui.SetTooltip($"Expects single line as output with maximum {Constants.MAX_TITLE_LENGTH} characters\nScriban syntax reference available on https://github.com/scriban/scriban");
                        }   
                    }
                    if (titleTemplateInput)
                    {
                        activityConfig.TitleTemplate = titleTemplate;
                        Config.Save();
                    }

                    var isPrefix = activityConfig.IsPrefix;
                    if (ImGui.Checkbox($"Prefix###{baseId}Prefix", ref isPrefix))
                    {
                        activityConfig.IsPrefix = isPrefix;
                        Config.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Spacing();
                    ImGui.SameLine();

                    var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());

                    var color = activityConfig.Color;
                    if (ImGuiHelper.DrawColorPicker($"Color###{baseId}Color", ref color, checkboxSize))
                    {
                        activityConfig.Color = color;
                        Config.Save();
                    }

                    ImGui.SameLine();
                    ImGui.Spacing();
                    ImGui.SameLine();
                    var glow = activityConfig.Glow;
                    if (ImGuiHelper.DrawColorPicker($"Glow###{baseId}Glow", ref glow, checkboxSize))
                    {
                        activityConfig.Glow = glow;
                        Config.Save();
                    }
                }
            }
        } 
    }

    private static bool TryParseTemplate(string template, out LogMessageBag logMessageBag)
    {
        var parsed = Template.Parse(template);
        logMessageBag = parsed.Messages;
        return !parsed.HasErrors;
    }
}
