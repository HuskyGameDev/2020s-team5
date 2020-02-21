﻿using UnityEngine;
using System.Collections.Generic;

// Notes on room types:
// 0 = arbitrary
// 1 = exits to the left and right.
// 2 = exits to the left, right, and down.
// 3 = exits to the left, right, and up.
// 4 = exits in all four directions. 

public class ProcGen
{
	struct PathEntry
	{
		public int x, y;
		public bool[] dirs;
		
		public PathEntry(int x, int y)
		{
			this.x = x;
			this.y = y;
			dirs = new bool[4];
		}
	}

	private const int Left = 0, Right = 1, Down = 2, Up = 3;

	private GameObject player;
	private GameObject[] mobs;
	private int mobCap = 2;

	private const int numTypes = 5;

	// Width and height of the level in rooms.
	private readonly int levelWidth = 4;
	private readonly int levelHeight = 4;

	// A flattened 2D array representing the room types at each location.
	private int[] level;

	private List<PathEntry> solutionPath = new List<PathEntry>();

	// Tracks which rooms have been put in the solution path already.
	// This prevents the path overwriting itself.
	private HashSet<Vector2Int> checkedRooms = new HashSet<Vector2Int>();

	// Provides all options for a given exit direction.
	// For example, the hash set corresponding to index 0
	// contains all rooms that exit to the left.
	private HashSet<int>[] roomOptions = new HashSet<int>[4];

	private static TextAsset[] obstacles;

	public ProcGen()
	{
		mobs = Resources.LoadAll<GameObject>("Mobs");
		player = GameObject.FindWithTag("Player");

		level = new int[levelWidth * levelHeight];

		roomOptions[Left] = new HashSet<int>() { 1, 2, 3, 4 };
		roomOptions[Right] = new HashSet<int>() { 1, 2, 3, 4 };
		roomOptions[Down] = new HashSet<int>() { 2, 4 };
		roomOptions[Up] = new HashSet<int>() { 3, 4 };
	}

	// Returns a random obstacle from all obstacle blocks
	// saved in the Resources/RoomData/Obstacles folder.
	public static TextAsset GetRandomObstacle()
	{
		if (obstacles == null)
			obstacles = Resources.LoadAll<TextAsset>("RoomData/Obstacles");

		int r = Random.Range(0, obstacles.Length);
		return obstacles[r];
	}

	public RectInt Generate(World world, int seed = -1)
	{
		if (seed == -1)
			seed = Random.Range(int.MinValue, int.MaxValue);

		Random.InitState(seed);
		Debug.Log("Seed: " + seed);

		Vector2Int startRoom = MakeSolutionPath();
		SetSolutionPathRooms();

		bool pSpawned = false;

		TextAsset[][] roomData = new TextAsset[numTypes][];

		// Load room data.
		for (int i = 0; i < numTypes; i++)
			roomData[i] = Resources.LoadAll<TextAsset>("RoomData/type" + i);

		//fill in level with rooms of appropriate types
		int type;
		int room;
		Chunk chunk;

		for (int roomY = levelWidth - 1; roomY >= 0; roomY--)
		{
			for (int roomX = 0; roomX < levelHeight; roomX++)
			{
				//generate the room
				type = GetRoomType(roomX, roomY);
				room = Random.Range(0, roomData[type].Length);

				chunk = new Chunk(roomX, roomY, roomData[type][room].text);
				world.SetChunk(roomX, roomY, chunk);

				//spawn the player in a safe space if it's the starting room
				if (roomX == startRoom.x && pSpawned == false)
				{
					int playerX = 8, playerY = 8;

					int direct = 0;
					int turns = 0;
					int cDist = 0, mDist = 1;
					while (!pSpawned)
					{
						if (IsSpawnable(chunk, playerX, playerY))
						{
							player.transform.position = new Vector2(16 * roomX + playerX + 0.5f, 16 * roomY + playerY + 0.05f);
							pSpawned = true;
							EventManager.Instance.SignalEvent(GameEvent.PlayerSpawned, null);
						}
						else
						{
							//move outward in spiral pattern to find a spawnpoint close to the center
							switch (direct)
							{
								case 0: playerY++; break;
								case 1: playerX++; break;
								case 2: playerY--; break;
								case 3: playerX--; break;
							}
							cDist++;
							if (cDist == mDist)
							{
								cDist = 0;
								//turn "left"
								direct = (direct + 1) % 4;
								turns++;
								if (turns == 2)
								{
									turns = 0;
									mDist++;
								}
							}
						}
					}
				}

				if (roomX == startRoom.x && roomY == startRoom.y)
					continue;

				//generate actors in the room
				int mobTot = 0;

				for (int tileY = 0; tileY < Chunk.Size; tileY++)
				{
					for (int tileX = 0; tileX < Chunk.Size; tileX++)
					{
						//stop spawning mobs if the cap is reached
						if (mobTot >= mobCap)
							break;

						//probability a mob spawns in a given space
						int willSpawn = Random.Range(0, 100);

						if (IsSpawnable(chunk, tileX, tileY) && mobTot <= mobCap && willSpawn < 5)
						{
							int randMob = Random.Range(0, mobs.GetLength(0));
							SpawnEnemy(randMob, roomX, roomY, tileX, tileY);
							mobTot++;

						}
					}
				}
			}

			mobCap++;
		}

		AddSolidPerimeter(world);

		return new RectInt(0, 0, Chunk.Size * 4, Chunk.Size * 4);
	}

