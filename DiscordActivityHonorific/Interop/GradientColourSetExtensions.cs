namespace DiscordActivityHonorific.Interop;

public static class GradientColourSetExtensions
{
    public static string GetFancyName(this GradientColourSet colorSet) => colorSet switch
    {
        GradientColourSet.PrideRainbow => "Pride Rainbow",
        GradientColourSet.Transgender => "Transgender",
        GradientColourSet.Lesbian => "Lesbian",
        GradientColourSet.Bisexual => "Bisexual",
        GradientColourSet.BlackAndWhite => "Black & White",
        GradientColourSet.BlackAndRed => "Black & Red",
        GradientColourSet.BlackAndBlue => "Black & Blue",
        GradientColourSet.BlackAndYellow => "Black & Yellow",
        GradientColourSet.BlackAndGreen => "Black & Green",
        GradientColourSet.BlackAndPink => "Black & Pink",
        GradientColourSet.BlackAndCyan => "Black & Cyan",
        GradientColourSet.CherryBlossom => "Cherry Blossom",
        GradientColourSet.Golden => "Golden",
        GradientColourSet.PastelRainbow => "Pastel Rainbow",
        GradientColourSet.DarkRainbow => "Dark Rainbow",
        _ => string.Empty
    }; 
}
