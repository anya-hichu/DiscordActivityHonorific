using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using DiscordActivityHonorific.Activities;
using DiscordActivityHonorific.Updaters;
using DiscordActivityHonorific.Utils;
using ImGuiNET;
using System.Linq;
using System.Numerics;

namespace DiscordActivityHonorific.Windows;

public class ConfigWindow : Window
{
    private Config Config { get; init; }
    private ImGuiHelper ImGuiHelper { get; init; }
    private Updater Updater { get; init; }

    public ConfigWindow(Config config, ImGuiHelper imGuiHelper, Updater updater) : base("Discord Activity Honorific Config##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
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
            Updater.Enable(enabled);
        }

        var token = Config.Token;
        var tokenInput = ImGui.InputText("Token##token", ref token, ushort.MaxValue);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Instructions:\n1. Create a new discord application (https://discord.com/developers/applications/) or reuse existing one\n2. Check 'Presence Intent' option in 'Bot' tab\n3. Authorize the application with 'Connect' bot permission to logging to your private server using the install link from the 'Installation' tab\n4. Get a token in the 'Bot' tab by clicking on 'Reset token' if you don't have one already\n5. Paste it here");
        }
        if (tokenInput)
        {
            Config.Token = token;
            Config.Save();
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        ImGui.Text($"State: {Updater.State()}");

        if (Config.Enabled)
        {
            ImGui.SameLine(ImGui.GetWindowWidth() - 80);
            if (ImGui.Button("Reconnect##reconnect"))
            {
                Updater.Restart();
            }
        }

        var username = Config.Username;
        var usernameInput = ImGui.InputText("Username##username", ref username, ushort.MaxValue);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Define username to filter out PRESENCE_UPDATE events if there are more than one user in the server");
        }
        if (usernameInput)
        {
            Config.Username = username;
            Config.Save();
        }

