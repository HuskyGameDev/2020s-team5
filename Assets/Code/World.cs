﻿//
// When We Fell
// 

using UnityEngine;
using System.Collections.Generic;

public class World : MonoBehaviour
{
	// Chunks are stored in a hash map for maximum flexibility. This doesn't force 
	// any constraints as to how the world should be except that it must be. 
	// Accessing chunks is slightly slower, but the difference is negligible.
	private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

	private void Start()
	{
		SampleGenerator generator = new SampleGenerator();
		generator.Generate(this);
	}

	// Returns the chunk at the given position, or null if the
	// chunk doesn't exist.
	public Chunk GetChunk(int cX, int cY)
	{
		if (chunks.TryGetValue(new Vector2Int(cX, cY), out Chunk chunk))
			return chunk;

		return null;
	}

	public Chunk GetChunk(Vector2Int cP)
		=> GetChunk(cP.x, cP.y);

	// Returns the tile at the given world location.
	public Tile GetTile(int wX, int wY)
	{
		Vector2Int cP = Utils.WorldToChunkP(wX, wY);
		Chunk chunk = GetChunk(cP);

		if (chunk == null)
			return TileType.Air;

		Vector2Int rel = Utils.WorldToRelP(wX, wY);
		return chunk.GetTile(rel.x, rel.y);
	}

	// Sets a tile at the given world location. Computes the chunk the tile belongs
	// in, and creates it if it doesn't exist.
	public void SetTile(int wX, int wY, Tile tile)
	{
		Vector2Int cP = Utils.WorldToChunkP(wX, wY);
		Chunk chunk = GetChunk(cP);

		if (chunk == null)
		{
			chunk = new Chunk(cP.x, cP.y);
			chunks.Add(cP, chunk);
		}

		Vector2Int rel = Utils.WorldToRelP(wX, wY);
		chunk.SetTile(rel.x, rel.y, tile);
	}
}