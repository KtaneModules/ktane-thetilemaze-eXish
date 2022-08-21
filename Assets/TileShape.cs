namespace TheTileMaze
{
    enum TileShape
    {
        // L-shapes
        NE,
        ES,
        SW,
        NW,

        // T-shapes
        ESW,
        SWN,
        NEW,
        NES,

        // Straights
        NS,
        EW,
        SN,     // Looks the same as NS except when a number is on it
        WE      // Looks the same as EW except when a number is on it
    }
}
