using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// Handles the creation, management, and gameplay logic of the Match-3 game board.
/// Controls cube spawning, matching, swapping, cascades, special effects, and win/lose conditions.
/// </summary>
public class GameBoard : MonoBehaviour
{
    // Singleton instance
    public static GameBoard Instance { get; private set; }

    // Board dimensions and tiles
    private int width;
    private int height;
    private BoardTile[,] boardTiles;

    // The cube that the player selects
    private Cube selectedCube;
    // The cube that will be made special after a 4 group match
    private Cube cubeToMakeSpecial = null;

    // Flags for game state
    private bool isSwapping = false;
    private bool glowTriggered = false;

    // Count of white cubes on the board
    private int whiteCubeCount = 0;

    // Flag to check if there are any white cubes on the board
    public bool HasAnyWhiteCubes => whiteCubeCount > 0;
    // Flag to track if the final bonus effect has been triggered
    private bool finalBonusTriggered = false;

    [Header("UI References")]
    [Tooltip("UI Helper to prevent any clicks when the pointer is on top of any UI element.")]
    [SerializeField] private UIHelper uiHelper;
    [Tooltip("Reference to the Progress Bar.")]
    [SerializeField] private ProgressBarManager progressBarManager;

    /// <summary>
    /// Sets the singleton instance of GameBoard.
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Sets up the game board at the start of the game.
    /// </summary>
    private void Start()
    {
        InitializeBoard();
    }