        if (ImGui.Button("New##activityConfigsNew"))
        {
            Config.ActivityConfigs.Add(new());
            Config.Save();
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 220);
        if (ImGui.Button($"Recreate Defaults (V{ActivityConfig.DEFAULT_VERSION})##activityConfigsRecreateDefaults"))
        {
            Config.ActivityConfigs.AddRange(ActivityConfig.GetDefaults());
            Config.Save();
        }
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DalamudRed);
        if (ImGui.Button("Delete All##activityConfigsDeleteAll"))
        {
            Config.ActivityConfigs.Clear();
            Config.Save();
        }
        ImGui.PopStyleColor();

        if (ImGui.BeginTabBar("activityConfigsTabBar"))
        {
            foreach (var activityConfig in Config.ActivityConfigs)
            {
                var activityConfigId = $"activityConfigs{activityConfig.GetHashCode()}";

                var name = activityConfig.Name;
                if (ImGui.BeginTabItem($"{(name.IsNullOrWhitespace() ? "(Blank)" : name)}###{activityConfigId}TabItem"))
                {
                    ImGui.Indent(10);

                    var activityConfigEnabled = activityConfig.Enabled;
                    if (ImGui.Checkbox($"Enabled###{activityConfigId}enabled", ref activityConfigEnabled))
                    {
                        activityConfig.Enabled = activityConfigEnabled;
                        Config.Save();
                    }

                    ImGui.SameLine(ImGui.GetWindowWidth() - 60);
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.DalamudRed);
                    if (ImGui.Button($"Delete###{activityConfigId}Delete"))
                    {
                        Config.ActivityConfigs.Remove(activityConfig);
                        Config.Save();
                    }
                    ImGui.PopStyleColor();

                    if (ImGui.InputText($"Name###{activityConfigId}Name", ref name, ushort.MaxValue))
                    {
                        activityConfig.Name = name;
                        Config.Save();
                    }

                    var priority = activityConfig.Priority;
                    if (ImGui.InputInt($"Priority###{activityConfigId}Priority", ref priority, ushort.MaxValue))
                    {
                        activityConfig.Priority = priority;
                        Config.Save();
                    }

                    var typeName = activityConfig.TypeName;
                    var typeNames = ActivityConfig.AVAILABLE_TYPES.Select(t => t.Name).Prepend(string.Empty).ToList();
                    var typeIndex = typeNames.IndexOf(typeName);

                    if (ImGui.Combo($"Type###{activityConfigId}Type", ref typeIndex, typeNames.ToArray(), typeNames.Count))
                    {
                        activityConfig.TypeName = typeNames[typeIndex];
                        Config.Save();
                    };

                    var type = activityConfig.ResolveType();
                    if (type != null)
                    {
                        if (ImGui.CollapsingHeader($"Available Template Variables###{activityConfigId}Properties"))
                        {
                            if (ImGui.BeginTable($"{activityConfigId}Properties", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable, new(ImGui.GetWindowWidth(), 200)))
                            {
                                ImGui.TableSetupColumn($"Name###{activityConfigId}PropertyNames", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn($"Type###{activityConfigId}PropertyTypes", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableHeadersRow();

                                foreach (var property in type.GetProperties())
                                {
                                    if (ImGui.TableNextColumn())
                                    {
                                        ImGui.Text($"Activity.{property.Name}");
                                    }
                                    if (ImGui.TableNextColumn())
                                    {
                                        ImGui.Text(property.PropertyType.ToString());
                                    }
                                }

                                foreach (var property in typeof(UpdaterContext).GetProperties())
                                {
                                    if (ImGui.TableNextColumn())
                                    {
                                        ImGui.Text($"Context.{property.Name}");
                                    }
                                    if (ImGui.TableNextColumn())
                                    {
                                        ImGui.Text(property.PropertyType.ToString());
                                    }
                                }

                                ImGui.EndTable();
                            }
                        }
                    }

                    var filterTemplate = activityConfig.FilterTemplate;
                    var filterTemplateInput = ImGui.InputTextMultiline($"Filter Template (scriban)###{activityConfigId}FilterTemplate", ref filterTemplate, ushort.MaxValue, new(ImGui.GetWindowWidth() - 170, 50));
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Expects parsable boolean as output if provided\nSyntax reference available on https://github.com/scriban/scriban");
                    }
                    if (filterTemplateInput)
                    {
                        activityConfig.FilterTemplate = filterTemplate;
                        Config.Save();
                    }

                    var titleTemplate = activityConfig.TitleTemplate;
                    var titleTemplateInput = ImGui.InputTextMultiline($"Title Template (scriban)###{activityConfigId}TitleTemplate", ref titleTemplate, ushort.MaxValue, new(ImGui.GetWindowWidth() - 170, ImGui.GetWindowHeight() - ImGui.GetCursorPosY() - 40));
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Expects single line as output\nSyntax reference available on https://github.com/scriban/scriban");
                    }
                    if (titleTemplateInput)
                    {
                        activityConfig.TitleTemplate = titleTemplate;
                        Config.Save();
                    }

                    var isPrefix = activityConfig.IsPrefix;
                    if (ImGui.Checkbox($"Prefix###{activityConfigId}Prefix", ref isPrefix))
                    {
                        activityConfig.IsPrefix = isPrefix;
                        Config.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Spacing();
                    ImGui.SameLine();

                    var checkboxSize = new Vector2(ImGui.GetTextLineHeightWithSpacing(), ImGui.GetTextLineHeightWithSpacing());

                    var color = activityConfig.Color;
                    if (ImGuiHelper.DrawColorPicker($"Color###{activityConfigId}Color", ref color, checkboxSize))
                    {
                        activityConfig.Color = color;
                        Config.Save();
                    }

                    ImGui.SameLine();
                    ImGui.Spacing();
                    ImGui.SameLine();
                    var glow = activityConfig.Glow;
                    if (ImGuiHelper.DrawColorPicker($"Glow###{activityConfigId}Glow", ref glow, checkboxSize))
                    {
                        activityConfig.Glow = glow;
                        Config.Save();
                    }

                    ImGui.Unindent();
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        } 
    }
}
