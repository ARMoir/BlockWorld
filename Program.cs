using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static readonly int width = 120;
    static readonly int height = 28;
    static char[,]? world;
    static int charX, charY;
    static int worldOffset = 0;
    static int skyHeight = height - 15;  // Sky height
    static int groundStart = skyHeight + 3;  // Ground level start
    static readonly char[,,] worldSections = new char[5, height, width];
    static bool needsRedraw = true;
    static Dictionary<char, int> inventory = new Dictionary<char, int>();


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

        // 1. Sky and ground (initialize sky and ground)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (y < skyHeight)
                {
                    // Only place clouds in the upper portion of the sky
                    if (y < skyHeight - 5 && rand.Next(100) < 10)
                    {
                        // Create cloud clusters
                        int cloudSize = rand.Next(1, 2);
                        for (int cy = y; cy < y + cloudSize && cy < skyHeight - 5; cy++)
                        {
                            for (int cx = x; cx < x + cloudSize && cx < width; cx++)
                            {
                                if (rand.Next(100) < 50) // 50% chance for each cell to be a cloud
                                {
                                    sections[sectionIndex, cy, cx] = '~';  // Cloud
                                }
                                else
                                {
                                    sections[sectionIndex, cy, cx] = ' ';  // Empty sky if not a cloud
                                }
                            }
                        }
                    }
                    else
                    {
                        sections[sectionIndex, y, x] = ' ';  // Empty sky
                    }
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
                        int treeHeight = rand.Next(2, 5); // Random tree height
                        for (int ty = y - treeHeight; ty < y; ty++)
                        {
                            if (IsInside(x, ty))
                                sections[sectionIndex, ty, x] = '|'; // Trunk
                        }
                        for (int fx = x - 1; fx <= x + 1; fx++) // Foliage width
                        {
                            if (IsInside(fx, y - treeHeight - 1))
                                sections[sectionIndex, y - treeHeight - 1, fx] = '^'; // Foliage
                        }
                        break; // Only one tree per column
                    }
                }
            }

            // === 2. Tree on Ground ===
            if (sections[sectionIndex, groundStart, x] == '#') // Ground exists at this column
            {
                if (rand.Next(100) < 10) // 10% chance
                {
                    int treeHeight = rand.Next(2, 5); // Random tree height
                                                      // Check if trunk and foliage space is free
                    if (IsInside(x, groundStart - treeHeight - 1) && sections[sectionIndex, groundStart - treeHeight - 1, x] == ' ')
                    {
                        for (int ty = groundStart - treeHeight; ty < groundStart; ty++)
                        {
                            if (IsInside(x, ty))
                                sections[sectionIndex, ty, x] = '|'; // Trunk
                        }
                        for (int fx = x - 1; fx <= x + 1; fx++) // Foliage width
                        {
                            if (IsInside(fx, groundStart - treeHeight - 1))
                                sections[sectionIndex, groundStart - treeHeight - 1, fx] = '^'; // Foliage
                        }
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
                        // Destroy the block in the direction of last movement
                        switch (lastDirection)
                        {
                            case Direction.Up:
                                if (IsInside(charX, charY - 1) && IsDestructible(world[charY - 1, charX]))
                                {
                                    char destroyedTile = world[charY - 1, charX];
                                    world[charY - 1, charX] = ' ';  // Destroy the tile above
                                    AddToInventory(destroyedTile);  // Add the destroyed tile to the inventory
                                    UpdateScreenBuffer(charX, charY - 1, ' ', ConsoleColor.Gray, ConsoleColor.Black);
                                    needsRedraw = true;
                                }
                                break;
                            case Direction.Down:
                                if (IsInside(charX, charY + 1) && IsDestructible(world[charY + 1, charX]))
                                {
                                    char destroyedTile = world[charY + 1, charX];
                                    world[charY + 1, charX] = ' ';  // Destroy the tile below
                                    AddToInventory(destroyedTile);  // Add the destroyed tile to the inventory
                                    UpdateScreenBuffer(charX, charY + 1, ' ', ConsoleColor.Gray, ConsoleColor.Black);
                                    needsRedraw = true;
                                }
                                break;
                            case Direction.Left:
                                if (IsInside(charX - 1, charY) && IsDestructible(world[charY, charX - 1]))
                                {
                                    char destroyedTile = world[charY, charX - 1];
                                    world[charY, charX - 1] = ' ';  // Destroy the tile to the left
                                    AddToInventory(destroyedTile);  // Add the destroyed tile to the inventory
                                    UpdateScreenBuffer(charX - 1, charY, ' ', ConsoleColor.Gray, ConsoleColor.Black);
                                    needsRedraw = true;
                                }
                                break;
                            case Direction.Right:
                                if (IsInside(charX + 1, charY) && IsDestructible(world[charY, charX + 1]))
                                {
                                    char destroyedTile = world[charY, charX + 1];
                                    world[charY, charX + 1] = ' ';  // Destroy the tile to the right
                                    AddToInventory(destroyedTile);  // Add the destroyed tile to the inventory
                                    UpdateScreenBuffer(charX + 1, charY, ' ', ConsoleColor.Gray, ConsoleColor.Black);
                                    needsRedraw = true;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                }

                // Update player position if the new position is valid
                if (IsInside(newX, newY) && IsWalkable(world[newY, newX]))
                {
                    UpdateScreenBuffer(charX, charY, world[charY, charX], ConsoleColor.Gray, ConsoleColor.Black);
                    charX = newX;
                    charY = newY;
                    UpdateScreenBuffer(charX, charY, '☺', ConsoleColor.Yellow, ConsoleColor.Black);
                    needsRedraw = true; // Trigger redraw when player moves
                }
            }
        }
    }

    static void AddToInventory(char item)
    {
        if (inventory.ContainsKey(item))
        {
            inventory[item]++;
        }
        else
        {
            inventory[item] = 1;
        }
    }

    // Example usage in game logic
static void UpdateGameState()
{
    // Apply gravity
    if (IsInside(charX, charY + 1) && IsWalkable(world[charY + 1, charX]))
    {
        // Mark current position as dirty
        UpdateScreenBuffer(charX, charY, world[charY, charX], ConsoleColor.Gray, ConsoleColor.Black);
        charY++;
        // Mark new position as dirty
        UpdateScreenBuffer(charX, charY, '☺', ConsoleColor.Yellow, ConsoleColor.Black);
        needsRedraw = true; // Trigger redraw when gravity affects player
    }
}

    static Dictionary<(int x, int y), (char tile, ConsoleColor foreground, ConsoleColor background)> screenBuffer = new();

    static bool firstRender = true;

    static void Render()
    {
        if (firstRender)
        {
            // Initial full screen render
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char tile = world[y, x];
                    ConsoleColor foreground = ConsoleColor.Gray; // Default color
                    ConsoleColor background = ConsoleColor.Black;

                    if (y < skyHeight)
                    {
                        // Sky color
                        background = ConsoleColor.Blue; // Change sky background to blue
                    }
                    else
                    {
                        // Ground color
                        background = ConsoleColor.DarkGreen; // Change ground background to dark green
                    }

                    if (x == charX && y == charY)
                    {
                        foreground = ConsoleColor.Yellow; // Player color
                        tile = '☺';  // Player symbol
                    }
                    else
                    {
                        // Set color based on tile type
                        switch (tile)
                        {
                            case '#':
                                foreground = ConsoleColor.DarkGray; // Ground/Mountain
                                break;
                            case '~':
                                foreground = ConsoleColor.White; // Cloud
                                break;
                            case '^':
                                foreground = ConsoleColor.Green; // Tree foliage
                                break;
                            case '|':
                                foreground = ConsoleColor.DarkGreen; // Tree trunk
                                break;
                            case '*':
                                foreground = ConsoleColor.Yellow; // Ore
                                break;
                            case '♦':
                                foreground = ConsoleColor.Blue; // Ore
                                break;
                            case '%':
                                foreground = ConsoleColor.Red; // Ore
                                break;
                        }
                    }

                    // Set the cursor position and color
                    Console.SetCursorPosition(x, y);
                    Console.ForegroundColor = foreground;
                    Console.BackgroundColor = background;

                    // Write the character
                    Console.Write(tile);
                }
            }

            firstRender = false; // Switch to dirty rendering after the initial render
        }
        else
        {
            // Dirty rendering for subsequent frames
            foreach (var cell in screenBuffer)
            {
                (int x, int y) = cell.Key;
                (char tile, ConsoleColor foreground, ConsoleColor background) = cell.Value;

                // Set the cursor position and color
                Console.SetCursorPosition(x, y);

                if (y < skyHeight)
                {
                    // Sky color
                    background = ConsoleColor.Blue; // Change sky background to blue
                }
                else
                {
                    // Ground color
                    background = ConsoleColor.DarkGreen; // Change ground background to dark green
                }

                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;

                // Write the character
                Console.Write(tile);
            }

            // Clear the buffer after rendering
            screenBuffer.Clear();
        }

        // Render the inventory at the bottom of the screen
        Console.SetCursorPosition(0, height);
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Write("Inventory: ");
        foreach (var item in inventory)
        {
            Console.Write($"{item.Key}:{item.Value} ");  // Display item and count
        }
    }


    static void UpdateScreenBuffer(int x, int y, char tile, ConsoleColor foreground, ConsoleColor background)
    {
        if (IsInside(x, y))
        {
            screenBuffer[(x, y)] = (tile, foreground, background);
        }
    }


    static bool IsInside(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    static bool IsWalkable(char tile)
    {
        return tile == ' ' || tile == '*' || tile == '♦' || tile == '~';
    }

    // A method to check if a tile is destructible
    static bool IsDestructible(char tile)
    {
        // You can add more tiles to the destructible list as needed
        return tile == '#' || tile == '*' || tile == '♦' || tile == '%' || tile == '^' || tile == '|';
    }
}