    /// <summary>
    /// Checks for user input to select and swap cubes.
    /// </summary>
    private void Update()
    {
        // If the pointer is over any UI element, do not process input
        if (uiHelper.IsPointerOverUI()) return;

        // Check for mouse left button click
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the camera to the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            // If the ray hit a collider and it is a Cube
            if (hit.collider != null && hit.collider.TryGetComponent(out Cube cube))
            {
                // Check if a swap is in progress or if ParticleManager is null or if all effects are not done, if so, return
                if (isSwapping || ParticleManager.Instance == null || !ParticleManager.Instance.AreAllEffectsDone())
                    return;
                // Otherwise, select the cube and swap it, or deselect it if it is already selected
                SelectCubeAndSwap(cube);
            }
        }
    }

    /// <summary>
    /// Itinializes the game board by setting up the tiles based on the level layout.
    /// </summary>
    private void InitializeBoard()
    {
        // Reset the game state
        whiteCubeCount = 0;
        finalBonusTriggered = false;
        glowTriggered = false;

        // Get the width and height of the grid from LevelManager
        width = LevelManager.Instance.GetGridWidth();
        height = LevelManager.Instance.GetGridHeight();

        // Set the spacing for the board tiles
        float xSpacing = (float)(width - 1) / 2;
        float ySpacing = (float)(height - 1) / 2;

        // Create a 2D array of BoardTile objects
        boardTiles = new BoardTile[width, height];
        // Get the board layout from LevelManager
        List<string> layout = LevelManager.Instance.GetBoardLayout();

        // Loop through the layout and instantiate the board tiles starting from the rows
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                // Calculate the index for the layout
                int index = j * width + i;
                // Get the token for the current tile (i.e. what object to be places in the tile)
                string token = layout[index];
                // Set the position based on the spacing
                Vector2 pos = new Vector2(i - xSpacing, j - ySpacing);
                // Spawn the board object from the object pool using the token
                GameObject boardObject = ObjectPoolManager.SpawnObjectByKey(GetPoolKey(token), pos);
                // Set the indices for the board object
                boardObject.GetComponent<BoardObject>().SetIndices(i, j);
                // Initialize the board tile with the indices, filled state, and the newly spawned board object
                boardTiles[i, j] = new BoardTile(i, j, false, boardObject);
                // Check if the board object is a Cube and if it is white, increment the white cube count
                if (boardObject.GetComponent<Cube>() && boardObject.GetComponent<Cube>().GetColor() == CubeColor.White)
                    GameBoard.Instance.IncrementWhiteCubeCount();
            }
        }
        // Decrease the scale of the board and everything inside it to fit the screen
        transform.localScale = new Vector3(0.5f, 0.5f, 1);
    }

    /// <summary>
    /// Returns the pool key for the given board object token.
    /// </summary>
    /// <param name="token">The board object token to get the key from.</param>
    private string GetPoolKey(string token)
    {
        return token switch
        {
            "bl" => "BlackCube",
            "b" => "BlueCube",
            "r" => "RedCube",
            "y" => "YellowCube",
            "w" => "WhiteCube",
            "p" => "PrismObstacle",
            "s" => "StoneObstacle",
            _ => null,
        };
    }

    /// <summary>
    /// Returns the board tile at the specified coordinates.
    /// </summary>
    /// <param name="x">The x coordinate on the board.</param>
    /// <param name="y">The y coordinate on the board.</param>
    public BoardTile GetBoardTileAt(int x, int y)
    {
        // Check if the coordinates are within the bounds of the board, if so return the board tile, otherwise return null
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return boardTiles[x, y];
    }

    /// <summary>
    /// Increases the count of white cubes on the board by one.
    /// </summary>
    public void IncrementWhiteCubeCount()
    {
        whiteCubeCount++;
    }

    /// <summary>
    /// Decreases the count of white cubes on the board by one, ensuring it does not go below zero.
    /// </summary>
    public void DecrementWhiteCubeCount()
    {
        whiteCubeCount = Mathf.Max(0, whiteCubeCount - 1);
    }

    /// <summary>
    /// Finds the world position of a tile given its indices on the board.
    /// </summary>
    /// <param name="x">The x coordinate on the board.</param>
    /// <param name="y">The y coordinate on the board.</param>
    public Vector2 FindPositionsGivenIndices(int x, int y)
    {
        float xPos = x - ((width / 2f) - 0.5f);
        float yPos = y - ((height / 2f) - 0.5f);
        return new Vector2(xPos, yPos);
    }

    /// <summary>
    /// Loops through the board tiles and checks if all cubes have finished moving.
    /// Returns true if all cubes are not moving, false otherwise.
    /// </summary>
    public bool AllCubesFinishedMoving()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Get the object inside the board tile
                GameObject obj = boardTiles[x, y].GetObjectInside();
                // If the object is not null and is a Cube, check if it is moving, if it does, return false
                if (obj != null && obj.TryGetComponent<Cube>(out Cube cube) &&
                    obj.GetComponent<Cube>().IsMoving)
                    return false;
            }
        }
        // If no cubes are moving, return true
        return true;
    }

    /// <summary>
    /// Checks if the given cube has at least 2 adjacent cubes that are matching color wise,
    /// and registers them if found.
    /// </summary>
    /// <param name="cube">The cube to be checked for at least 2 matches.</param>
    private bool CheckForMatches(Cube cube)
    {
        // Clear the match list of the cube to ensure no previous matches are registered
        cube.ClearMatches();

        // Create lists to hold horizontal and vertical matches
        List<Cube> horizontal = new List<Cube>();
        List<Cube> vertical = new List<Cube>();

        // Create visited sets to avoid infinite loops
        HashSet<Cube> visitedH = new HashSet<Cube> { cube };
        HashSet<Cube> visitedV = new HashSet<Cube> { cube };

        // Check all four directions (left, right, up, down) for matches
        CheckDirection(cube, Vector2.left, horizontal, visitedH);
        CheckDirection(cube, Vector2.right, horizontal, visitedH);
        CheckDirection(cube, Vector2.up, vertical, visitedV);
        CheckDirection(cube, Vector2.down, vertical, visitedV);

        // Set the initial flag for whether any matches were found as false
        bool matched = false;

        // If there are two or more horizontal matches, register them
        if (horizontal.Count >= 2)
        {
            matched = true;
            cube.RegisterHorizontalMatches(horizontal);
        }

        // If there are two or more vertical matches, register them
        if (vertical.Count >= 2)
        {
            matched = true;
            cube.RegisterVerticalMatches(vertical);
        }
        // Return true if at least more than 2 matches were found, false otherwise
        return matched;
    }

    /// <summary>
    /// Checks the specified direction from the origin cube for matching cubes.
    /// If a matching cube is found, it recursively checks in the same direction.
    /// </summary>
    /// <param name="origin">The cube to start checking for any matches around it.</param>
    /// <param name="dir">The direction to check for matches.</param>
    /// <param name="matches">The matched cube group list of the given origin cube.</param>
    /// <param name="visited">The cubes that were already visited while checking for matches.</param>
    private void CheckDirection(Cube origin, Vector2 dir, List<Cube> matches, HashSet<Cube> visited)
    {
        // Set the x and y coordinates of the origin cube
        int x = origin.GetX();
        int y = origin.GetY();
        // Set the new x and y coordinates based on the direction
        int newX = x + (int)dir.x;
        int newY = y + (int)dir.y;

        // Check if the new coordinates are within bounds and if the tile is not empty
        if (newX >= 0 && newX < width && newY >= 0 && newY < height && !boardTiles[newX, newY].GetEmpty())
        {
            // Get the object inside the board tile at the new coordinates
            GameObject nextObj = boardTiles[newX, newY].GetObjectInside();
            // Check if the object is not null and if it is a Cube
            if (nextObj != null && nextObj.TryGetComponent<Cube>(out Cube cube))
            {
                Cube nextCube = nextObj.GetComponent<Cube>();
                // Check if the next cube has the same color as the origin cube and is not previously visited
                if (nextCube.GetColor() == origin.GetColor() && !visited.Contains(nextCube))
                {
                    // If it has the same color and has not been visited, add it to the visited set and matches list
                    visited.Add(nextCube);
                    matches.Add(nextCube);
                    // And continue checking in the same direction recursively using the new cube as the origin
                    CheckDirection(nextCube, dir, matches, visited);
                }
            }
        }
    }

    /// <summary>
    /// Finds the center cube from the given cube group and returns its position.
    /// </summary>
    /// <param name="cubes">The list of cubes that are in the matched group.</param>
    /// <param name="numberOfMatches">The number of matched cubes in the group.</param>
    private Vector2 FindCenterCubeIndices(List<Cube> cubes, int numberOfMatches)
    {
        // Loop through the cubes to find if any cube has a MatchType of Both
        foreach (Cube c in cubes)
        {
            if (c.GetMatchType() == MatchType.Both)
            {
                // If there is a cube that has a MatchType of Both, return its position immediately
                return new Vector2(c.GetX(), c.GetY());
            }
        }

        // Otherwise, sort cubes to stabilize order (left to right, bottom to top)
        cubes.Sort((a, b) =>
        {
            if (a.GetY() == b.GetY())
                return a.GetX().CompareTo(b.GetX());
            return a.GetY().CompareTo(b.GetY());
        });

        // Calculate the index of the center cube based on the number of matches
        // If the number of matches is even, the center is between two cubes, so we take the left one
        int spawnIndex = (numberOfMatches % 2 == 0) ? (numberOfMatches / 2) - 1 : numberOfMatches / 2;
        // Get the center cube from the sorted list of cubes
        Cube centerCube = cubes[spawnIndex];
        // Return the position of the center cube as a Vector2
        int x = centerCube.GetX();
        int y = centerCube.GetY();
        return new Vector2(x, y);
    }

    /// <summary>
    /// Selects a cube if no cube were previously selected. Swaps the previously selected cube
    /// and the currently selected cube if they are different and adjacent.
    /// Deselects the currently selected cube if it is the same as the previously selected cube.
    /// </summary>
    /// <param name="cube">The cube to player has selected.</param>
    private void SelectCubeAndSwap(Cube cube)
    {
        // Check if the selectedCube is null, if so, set it to the currently selected cube
        if (selectedCube == null)
        {
            selectedCube = cube;
        }
        // Check if the selectedCube is the same as the currently selected cube, if so, deselect it
        else if (selectedCube == cube)
        {
            selectedCube = null;
        }
        // Otherwise, check if the selectedCube and the currently selected cube are different and adjacent
        else
        {
            if (IsAdjacent(selectedCube, cube))
            {
                // If they are adjacent, start the swap coroutine
                StartCoroutine(ProcessSwap(selectedCube, cube));
            }
            // After the swap, reset the selected cube to null
            selectedCube = null;
        }
    }

    /// <summary>
    /// Returns true if the two cubes are adjacent on the board, false otherwise.
    /// </summary>
    /// <param name="a">The first cube on the board.</param>
    /// <param name="b">The second cube on the board.</param>
    private bool IsAdjacent(Cube a, Cube b)
    {
        int dx = Mathf.Abs(a.GetX() - b.GetX());
        int dy = Mathf.Abs(a.GetY() - b.GetY());
        return dx + dy == 1;
    }

    /// <summary>
    /// Returns true if the two cubes are adjacent on the board, false otherwise.
    /// </summary>
    /// <param name="a">The first cube on the board.</param>
    /// <param name="b">The second cube on the board.</param>
    private IEnumerator ProcessSwap(Cube a, Cube b)
    {
        // Sets a swap flag to true to prevent further swaps while this one is in progress
        isSwapping = true;

        // Visually swap the cubes on the board
        SwapVisuals(a, b);

        // Then wait until both cubes are not moving
        yield return new WaitUntil(() => !a.IsMoving && !b.IsMoving);

        // Check if either cube is a WhiteCube and handle the special case
        bool aIsWhite = a.GetColor() == CubeColor.White;
        bool bIsWhite = b.GetColor() == CubeColor.White;

        // Reset the combo count before processing any matches
        ComboManager.Instance.ResetCombo();

        // If both cubes are white, handle the double white cube destruction
        if (aIsWhite && bIsWhite)
        {
            yield return StartCoroutine(((WhiteCube)a).DoubleWhiteCubeDestruction(b));
            yield return HandlePostMatch();
            yield break;
        }

        // If one of the cubes is white, activate the color clear effect
        if (aIsWhite)
        {
            yield return StartCoroutine(((WhiteCube)a).ActivateColorClear(b.GetColor()));
            yield return HandlePostMatch();
            yield break;
        }
        if (bIsWhite)
        {
            yield return StartCoroutine(((WhiteCube)b).ActivateColorClear(a.GetColor()));
            yield return HandlePostMatch();
            yield break;
        }

        // If neither cube is white, check for matches
        bool aMatched = CheckForMatches(a);
        bool bMatched = CheckForMatches(b);

        // If both cubes matched, check if one of them is a special cube
        if (aMatched || bMatched)
        {
            // If one of the cubes created a matched group of 4, set it to be made special
            if (a.GetMatchGroupSize() == 4) cubeToMakeSpecial = a;
            else if (b.GetMatchGroupSize() == 4) cubeToMakeSpecial = b;

            // Decrease the move count and run the cascade loop
            LevelManager.Instance.DecreaseMoveCount();
            yield return StartCoroutine(RunCascadeLoop());
        }
        // If neither cube matched, just swap them back and wait for them to finish moving
        else
        {
            SwapVisuals(a, b);
            yield return new WaitUntil(() => !a.IsMoving && !b.IsMoving);
        }

        // After the swap process, set the swap flag to false to enable new swaps
        isSwapping = false;
    }

    /// <summary>
    /// Handles the post-match logic, such as dropping cubes and spawning new ones and resetting swap flag at the end.
    /// </summary>
    private IEnumerator HandlePostMatch()
    {
        // Decrease the move count
        LevelManager.Instance.DecreaseMoveCount();
        // Find the empty spots on the board to refill
        List<Vector2> refillSpots = DropCubes();
        // Spawn new cubes at the refill spots and drop them to their positions
        SpawnNewCubesAndDrop(refillSpots);
        // Run the cascade loop to handle any matches that might occur after the new cubes are spawned
        yield return StartCoroutine(RunCascadeLoop());
        // After it all cascades are done, reset the swap flag to allow new swaps
        isSwapping = false;
    }

    /// <summary>
    /// Swaps the cube positions both inside the tiles and visually on the board.
    /// </summary>
    /// <param name="a">The first cube on the board.</param>
    /// <param name="b">The second cube on the board.</param>
    private void SwapVisuals(Cube a, Cube b)
    {
        // Get the coordinates of the cubes
        int ax = a.GetX(), ay = a.GetY();
        int bx = b.GetX(), by = b.GetY();

        // Put the cubes inside the opposite tiles
        boardTiles[ax, ay].SetObjectInside(b.gameObject);
        boardTiles[bx, by].SetObjectInside(a.gameObject);

        // Then move the cubes to their new positions
        a.MoveToPositionAndUpdateIndices(bx, by, FindPositionsGivenIndices(bx, by));
        b.MoveToPositionAndUpdateIndices(ax, ay, FindPositionsGivenIndices(ax, ay));
    }

    /// <summary>
    /// Swaps the cube positions both inside the tiles and visually on the board.
    /// </summary>
    private IEnumerator RunCascadeLoop()
    {
        // Loop until breaking condition is met
        while (true)
        {
            // Wait until all cubes are not moving
            yield return new WaitUntil(() => AllCubesFinishedMoving());

            // Reset the five plus effect flag in ParticleManager to play it only once per cascade loop
            ParticleManager.Instance.ResetFivePlusEffectFlag();

            // Find the best match group of cubes on the board to destroy
            List<Cube> matchGroup = FindBestMatchGroup();

            // If the match group is null or has less than 3 cubes, break the loop
            if (matchGroup == null || matchGroup.Count < 3)
                break;

            // Otherwise, check if all the targets for the current level are destroyed
            if (LevelManager.Instance.CheckIfAllTargetsAreDestroyed())
            {
                // If all targets are destroyed and the Progress Bar's glow has not been triggered yet,
                if (!glowTriggered)
                {
                    // Trigger the glow effect on the Progress Bar
                    glowTriggered = true;
                    progressBarManager.TriggerPostClearGlow();
                }
                // If the Progress Bar glow effect has already been triggered,
                else
                {
                    // Pulse the Progress Bar to indicate an additional match
                    progressBarManager.TriggerChargedPulse();
                }
            }

            // Handle the match group by starting the coroutine to process it
            yield return StartCoroutine(HandleMatch(matchGroup));

            // Find the empty spots on the board to refill
            List<Vector2> refillSpots = DropCubes();
            // Spawn new cubes at the refill spots and drop them to their positions
            SpawnNewCubesAndDrop(refillSpots);

            // Wait until all cubes are not moving and all particle effects are done
            yield return new WaitUntil(() => AllCubesFinishedMoving());
            yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());
        }

        // At the end of the cascade loop, check if the win or lose conditions are met
        yield return StartCoroutine(CheckWinOrLose());
    }

    /// <summary>
    /// Drops Cubes or Prisms above to the empty tiles below them, and returns a list of refill spots that
    /// are opened after the drop.
    /// </summary>
    private List<Vector2> DropCubes()
    {
        // Create a list to hold the refill spots
        List<Vector2> refillSpots = new List<Vector2>();

        // Loop through each column of the board
        for (int x = 0; x < width; x++)
        {
            // Reset the empty count for the current column
            int emptyCount = 0;

            // Loop through each row of the current column
            for (int y = 0; y < height; y++)
            {
                // If the current tile is empty, increment the empty count
                if (boardTiles[x, y].GetEmpty())
                {
                    emptyCount++;
                }
                // If the number of empty tiles is greater than 0,
                else if (emptyCount > 0)
                {
                    // Get the object inside the current tile
                    GameObject obj = boardTiles[x, y].GetObjectInside();

                    // If the object is null or is an Obstacle that should not fall, reset the empty count and continue
                    if (obj != null && obj.TryGetComponent<Obstacle>(out Obstacle obstacle) && !obstacle.ShouldFall())
                    {
                        // Obstacle like Stone should not move, reset emptyCount because it blocks further fall
                        emptyCount = 0;
                        continue;
                    }

                    // Otherwise, update the new y index based on the empty count in the column
                    int newY = y - emptyCount;
                    // Find the new position for the object based on the new indices
                    Vector2 newPos = FindPositionsGivenIndices(x, newY);

                    // Update the board tile at the new position with the object
                    boardTiles[x, newY].SetObjectInside(obj);

                    // Update the board tile at the old position to be empty
                    boardTiles[x, y].RemoveObjectInside();

                    // If the object is a Cube or Obstacle, drop it to the new position
                    // (kept different if statements for future change possibility)
                    if (obj != null && obj.TryGetComponent<Cube>(out Cube cube))
                        cube.MoveToPositionAndUpdateIndices(x, newY, newPos);
                    else if (obj != null && obj.TryGetComponent<Obstacle>(out Obstacle obs))
                        obs.MoveToPositionAndUpdateIndices(x, newY, newPos);
                }
            }

            // Identify empty top cells to refill
            for (int y = height - emptyCount; y < height; y++)
            {
                refillSpots.Add(new Vector2(x, y));
            }
        }

        // Return the list of refill spots where new cubes should be spawned
        return refillSpots;
    }

    /// <summary>
    /// Spawns new cubes at the top of the board and drops them at the specified refill spots.
    /// </summary>
    /// <param name="refillSpots">The spots on the board that are empty and need to be refilled.</param>
    private void SpawnNewCubesAndDrop(List<Vector2> refillSpots)
    {
        // Loop through each refill spot
        foreach (var pos in refillSpots)
        {
            // Set the x and y indices from the refill spot position
            int x = (int)pos.x;
            int y = (int)pos.y;
            // Find the world position for the spawn
            Vector2 spawnPos = FindPositionsGivenIndices(x, height + 2);

            // Get a random cube to spawn at the refill spot
            GameObject cube = SpawnRandomCube(x, y, spawnPos);
            // Find the target position for the cube based on its indices
            Vector2 targetPos = FindPositionsGivenIndices(x, y);
            // Move the newly spawned cube to the target position
            cube.GetComponent<Cube>().MoveToPosition(targetPos);

            // Update the board tile with the new cube
            boardTiles[x, y].SetObjectInside(cube);
        }
    }

    /// <summary>
    /// Spawns a random cube at the specified position on the board.
    /// </summary>
    /// <param name="x">The x coordinate on the board.</param>
    /// <param name="y">The y coordinate on the board.</param>
    /// <param name="spawnPos">The spawn position of the new cube to spawn.</param>
    private GameObject SpawnRandomCube(int x, int y, Vector2 spawnPos)
    {
        // All the possible cube keys (i.e. colors) to spawn
        string[] keys = { "BlackCube", "BlueCube", "RedCube", "YellowCube" };
        // Randoly select one of the keys/colors
        string selected = keys[Random.Range(0, keys.Length)];
        // Spawn the cube from the object pool using the selected key and position
        GameObject cube = ObjectPoolManager.SpawnObjectByKey(selected, spawnPos);
        // Initialize the new cube with the specified indices and position to update the board tile
        InitializeNewCube(cube, x, y, spawnPos);
        // Then, return the newly spawned cube
        return cube;
    }

    /// <summary>
    /// Initializes a new cube at the specified position on the board.
    /// </summary>
    /// <param name="cube">The Game Object of the cube to initialize.</param>
    /// <param name="x">The x coordinate of the cube on the board.</param>
    /// <param name="y">The y coordinate of the cube on the board.</param>
    /// <param name="spawnPos">The spawn position of the cube.</param>
    public void InitializeNewCube(GameObject cube, int x, int y, Vector2 pos)
    {
        // Make sure cube is active
        if (!cube.activeSelf)
            cube.SetActive(true);

        // Set the parent, position, and scale of the new cube
        cube.transform.SetParent(this.transform);
        cube.transform.localPosition = pos;
        cube.transform.localScale = Vector3.one;

        // Set internal indices of the new cube
        Cube cubeComp = cube.GetComponent<Cube>();
        cubeComp.SetIndices(x, y);
        // Reset the match state of the cube to make sure it is not in a match state
        cubeComp.ResetMatchState();

        // Update the board tile with the new cube
        boardTiles[x, y].SetObjectInside(cube);
    }

    /// <summary>
    /// Handles the matched group of cubes by processing the match, updating the score, and spawning special cubes if needed.
    /// </summary>
    /// <param name="group">The group of matched cubes.</param>
    private IEnumerator HandleMatch(List<Cube> group)
    {
        // Increment the combo count and show the combo text
        ComboManager.Instance.IncrementCombo();
        // Get the number of cubes in the matched group
        int count = group.Count;
        // Find the center cube indices for the group
        Vector2 center = GameBoard.Instance.FindCenterCubeIndices(group, count);
        ComboManager.Instance.ShowComboTextAt(boardTiles[(int)center.x, (int)center.y].GetObjectInside().transform);

        // Calculate the score to add based on the match group
        int scoreToAdd = ScoreManager.Instance.CalculateMatchScore(group);
        // Add the calculated score to the score manager
        ScoreManager.Instance.AddScore(scoreToAdd);

        // First, check a group of 3+ White Cubes have matched
        int whiteCount = group.Count(c => c.GetColor() == CubeColor.White);
        if (whiteCount >= 3)
        {
            // Just pick any white cube in the group to trigger the destruction
            WhiteCube anyWhite = group.First(c => c.GetColor() == CubeColor.White) as WhiteCube;
            yield return StartCoroutine(anyWhite.DestroyTheBoard());
            yield break;
        }
        // Second, check if the group has 5+ matched cubes, if so we will spawn a White Cube at the position of the center cube
        else if (count >= 5)
        {
            // Set the indices for the new White Cube to be spawned
            int whiteCubeX = (int)center.x;
            int whiteCubeY = (int)center.y;

            // Loop through the group and play the pop animation for each cube
            foreach (Cube c in group)
            {
                StartCoroutine(c.PlayPopAnimation(1.3f, 0.1f));
            }

            yield return new WaitForSeconds(0.15f);

            // Get the center cube from the group
            Cube centerCube = boardTiles[whiteCubeX, whiteCubeY].GetObjectInside().GetComponent<Cube>();
            // Play the five cubes destruction effect at the center cube position for the color of the center cube
            ParticleManager.Instance.PlayFiveCubesDestructionEffect(centerCube);
            // Wait until the particle effect is done
            yield return new WaitForSeconds(ParticleManager.Instance.GetColoredCubeDestructionTime(count));

            // Damage the adjacent obstacles around the group
            DamageAdjacentObstacles(group);

            // Despawn all cubes in the group
            foreach (var cube in group)
            {
                ObjectUtils.SafeDespawn(cube);
            }

            // Spawn a new White Cube at the center position, after all the cubes are despawned
            yield return StartCoroutine(SpawnWhiteCube(whiteCubeX, whiteCubeY));
            yield break;
        }

        // Third, check if the group has a Special Cube, if so we will play its effect first
        Cube specialCube = null;
        // Loop through the group to find a Special Cube, if any exists, set the cube as the specialCube
        foreach (Cube c in group)
        {
            if (c.IsSpecial)
            {
                specialCube = c; break;
            }
        }
        // If there is a Special Cube, play its effect first
        if (specialCube != null)
        {
            // Trigger the correct special effect depending on the color of the Special Cube
            yield return StartCoroutine(specialCube.GetComponent<SpecialCube>().TriggerSpecialEffect());

            // Damage the adjacent obstacles around the group
            DamageAdjacentObstacles(group);

            // Despawn all cubes in the group
            foreach (var cube in group)
            {
                ObjectUtils.SafeDespawn(cube);
            }
        }
        // Fourth, check if the group has 4 matched cubes, if so we will spawn a Special Cube at the position of the center cube
        else if (count == 4)
        {
            // If there is a cube to make special, we will replace it with a Special Cube
            if (cubeToMakeSpecial != null)
            {
                // Play the destruction effect for the matched group of 4 cubes
                foreach (var cube in group)
                    ParticleManager.Instance.PlayFourCubesDestructionEffect(cube);
                yield return new WaitForSeconds(ParticleManager.Instance.GetColoredCubeDestructionTime(count));

                // Then damage the adjacent obstacles around the group
                DamageAdjacentObstacles(group);

                // Remove the cube to be made special from the group and replace it with a Special Cube
                group.Remove(cubeToMakeSpecial);
                ReplaceWithSpecialCube(cubeToMakeSpecial);

                // Play spawn pop animation
                StartCoroutine(cubeToMakeSpecial.PlayPopAnimation());
                // Reset the cube to make special
                cubeToMakeSpecial = null;
            }
            // If there is no cube to make special, we will just play the destruction effect for the matched group of 4 cubes
            // and damage the adjacent obstacles
            else
            {
                // Play the destruction effect for the matched group of 4 cubes
                foreach (var cube in group)
                    ParticleManager.Instance.PlayFourCubesDestructionEffect(cube);
                yield return new WaitForSeconds(ParticleManager.Instance.GetColoredCubeDestructionTime(4));
                // Damage the adjacent obstacles around the group
                DamageAdjacentObstacles(group);
            }

            // Despawn all cubes in the group after the effects are played and the obstacles are damaged
            foreach (var cube in group)
            {
                // If the cube is a Special Cube, we will not despawn it, but continue to the next one
                if (cube.IsSpecial)
                {
                    continue;
                }
                ObjectUtils.SafeDespawn(cube);
            }
        }
        // If the group has less than 4 cubes, we will just play the destruction effect for the matched group of 3 cubes
        else
        {
            // Damage the adjacent obstacles around the group
            DamageAdjacentObstacles(group);

            // Loop through the group and despawn all cubes
            foreach (var cube in group)
            {
                ObjectUtils.SafeDespawn(cube);
            }
        }
        // Reset the cube to make special after the match is handled
        cubeToMakeSpecial = null;
    }

    /// <summary>
    /// Replaces the given base cube with a Special Cube of the same color.
    /// </summary>
    /// <param name="baseCube">The cube that is to be made into a Special Cube.</param>
    public void ReplaceWithSpecialCube(Cube baseCube)
    {
        // Get the x and y indices of the base cube
        int x = baseCube.GetX();
        int y = baseCube.GetY();
        // Find the spawn position for the new Special Cube based on the indices
        Vector2 spawnPos = FindPositionsGivenIndices(x, y);

        // Set the color of the base cube
        CubeColor color = baseCube.GetColor();

        // Despawn the base cube safely
        ObjectUtils.SafeDespawn(baseCube);

        // Create a new key for the Special Cube based on the color of the base cube
        string newSpecialCubeKey = color.ToString() + "CubeSpecial";

        // Choose the correct special prefab and spawn it at the spawn position
        GameObject specialGO = ObjectPoolManager.SpawnObjectByKey(newSpecialCubeKey, spawnPos);
        // Initialize the new Special Cube on the board
        InitializeNewCube(specialGO, x, y, spawnPos);
    }

    /// <summary>
    /// Spawns a White Cube at the specified position on the board.
    /// </summary>
    /// <param name="x">The x coordinate on the board.</param>
    /// <param name="y">The y coordinate on the board.</param>
    public IEnumerator SpawnWhiteCube(int x, int y)
    {
        // Find the spawn position for the new White Cube based on the indices
        Vector2 spawnPos = FindPositionsGivenIndices(x, y);
        // Create a new key for the White Cube prefab
        string newWhiteCubeKey = "WhiteCube";

        // Spawn a White Cube from the object pool at the spawn position using the new key
        GameObject whiteGO = ObjectPoolManager.SpawnObjectByKey(newWhiteCubeKey, spawnPos);
        Cube whiteCube = whiteGO.GetComponent<Cube>();
        // Initialize the new White Cube on the board
        InitializeNewCube(whiteGO, x, y, spawnPos);
        // Increase the number of White Cubes on the board
        GameBoard.Instance.IncrementWhiteCubeCount();
        yield break;
    }

    /// <summary>
    /// Finds and returns the best match group of cubes to destroy on the board.
    /// The priority is to find a group with the most amount of adjacent Obstacles.
    /// Then it is to find a group of 3+ White Cubes, then groups of 5+ cubes,
    /// a group that has a Special Cube inside, a groups of 4 cubes, and finally
    /// a group of 3 cubes.
    /// </summary>
    private List<Cube> FindBestMatchGroup()
    {
        // Initialize a list to hold all groups of matched cubes
        List<List<Cube>> allGroups = new List<List<Cube>>();

        // Loop through each tile on the board to find cubes
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Get the object inside the current board tile
                GameObject obj = boardTiles[x, y].GetObjectInside();
                // If the object is null or not a Cube, continue to the next tile
                if (obj == null || !obj.TryGetComponent<Cube>(out Cube cube)) continue;

                // Reset the match state of the cube to ensure it is not in a match state
                cube.ResetMatchState();
                // Check for matches with the cube
                if (CheckForMatches(cube))
                {
                    // If the cube has matches, create a group with it and its match group
                    List<Cube> group = new List<Cube> { cube };
                    group.AddRange(cube.GetMatchGroup());
                    // If the group has 3 or more cubes, check for white cubes
                    if (group.Count >= 3)
                    {
                        // Find the number of white cubes in the group
                        int whiteCount = group.Count(c => c.GetColor() == CubeColor.White);
                        // If the group has 3 or more white cubes, set them as matched and return the group
                        if (whiteCount >= 3)
                        {
                            foreach (var c in group) c.SetMatch(true);
                            return group;
                        }
                        allGroups.Add(group);
                    }
                }
            }
        }

        // If no groups were found, return null
        if (allGroups.Count == 0) return null;

        // Create a variable to hold the best group and its score
        List<Cube> bestGroup = null;
        int bestScore = int.MinValue;

        // Loop through all the groups to find the best one based on the score
        foreach (var group in allGroups)
        {
            // Reset the score for the current group
            int score = 0;

            // Check the adjacent obstacles around the group and add their count to the score
            HashSet<Obstacle> adjacentObstacles = new HashSet<Obstacle>();
            // Then go through each cube in the group and check for any adjacent obstacles
            foreach (var cube in group)
            {
                int cx = cube.GetX();
                int cy = cube.GetY();
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var dir in dirs)
                {
                    int nx = cx + dir.x;
                    int ny = cy + dir.y;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        GameObject neighbor = boardTiles[nx, ny].GetObjectInside();
                        if (neighbor != null && neighbor.TryGetComponent<Obstacle>(out Obstacle obs))
                        {
                            adjacentObstacles.Add(obs);
                        }
                    }
                }
            }
            // Add the count of adjacent obstacles to the score
            score += adjacentObstacles.Count * 1000;

            // If the group has 5 or more cubes, add 500 bonus score
            if (group.Count >= 5) score += 500;
            // If there is a Special Cube in the group, add 300 bonus score
            if (group.Any(c => c.IsSpecial)) score += 300;
            // If the group has 4 cubes, add 100 bonus score
            if (group.Count == 4) score += 100;
            // If the group has 3 cubes, add 10 bonus score
            if (group.Count == 3) score += 10;

            // If the current score is better than the best score found so far,
            if (score > bestScore)
            {
                // Update the best score and best group
                bestScore = score;
                bestGroup = group;
            }
        }
        // At the end of the loop, return the best group found out of all groups
        return bestGroup;
    }

    /// <summary>
    /// Despawns the given Cube from the board and updates the board tile to be empty.
    /// Decrements the target count for the cube if decreaseTargetCount is true.
    /// </summary>
    /// <param name="cube">The Cube to despawn.</param>
    /// <param name="decreaseTargetCount">The boolean flag to decrease the target count for the Cube or not, by default it is true.</param>
    public void DespawnCube(Cube cube, bool decreaseTargetCount = true)
    {
        // Get the x and y indices of the cube
        int x = cube.GetX();
        int y = cube.GetY();

        // Update the board tile to be empty, remove the cube from it and reset its match state
        boardTiles[x, y].RemoveObjectInside();
        cube.ResetMatchState();

        // Despawn the cube from the board using the ObjectPoolManager
        ObjectPoolManager.DespawnObject(cube.gameObject);

        // If decreaseTargetCount is true, decrease the target count for the cube
        if (decreaseTargetCount)
        {
            LevelManager.Instance.DecreaseTargetCount(cube.gameObject, 1);

            // If the cube is a WhiteCube, decrement the white cube count on the board
            if (cube is WhiteCube)
                DecrementWhiteCubeCount();
        }
    }

    /// <summary>
    /// Despawns the given Obstacle from the board and updates the board tile to be empty.
    /// Decrements the target count for the cube if decreaseTargetCount is true.
    /// </summary>
    /// <param name="obstacle">The Obstacle to despawn.</param>
    /// <param name="decreaseTargetCount">The boolean flag to decrease the target count for the Obstacle or not, by default it is true.</param>
    public void DespawnObstacle(Obstacle obstacle, bool decreaseTargetCount = true)
    {
        // Follow the same logic as DespawnCube, but for Obstacle
        int x = obstacle.GetX();
        int y = obstacle.GetY();

        boardTiles[x, y].RemoveObjectInside();

        ObjectPoolManager.DespawnObject(obstacle.gameObject);

        if (decreaseTargetCount)
            LevelManager.Instance.DecreaseTargetCount(obstacle.gameObject, 1);
    }

    /// <summary>
    /// Checks if the Win or Lose conditions are met after all cubes have finished moving.
    /// </summary>
    private IEnumerator CheckWinOrLose()
    {
        // Wait until all cubes are not moving and all particle effects are done
        yield return new WaitUntil(() => AllCubesFinishedMoving());
        yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());

        // If all targets are destroyed, check if the final bonus has been triggered
        if (LevelManager.Instance.CheckIfAllTargetsAreDestroyed())
        {
            // If the final bonus has not been triggered yet and there are any white cubes on the board,
            if (!finalBonusTriggered && HasAnyWhiteCubes)
            {
                // Trigger the final bonus effect by uniting all white cubes and running the final effect
                finalBonusTriggered = true;
                yield return StartCoroutine(UniteWhiteCubesAndTriggerFinalEffect());
            }
            yield return new WaitForSeconds(0.25f);
            // Despawn all board objects and open the win screen
            DespawnAllBoardObjects();
            LevelManager.Instance.OpenWinScreen();
        }
        // If there are no moves left and not all targets are destroyed, open the lose screen
        else if (LevelManager.Instance.GetMoveCount() == 0)
        {
            yield return new WaitForSeconds(0.25f);
            DespawnAllBoardObjects();
            LevelManager.Instance.OpenLoseScreen();
        }
    }

    /// <summary>
    /// Despawns all objects on the board, including cubes and obstacles.
    /// </summary>
    public void DespawnAllBoardObjects()
    {
        // Loop through each tile on the board
        for (int x = 0; x < boardTiles.GetLength(0); x++)
        {
            for (int y = 0; y < boardTiles.GetLength(1); y++)
            {
                // Get the object inside the current board tile
                GameObject obj = boardTiles[x, y].GetObjectInside();
                if (obj != null)
                {
                    // Despawn the object safely using ObjectUtils and update the board tile to be empty
                    ObjectUtils.SafeDespawn(obj);

                    boardTiles[x, y].RemoveObjectInside();
                }
            }
        }
    }

    /// <summary>
    /// Unites all active White Cubes on the board in the center of the screen and triggers the final effect.
    /// </summary>
    public IEnumerator UniteWhiteCubesAndTriggerFinalEffect()
    {
        // Get all active White Cubes on the board
        List<WhiteCube> whiteCubes = FindObjectsOfType<WhiteCube>()
            .Where(w => w.gameObject.activeInHierarchy).ToList();

        // If there are no White Cubes on the board, just return
        if (whiteCubes.Count == 0)
            yield break;

        // Find the center world position of the screen
        Vector2 centerWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector2(Screen.width / 2f, Screen.height / 2f)
        );

        // Move all White Cubes to the center of the screen
        foreach (var white in whiteCubes)
        {
            white.StartCoroutine(white.MoveToPositionAnimated(centerWorldPos, 0.4f));
        }

        yield return new WaitForSeconds(0.4f);

        // Choose the closest White Cube to the center position as the caster of the effect
        WhiteCube caster = whiteCubes
            .OrderBy(w => Vector2.Distance(w.transform.position, centerWorldPos))
            .First();

        // Store the number of white cubes on the board before destroying all but one
        int numOfWhiteCubes = whiteCubes.Count;

        // Loop through all White Cubes and despawn them except the caster
        foreach (var white in whiteCubes)
        {
            if (white != caster)
            {
                ObjectUtils.SafeDespawn(white);
            }
        }
        // Make the caster White Cube the only active White Cube on the board and set it bigger
        caster.transform.localScale = Vector3.one * 1.8f;

        // Trigger the final bonus effect on the caster
        yield return StartCoroutine(caster.TriggerFinalBonusEffect(numOfWhiteCubes));

        // After the final effect is done, drop the cubes that were left on the board
        List<Vector2> refillSpots = DropCubes();
        // And fill the empty spots with new cubes
        SpawnNewCubesAndDrop(refillSpots);

        // Wait until all cubes are not moving and all particle effects are done
        yield return new WaitUntil(() => AllCubesFinishedMoving());
        yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());

        // Run Cascade Loop, if final White Cube effect caused chain matches
        yield return StartCoroutine(RunCascadeLoop());
    }

    /// <summary>
    /// Tries to convert the Cube at the specified position to a White Cube.
    /// </summary>
    /// <param name="x">The x coordinate of the cube to be converted.</param>
    /// <param name="y">The y coordinate of the cube to be converted..</param>
    /// <param name="resultSet">The set of converted White Cubes.</param>
    /// <param name="pop">The flag to check if the pop effect plays.</param>
    public IEnumerator TryConvertToWhiteCube(int x, int y, HashSet<Cube> resultSet, bool pop = true)
    {
        // Check if the tile at the given indices is not empty
        if (!boardTiles[x, y].GetEmpty())
        {
            // Get the object inside the board tile at the given indices
            GameObject obj = boardTiles[x, y].GetObjectInside();
            // If the object is not null and is a Cube, try to convert it to a White Cube
            if (obj != null && obj.TryGetComponent<Cube>(out Cube cube))
            {
                Cube currentCube = obj.GetComponent<Cube>();
                // If the current cube is not already a White Cube, convert it to a White Cube
                if (currentCube.GetColor() != CubeColor.White)
                {
                    // Convert the current cube to a White Cube, notify the target for the old color and play pop if enabled
                    WhiteCube newWhite = currentCube.ConvertToWhiteAndNotifyTarget(pop);
                    // If the new White Cube is not null, add it to the result set
                    if (newWhite != null)
                        resultSet.Add(newWhite);
                }
                // If the current cube is already a White Cube, just add it to the result set
                else
                {
                    // Decrease the target count for the White Cube
                    LevelManager.Instance.DecreaseTargetCount(currentCube.gameObject, 1);
                    resultSet.Add(currentCube);
                }
            }
        }
        // Add a small delay to allow all the processes to complete
        yield return new WaitForSeconds(0.02f);
    }

    /// <summary>
    /// Damages all adjacent obstacles around the given cubes.
    /// </summary>
    /// <param name="cubes">The group of matched cubes.</param>
    public void DamageAdjacentObstacles(List<Cube> cubes)
    {
        // Create a HashSet to keep track of damaged obstacles to avoid double damage
        HashSet<Obstacle> damagedObstacles = new HashSet<Obstacle>();

        // Loop through each cube in the group
        foreach (var cube in cubes)
        {
            // Set its x and y indices
            int x = cube.GetX();
            int y = cube.GetY();

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            // Loop through each direction (up, down, left, right)
            foreach (Vector2Int dir in directions)
            {
                int nx = x + dir.x;
                int ny = y + dir.y;

                // Check if the neighbor indices are within the bounds of the board
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    // If so, get the neighbor GameObject inside the board tile
                    GameObject neighbor = boardTiles[nx, ny].GetObjectInside();
                    // If the neighbor is not null, check if it is an Obstacle
                    if (neighbor != null)
                    {
                        Obstacle obs = neighbor.GetComponent<Obstacle>();
                        // If the neighbor is an Obstacle and has not been damaged yet, damage it and add it to the damaged obstacles set
                        if (obs != null && !damagedObstacles.Contains(obs))
                        {
                            GameBoard.Instance.StartCoroutine(obs.TakeHit());
                            damagedObstacles.Add(obs);
                        }
                    }
                }
            }
        }
    }
}