	private int GetRoomType(int roomX, int roomY)
	{
		if (roomX >= 0 && roomX < levelWidth && roomY >= 0 && roomY < levelWidth)
			return level[roomY * levelWidth + roomX];

		return -1;
	}
	
	private void SetRoomType(int roomX, int roomY, int type)
	{
		if (roomX >= 0 && roomX < levelWidth && roomY >= 0 && roomY < levelWidth)
			level[roomY * levelWidth + roomX] = type;
	}
	
	// Fills the solutionPath list with a path through the level.
	// It is guaranteed this path will be traversable. Other paths
	// could also generate off the main path by chance.
	private Vector2Int MakeSolutionPath()
	{
		int startRoomX = Random.Range(0, 4);
		int startRoomY = levelHeight - 1;

		int roomX = startRoomX;
		int roomY = startRoomY;

		PathEntry first = new PathEntry(roomX, roomY);
		solutionPath.Add(first);
		checkedRooms.Add(new Vector2Int(roomX, roomY));

		int prevIndex = 0;

		while (roomY >= 0)
		{
			PathEntry prev = solutionPath[prevIndex];
			int direction = Random.Range(0, 5);

			if (direction == 0 || direction == 1)
			{
				if (roomX > 0 && !checkedRooms.Contains(new Vector2Int(roomX - 1, roomY)))
					AddLeft();
				else AddDown();
			}
			else if (direction == 2 || direction == 3)
			{
				if (roomX < levelWidth - 1 && !checkedRooms.Contains(new Vector2Int(roomX + 1, roomY)))
					AddRight();
				else AddDown();
			}
			else AddDown();

			solutionPath[prevIndex++] = prev;

			void AddLeft()
			{
				--roomX;
				prev.dirs[Left] = true;

				PathEntry next = new PathEntry(roomX, roomY);
				next.dirs[Right] = true;

				solutionPath.Add(next);
				checkedRooms.Add(new Vector2Int(roomX, roomY));
			}

			void AddRight()
			{
				++roomX;
				prev.dirs[Right] = true;

				PathEntry next = new PathEntry(roomX, roomY);
				next.dirs[Left] = true;

				solutionPath.Add(next);
				checkedRooms.Add(new Vector2Int(roomX, roomY));
			}

			void AddDown()
			{
				--roomY;
				prev.dirs[Down] = true;

				PathEntry next = new PathEntry(roomX, roomY);
				next.dirs[Up] = true;

				solutionPath.Add(next);
				checkedRooms.Add(new Vector2Int(roomX, roomY));
			}
		}

		return new Vector2Int(startRoomX, startRoomY);
	}

	// Given a filled solution path, sets the room types
	// along the path randomly according to what is possible.
	// This method is not very efficient, though it won't matter
	// for our purposes currently. If it later does, we can optimize 
	// it then.
	private void SetSolutionPathRooms()
	{
		for (int i = 0; i < solutionPath.Count; ++i)
		{
			PathEntry entry = solutionPath[i];

			HashSet<int> options = null;

			// Go through each direction and add all types
			// that are supported by all directions we need
			// to support for this room.
			for (int j = 0; j < 4; ++j)
			{
				if (entry.dirs[j])
				{
					if (options == null)
						options = roomOptions[j];
					else options.IntersectWith(roomOptions[j]);
				}
			}

			int[] arr = new int[options.Count];
			options.CopyTo(arr);

			int choice = Random.Range(0, arr.Length);
			SetRoomType(entry.x, entry.y, arr[choice]);
		}
	}

	private bool IsSpawnable(Chunk chunk, int tileX, int tileY)
	{
		// We ensure tileY is larger than 0 for now to ensure the tileY - 1 check doesn't
		// go out of bounds. This prevents enemies from spawning on the bottom row of the room.
		// We can change this fairly easily if we care.
		if (tileY > 0)
		{
			bool passable = TileManager.GetData(chunk.GetTile(tileX, tileY)).passable;
			bool passableBelow = TileManager.GetData(chunk.GetTile(tileX, tileY - 1)).passable;

			if (passable && !passableBelow)
				return true;
		}
		
		return false;
	}

	private void SpawnEnemy(int num, int roomX, int row, int tileX, int tileY)
	{
		Entity entity = Object.Instantiate(mobs[num]).GetComponent<Entity>();
		float yOffset = entity.useCenterPivot ? 0.55f : 0.05f;

		// Room position * Chunk.Size gets the world position of the room's corner. The tile position 
		// determines the offset into the room. yOffset prevents clipping into walls on spawn.
		Object.Instantiate(mobs[num], new Vector2(roomX * Chunk.Size + tileX + 0.5f, row * 16 + tileY + yOffset), Quaternion.identity);
	}

	// Adds a solid-filled room around the outside of the map.
	private void AddSolidPerimeter(World world)
	{
		TextAsset data = Resources.Load<TextAsset>("RoomData/Solid");

		for (int y = -1; y < levelHeight + 1; ++y)
		{
			Chunk left = new Chunk(-1, y, data.text);
			Chunk right = new Chunk(levelWidth, y, data.text);

			world.SetChunk(-1, y, left);
			world.SetChunk(levelWidth, y, right);
		}

		for (int x = 0; x < levelWidth; ++x)
		{
			Chunk bottom = new Chunk(x, -1, data.text);
			Chunk top = new Chunk(x, levelHeight, data.text);

			world.SetChunk(x, -1, bottom);
			world.SetChunk(x, levelHeight, top);
		}
	}
}
