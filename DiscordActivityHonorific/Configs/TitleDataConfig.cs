using DiscordActivityHonorific.Interop;
using System;

namespace DiscordActivityHonorific.Configs;

[Serializable]
public class TitleDataConfig : PartialTitleData
{
    public TitleData ToTitleData(string title, bool isHonorificSupporter) => new()
    {
        Title = title,
        IsPrefix = IsPrefix,
        Color = Color,
        Glow = Glow,
        GradientColourSet = isHonorificSupporter ? GradientColourSet : null,
        GradientAnimationStyle = isHonorificSupporter && GradientColourSet != null ? GradientAnimationStyle : null
    };
}
