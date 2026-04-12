// WorldRegion.cs
// Defines all 7 world regions and provides a geographic distance matrix
// used by the corruption, buff, turn, and re-corruption systems.

public enum WorldRegion
{
    NorthAmerica,
    SouthAmerica,
    Europe,
    Russia,
    Asia,
    Africa,
    Australia
}

public static class RegionDistanceHelper
{
    // Geographic distance between regions on a 0-4 scale.
    // 0 = same, 1 = adjacent/nearby, 4 = opposite side of the world.
    //
    //               NA  SA  EU  RU  AS  AF  AU
    private static readonly int[,] DistanceMatrix =
    {
        { 0, 1, 2, 3, 3, 2, 4 }, // NorthAmerica
        { 1, 0, 3, 4, 4, 2, 4 }, // SouthAmerica
        { 2, 3, 0, 1, 2, 1, 3 }, // Europe
        { 3, 4, 1, 0, 1, 2, 3 }, // Russia
        { 3, 4, 2, 1, 0, 2, 1 }, // Asia
        { 2, 2, 1, 2, 2, 0, 2 }, // Africa
        { 4, 4, 3, 3, 1, 2, 0 }  // Australia
    };

    public static int GetDistance(WorldRegion a, WorldRegion b)
        => DistanceMatrix[(int)a, (int)b];

    public static string GetDisplayName(WorldRegion region) => region switch
    {
        WorldRegion.NorthAmerica => "North America",
        WorldRegion.SouthAmerica => "South America",
        WorldRegion.Europe       => "Europe",
        WorldRegion.Russia       => "Russia",
        WorldRegion.Asia         => "Asia",
        WorldRegion.Africa       => "Africa",
        WorldRegion.Australia    => "Australia",
        _                        => region.ToString()
    };
}