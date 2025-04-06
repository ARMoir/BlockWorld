using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static readonly int width = 120;
    static readonly int height = 40;
    static char[,]? world;
    static int charX, charY;
    static int worldOffset = 0;
    static readonly char[,,] worldSections = new char[5, height, width];
    static bool needsRedraw = true;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        Console.SetWindowSize(width + 1, height + 1);

        // Initialize world sections and generate the first section
        GenerateWorldSection(worldSections, 1);
        world = GetWorldSection(worldSections, 1);
        FindSpawnPoint();
        GameLoop();
    }

    static void GenerateWorldSection(char[,,] sections, int sectionIndex)
    {
        Random rand = new Random();
        int skyHeight = height - 15;  // Sky height
        int groundStart = skyHeight + 3;  // Ground level start

        // 1. Sky and ground (initialize sky and ground)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y < skyHeight)
                {
                    // Only place clouds in the upper portion of the sky
                    if (y < skyHeight - 5 && rand.Next(100) < 2)
                        sections[sectionIndex, y, x] = '~';  // Cloud
                    else
                        sections[sectionIndex, y, x] = ' ';  // Empty sky
                }
                else
                {
                    sections[sectionIndex, y, x] = '#';  // Ground
                }
            }
        }

        // 2. Mountains (add mountains)
        for (int i = 0; i < 10; i++)  // Add a few mountains
        {
            int mountainX = rand.Next(width);
            int mountainHeight = rand.Next(5, 10);
            int mountainBase = skyHeight - mountainHeight;

            for (int y = mountainBase; y < skyHeight; y++)  // Build the mountain
            {
                int mountainWidth = (mountainHeight - (skyHeight - y)) * 2;
                for (int x = mountainX - mountainWidth / 2; x <= mountainX + mountainWidth / 2; x++)
                {
                    if (IsInside(x, y))
                        sections[sectionIndex, y, x] = '#';  // Mountain terrain
                }
            }
        }

        // 3. Trees (randomly place trees on mountains and ground)
        for (int x = 0; x < width; x++)
        {
            // === 1. Tree on Mountain Peaks ===
            if (rand.Next(100) < 10) // 10% chance
            {
                for (int y = 1; y < height - 2; y++) // Start at y=1 to allow checking y-1 and y-2 safely
                {
                    // Find a mountain peak: a '#' block with empty space above it
                    if (sections[sectionIndex, y, x] == '#' &&
                        sections[sectionIndex, y - 1, x] == ' ' &&
                        sections[sectionIndex, y - 2, x] == ' ')
                    {
                        sections[sectionIndex, y - 2, x] = '^'; // Foliage
                        sections[sectionIndex, y - 1, x] = '|'; // Trunk
                        break; // Only one tree per column
                    }
                }
            }

            // === 2. Tree on Ground ===
            if (sections[sectionIndex, groundStart, x] == '#') // Ground exists at this column
            {
                if (rand.Next(100) < 10) // 10% chance
                {
                    // Check if trunk and foliage space is free
                    if (IsInside(x, groundStart - 1) && sections[sectionIndex, groundStart - 1, x] == ' ' &&
                        IsInside(x, groundStart - 2) && sections[sectionIndex, groundStart - 2, x] == ' ')
                    {
                        sections[sectionIndex, groundStart - 2, x] = '^'; // Foliage
                        sections[sectionIndex, groundStart - 1, x] = '|'; // Trunk
                    }
                }
            }
        }


        // 4. Carve caves
        for (int i = 0; i < 20; i++) // Reduced number of caves
        {
            int caveX = rand.Next(10, width - 10);
            int caveY = groundStart + 2;
            for (int j = 0; j < 50; j++) // Reduced steps per cave
            {
                if (caveX < 1 || caveX >= width - 1 || caveY < groundStart || caveY >= height - 1)
                    break;

                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (IsInside(caveX + dx, caveY + dy))
                            sections[sectionIndex, caveY + dy, caveX + dx] = ' ';

                caveX += rand.Next(-1, 2);
                caveY += rand.Next(-1, 2);
            }
        }

        // 5. Ores
        for (int y = groundStart; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (sections[sectionIndex, y, x] == ' ')
                {
                    int ore = rand.Next(100);
                    if (ore < 2) sections[sectionIndex, y, x] = '♦';
                    else if (ore < 6) sections[sectionIndex, y, x] = '*';
                    else if (ore < 8) sections[sectionIndex, y, x] = '%';
                }
            }
        }
    }

    static char[,] GetWorldSection(char[,,] sections, int sectionIndex)
    {
        char[,] section = new char[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                section[y, x] = sections[sectionIndex, y, x];
            }
        }
        return section;
    }

    static void FindSpawnPoint()
    {
        for (int x = width / 2; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (world[y, x] == '#' && y > 0 && world[y - 1, x] == ' ')
                {
                    charX = x;
                    charY = y - 1;
                    return;
                }
            }
        }
    }

    static void GameLoop()
    {
        Task.Factory.StartNew(() => HandleInput());

        while (true)
        {
            if (needsRedraw)
            {
                Render();
                needsRedraw = false;
            }
            UpdateGameState();
            System.Threading.Thread.Sleep(100); // Control game speed
        }
    }

    // Enum to represent movement directions
    enum Direction { None, Up, Down, Left, Right }

    static Direction lastDirection = Direction.None;  // To track the last direction of movement

    static void HandleInput()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                int newX = charX;
                int newY = charY;

                switch (key)
                {
                    case ConsoleKey.W:
                        // Try to move up
                        if (IsInside(charX, charY - 1) && world[charY - 1, charX] == ' ')
                        {
                            newY--;
                        }
                        lastDirection = Direction.Up;  // Update lastDirection regardless of whether the move was successful
                        break;

                    case ConsoleKey.S:
                        // Move down
                        newY++;
                        lastDirection = Direction.Down;  // Update lastDirection
                        break;

                    case ConsoleKey.A:
                        // Move left
                        newX--;
                        lastDirection = Direction.Left;  // Update lastDirection
                        if (IsInside(charX - 1, charY) && world[charY, charX - 1] == '#')
                        {
                            if (IsInside(charX - 1, charY - 1) && world[charY - 1, charX - 1] != '#')
                            {
                                newY--;  // Climb onto the top of the mountain
                            }
                        }
                        break;

                    case ConsoleKey.D:
                        // Move right
                        newX++;
                        lastDirection = Direction.Right;  // Update lastDirection
                        if (IsInside(charX + 1, charY) && world[charY, charX + 1] == '#')
                        {
                            if (IsInside(charX + 1, charY - 1) && world[charY - 1, charX + 1] != '#')
                            {
                                newY--;  // Climb onto the top of the mountain
                            }
                        }
                        break;

                    case ConsoleKey.Spacebar:
                        // Destroy the '#' in the direction of last movement
                        switch (lastDirection)
                        {
                            case Direction.Up:
                                // Check above the player
                                if (IsInside(charX, charY - 1) && world[charY - 1, charX] == '#')
                                {
                                    world[charY - 1, charX] = ' ';  // Destroy the # above
                                    needsRedraw = true;  // Trigger redraw after destroying the tile
                                }
                                break;

                            case Direction.Down:
                                // Check below the player
                                if (IsInside(charX, charY + 1) && world[charY + 1, charX] == '#')
                                {
                                    world[charY + 1, charX] = ' ';  // Destroy the # below
                                    needsRedraw = true;  // Trigger redraw after destroying the tile
                                }
                                break;

                            case Direction.Left:
                                // Check left of the player
                                if (IsInside(charX - 1, charY) && world[charY, charX - 1] == '#')
                                {
                                    world[charY, charX - 1] = ' ';  // Destroy the # to the left
                                    needsRedraw = true;  // Trigger redraw after destroying the tile
                                }
                                break;

                            case Direction.Right:
                                // Check right of the player
                                if (IsInside(charX + 1, charY) && world[charY, charX + 1] == '#')
                                {
                                    world[charY, charX + 1] = ' ';  // Destroy the # to the right
                                    needsRedraw = true;  // Trigger redraw after destroying the tile
                                }
                                break;

                            default:
                                break;  // If no movement direction, do nothing
                        }
                        break;
                }

                // Update player position if the new position is valid
                if (IsInside(newX, newY) && IsWalkable(world[newY, newX]))
                {
                    charX = newX;
                    charY = newY;
                    needsRedraw = true; // Trigger redraw when player moves
                }

                // Handle world section loading when player reaches the edge
                if (newX <= 0 || newX >= width - 1 || newY <= 0 || newY >= height - 1)
                {
                    worldOffset++;
                    if (worldOffset >= worldSections.GetLength(0)) worldOffset = 0;  // Loop around sections
                    world = GetWorldSection(worldSections, worldOffset);
                }
            }
        }
    }

    static void UpdateGameState()
    {
        // Apply gravity
        if (IsInside(charX, charY + 1) && IsWalkable(world[charY + 1, charX]))
        {
            charY++;
            needsRedraw = true; // Trigger redraw when gravity affects player
        }
    }

    static readonly StringBuilder offScreenBuffer = new StringBuilder(); // Off-screen buffer to hold the next frame

    static void Render()
    {
        // Clear the off-screen buffer for each new frame
        offScreenBuffer.Clear();

        // Build the frame in the off-screen buffer
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == charX && y == charY)
                    offScreenBuffer.Append('☺'); // Representing the player with '@'
                else
                    offScreenBuffer.Append(world[y, x]); // Otherwise, the world state
            }
            offScreenBuffer.Append('\n'); // Add a line break after each row
        }

        // Ensure the console is fully cleared and the cursor is at the top-left
        Console.Clear(); // Clear the screen

        // Output the entire frame from the off-screen buffer to the console
        Console.SetCursorPosition(0, 0); // Reset cursor to top-left
        Console.Write(offScreenBuffer.ToString()); // Write the entire frame to the console
    }

    static bool IsInside(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    static bool IsWalkable(char tile)
    {
        return tile == ' ' || tile == '*' || tile == '♦' || tile == '~';
    }
}
