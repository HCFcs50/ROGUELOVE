using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WalkerGenerator : MonoBehaviour
{
    // TILE TYPES
    public enum Grid {
        FLOOR, EMPTY, OBSTACLES, DECOR
    }

    // 2D Array
    public Grid[,] gridHandler;

    public List<int> tileListX = new List<int>();

    public List<int> tileListY = new List<int>();

    // List of all active Walkers
    public List<WalkerObject> walkers;

    [Header("TILEMAP OBJECTS")]

    // TILEMAP REFERENCES
    public Tilemap tilemap;

    public Tilemap oTilemap;

    public RuleTile floor;

    public Sprite ground;

    public RuleTile obstacles;

    public RuleTile decor;

    public Tile empty;

    [Space(10)]

    [Header("MAP SETTINGS")]

    // Map maximum width <-->
    public int mapWidth = 40;

    // Map maximum height ^v
    public int mapHeight = 40;

    // Maximum amount of active walkers
    public int maxWalkers = 10;

    // Current tile count
    public int tileCount = default;

    // Current obstacle tile count
    public int oTileCount = default;

    // Compares the amount of floor tiles to the percentage of the total grid covered (in floor tiles)
    // NEVER SET THIS TO 1 OR ELSE UNITY WILL CRASH :skull:
    public float fillPercentage = 0.6f;

    // Generation method, pause time between each successful movement
    public float waitTime = 0.005f;

    private AstarPath astarPath;

    //private int tileLocX;
    //private int tileLocY;

    [Space(10)]
    [Header("ENEMIES")]

    public GameObject[] enemies;



    // Initializes grid to be generated (size)
    void Start() {
        InitializeGrid();
    }

    void InitializeGrid() {

        // Initializes grid size from variables
        gridHandler = new Grid[mapWidth, mapHeight];

        // Loops through grid size and sets every tile to EMPTY tile
        for (int x = 0; x < gridHandler.GetLength(0); x++) {
            for (int y = 0; y < gridHandler.GetLength(1); y++) {
                gridHandler[x,y] = Grid.EMPTY;
            }
        }

        // New instance of the WalkerObject list
        walkers = new List<WalkerObject>();

        // Creates reference of the exact centerpiece of the tilemap
        Vector3Int tileCenter = new(0, 0, 0);

        // Creates a new walker, and sets initial values
        WalkerObject currWalker = new WalkerObject(new Vector2(tileCenter.x, tileCenter.y), GetDirection(), 0.5f);
        // Sets current grid location to floor
        gridHandler[tileCenter.x, tileCenter.y] = Grid.FLOOR;
        tilemap.SetTile(tileCenter, floor);
        //tilemapObstacles.SetTile(tileCenter, floor);
        // Adds current walker to Walker list
        walkers.Add(currWalker);

        // Increases total tile count
        tileCount++;

        // Handles walker rules
        StartCoroutine(CreateFloors());
    }


    // Returns a random single vector direction
    Vector2 GetDirection() {
        int choice = Mathf.FloorToInt(UnityEngine.Random.value * 3.99f);

        switch (choice) {
            case 0:
                return Vector2.down;
            case 1:
                return Vector2.left;
            case 2:
                return Vector2.up;
            case 3:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }

    IEnumerator CreateFloors() {
        // Compares tile count as a float to the total size of the grid, and will continue looping as long as it is
        // less than the fillPercentage value set earlier
        while ((float)tileCount / (float)gridHandler.Length < fillPercentage) {
            
            // Yield return value for coroutine
            bool hasCreatedFloor = false;
            
            // Loops through every walker in the list, and creates a reference to its current position to
            // check if it is a FLOOR piece
            foreach (WalkerObject currWalker in walkers) {

                Vector3Int currPos = new Vector3Int((int)currWalker.position.x, (int)currWalker.position.y, 0);
                Vector3Int currPos2 = new Vector3Int((int)currWalker.position.x - 1, (int)currWalker.position.y, 0);
                Vector3Int currPos3 = new Vector3Int((int)currWalker.position.x, (int)currWalker.position.y - 1, 0);
                Vector3Int currPos4 = new Vector3Int((int)currWalker.position.x - 1, (int)currWalker.position.y - 1, 0);

                // If ^ is not, then set a new FLOOR tile in that position and increment the total tile count
                if (gridHandler[currPos.x, currPos.y] != Grid.FLOOR) {
                    tilemap.SetTile(currPos, floor);
                    tileListX.Add(currPos.x);
                    tileListY.Add(currPos.y);
                    tilemap.SetTile(currPos2, floor);
                    tileListX.Add(currPos2.x);
                    tileListY.Add(currPos2.y);
                    tilemap.SetTile(currPos3, floor);
                    tileListX.Add(currPos3.x);
                    tileListY.Add(currPos3.y);
                    tilemap.SetTile(currPos4, floor);
                    tileListX.Add(currPos4.x);
                    tileListY.Add(currPos4.y);
                    tileCount++;
                    tileCount++;
                    tileCount++;
                    gridHandler[currPos.x, currPos.y] = Grid.FLOOR;
                    gridHandler[currPos.x - 1, currPos.y] = Grid.FLOOR;
                    gridHandler[currPos.x, currPos.y - 1] = Grid.FLOOR;
                    gridHandler[currPos.x - 1, currPos.y - 1] = Grid.FLOOR;
                    //Debug.Log(gridHandler.Length);
                    hasCreatedFloor = true;
                }
            }

            // Walker methods
            ChanceToRemove();
            ChanceToRedirect();
            ChanceToCreate();
            UpdatePosition();

            if (hasCreatedFloor) {
                yield return new WaitForSeconds(waitTime);
            }
        }

        StartCoroutine(CreateObstacles());
        
        tilemap.SetTile(new Vector3Int(0, 0, 0), empty);
        tileCount--;
        gridHandler[0, 0] = Grid.EMPTY;
        StartCoroutine(CreateDecor());
    }

    // Loops through walker list, randomly compares a value to the WalkersChance, and if it's > 1 then 
    // deactivates walker if true
    void ChanceToRemove() {
        int updatedCount = walkers.Count;
        for (int i = 0; i < updatedCount; i++) {
            if (UnityEngine.Random.value < walkers[i].chanceToChange && walkers.Count > 1) {
                walkers.RemoveAt(i);
                break;
            }
        }
    }

    // ^^ same thing but updates direction instead of deletes
    void ChanceToRedirect() {
        for (int i = 0; i < walkers.Count; i++) {
            if (UnityEngine.Random.value < walkers[i].chanceToChange) {
                WalkerObject currWalker = walkers[i];
                currWalker.direction = GetDirection();
                walkers[i] = currWalker;
            }
        }
    }

    // ^^ same thing but creates a new walker (duplicates at position) as long as it is under max walkers
    void ChanceToCreate() {
        int updatedCount = walkers.Count;
        for (int i = 0; i < updatedCount; i++) {
            if (UnityEngine.Random.value < walkers[i].chanceToChange && walkers.Count < maxWalkers) {
                Vector2 newDirection = GetDirection();
                Vector2 newPosition = walkers[i].position;

                WalkerObject newWalker = new WalkerObject(newPosition, newDirection, 0.5f);
                walkers.Add(newWalker);
            }
        }
    }

    // Update actual position of the walkers, within the bounds of the grid size
    void UpdatePosition() {
        for (int i = 0; i < walkers.Count; i++) {
            WalkerObject foundWalker = walkers[i];
            foundWalker.position += foundWalker.direction;
            foundWalker.position.x = Mathf.Clamp(foundWalker.position.x, 1, gridHandler.GetLength(0) - 2);
            foundWalker.position.y = Mathf.Clamp(foundWalker.position.y, 1, gridHandler.GetLength(1) - 2);
            walkers[i] = foundWalker;
        }
    }

    // CREATES OBSTACLES
    IEnumerator CreateObstacles() {

        for (int x = 0; x < gridHandler.GetLength(0) - 1; x++) {
            for (int y = 0; y < gridHandler.GetLength(1) - 1; y++) {
                int rand = UnityEngine.Random.Range(0, 30);

                // Checks each x and y value of the grid to see if they are floors
                if (gridHandler[x, y] == Grid.FLOOR) {
                    bool hasCreatedObstacle = false;

                    // SINGLE FENCE CHECK
                    if (gridHandler[x, y] == Grid.FLOOR && rand == 0) {
                        oTilemap.SetTile(new Vector3Int(x, y, 0), obstacles);
                        gridHandler[x, y] = Grid.OBSTACLES;
                        hasCreatedObstacle = true;
                        oTileCount++;
                    }

                    if (hasCreatedObstacle) {
                        yield return new WaitForSeconds(waitTime);
                    }
                }
            }
        }
    }

    IEnumerator CreateDecor() {

        for (int x = 0; x < gridHandler.GetLength(0) - 1; x++) {
            for (int y = 0; y < gridHandler.GetLength(1) - 1; y++) {

                // Checks each x and y value of the grid to see if they are floors
                if (gridHandler[x, y] == Grid.EMPTY) {
                    bool hasCreatedDecor = false;

                    // SINGLE FENCE CHECK
                    tilemap.SetTile(new Vector3Int(x, y, 0), decor);
                    gridHandler[x, y] = Grid.DECOR;
                    hasCreatedDecor = true;
                    tileCount++;

                    if (hasCreatedDecor) {
                        yield return new WaitForSeconds(waitTime);
                    }
                }
            }
        }
        SpawnRandom();
        PathScan();
    }

    private void PathScan() {
        astarPath = FindFirstObjectByType<AstarPath>();
        AstarData data = AstarPath.active.data;
        GridGraph gg = data.gridGraph;

        // Gets the distance from 0,0 on the x axis (half of the total tilemap size)
        float radiusX = (0.16f * (mapWidth - 1f)) / 2f;
        // Gets the distance from 0,0 on the y axis (half of the total tilemap size)
        float radiusY = (0.16f * (mapHeight - 1f)) / 2f;

        gg.SetDimensions((mapWidth - 1) * 4, (mapHeight - 1) * 4, 0.04f);
        gg.center = new Vector3(radiusX, radiusY, 0);

        AstarPath.active.Scan();
    }

    void SpawnRandom() {

        // Finds player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // For every enemy in the level
        for (int e = 0; e < enemies.Length; e++) {

            // Generates random number to pick Enemy spawnpoint
            int rand = Random.Range(0, tileListX.Count);
            // Generates random number to pick Player spawnpoint
            int randP = Random.Range(0, tileListX.Count);

            // For as many floor tiles as there are in the tilemap:
            for (int i = 0; i < tileListX.Count; i++) {

                // If suitable floor tiles have been found
                if ((tilemap.GetSprite(new Vector3Int(tileListX[rand], tileListY[rand], 0)) == ground) 
                && (tilemap.GetSprite(new Vector3Int(tileListX[randP], tileListY[randP], 0)) == ground)
                && (oTilemap.GetTile(new Vector3Int(tileListX[rand], tileListY[rand], 0)) != obstacles)
                && (oTilemap.GetTile(new Vector3Int(tileListX[randP], tileListY[randP], 0)) != obstacles)) {

                    Debug.Log("true!");
                    
                    // Spawns Enemy
                    Instantiate(enemies[e], new Vector2(tileListX[rand] * 0.16f, tileListY[rand] * 0.16f), Quaternion.identity);
                    // Spawns Player
                    player.transform.position = new Vector2(tileListX[randP] * 0.16f, tileListY[randP] * 0.16f);
                    
                    break;

                } else {
                    
                    // Generates random number to pick Enemy spawnpoint
                    rand = Random.Range(0, tileListX.Count);
                    // Generates random number to pick Player spawnpoint
                    randP = Random.Range(0, tileListX.Count);

                    Debug.Log("false");
                    Debug.Log("slime " + tilemap.GetSprite(new Vector3Int(tileListX[rand], tileListY[rand], 0)));
                    Debug.Log("player " + tilemap.GetSprite(new Vector3Int(tileListX[randP], tileListY[randP], 0)));
                }

            }
        }
    }
}
