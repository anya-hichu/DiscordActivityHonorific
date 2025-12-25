using System.Numerics;

namespace DiscordActivityHonorific.Interop;

public class PartialTitleData
{
    public bool IsPrefix { get; set; } = false;
    public Vector3? Color { get; set; }
    public Vector3? Glow { get; set; }

    public GradientColourSet? GradientColourSet { get; set; }
    public GradientAnimationStyle? GradientAnimationStyle { get; set; }
}
