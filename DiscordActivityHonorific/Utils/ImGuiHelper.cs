

using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace DiscordActivityHonorific.Utils;

public class ImGuiHelper
{
    // Source: https://github.com/Caraxi/Honorific/blob/1.4.1.0/ConfigWindow.cs#L826
    private Vector3 editingColour = Vector3.One;
    public bool DrawColorPicker(string label, ref Vector3? color, Vector2 checkboxSize)
    {
        var modified = false;
        bool comboOpen;
        ImGui.SetNextItemWidth(checkboxSize.X * 2);
        if (color == null)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0xFFFFFFFF);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0xFFFFFFFF);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0xFFFFFFFF);
            var p = ImGui.GetCursorScreenPos();
            var dl = ImGui.GetWindowDrawList();
            comboOpen = ImGui.BeginCombo(label, " ", ImGuiComboFlags.HeightLargest);
            dl.AddLine(p, p + new Vector2(checkboxSize.X), 0xFF0000FF, 3f * ImGuiHelpers.GlobalScale);
            ImGui.PopStyleColor(3);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(color.Value, 1));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(color.Value, 1));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(color.Value, 1));
            comboOpen = ImGui.BeginCombo(label, "  ", ImGuiComboFlags.HeightLargest);
            ImGui.PopStyleColor(3);
        }

        if (comboOpen)
        {
            if (ImGui.IsWindowAppearing())
            {
                editingColour = color ?? Vector3.One;
            }
            if (ImGui.ColorButton($"##ColorPickClear", Vector4.One, ImGuiColorEditFlags.NoTooltip))
            {
                color = null;
                modified = true;
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Clear selected colour");
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            var dl = ImGui.GetWindowDrawList();
            dl.AddLine(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), 0xFF0000FF, 3f * ImGuiHelpers.GlobalScale);

            if (color != null)
            {
                ImGui.SameLine();
                if (ImGui.ColorButton($"##ColorPick_old", new Vector4(color.Value, 1), ImGuiColorEditFlags.NoTooltip))
                {
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Revert to previous selection");
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
            }

            ImGui.SameLine();

            if (ImGui.ColorButton("Confirm", new Vector4(editingColour, 1), ImGuiColorEditFlags.NoTooltip, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetItemRectSize().Y)))
            {
                color = editingColour;
                modified = true;
                ImGui.CloseCurrentPopup();
            }
            var size = ImGui.GetItemRectSize();

            if (ImGui.IsItemHovered())
            {
                dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), 0x33333333);
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            var textSize = ImGui.CalcTextSize("Confirm");
            dl.AddText(ImGui.GetItemRectMin() + size / 2 - textSize / 2, ImGui.ColorConvertFloat4ToU32(new Vector4(editingColour, 1)) ^ 0x00FFFFFF, "Confirm");
            ImGui.ColorPicker3($"##ColorPick", ref editingColour, ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview);

            ImGui.EndCombo();
        }

        return modified;
    }
}
