﻿//
// When We Fell
//

public enum TileType
{
	Air,
	Floor,
	Wall,
	Platform,
	Count
}

// Stores tile data as a lookup table. Tiles are represented 
// by an index. Associated data can be found for a given tile
// type by getting it from here.
public static class TileManager
{
	// Note: this list must match the exact order of the TileType
	// enumeration above.
	private static TileData[] data =
	{
		new AirTile(),
		new FloorTile(),
		new WallTile(),
		new PlatformTile()
	};

	public static TileData GetData(TileType type)
		=> data[(int)type];
}