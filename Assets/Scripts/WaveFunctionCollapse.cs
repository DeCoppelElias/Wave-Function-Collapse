using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaveFunctionCollapse : MonoBehaviour
{
    private enum State { Idle, Collapsing, Backtracking}
    private State state = State.Idle;

    private Dictionary<string, string[]> markers = new Dictionary<string, string[]>();

    private Cell[,] grid = new Cell[10, 10];
    private Dictionary<int, TileWrapper> tiles = new Dictionary<int, TileWrapper>();

    private List<Cell> collapsedCells = new List<Cell>();

    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private Tile empty;
    [SerializeField]
    private Tile error;

    private float actionCooldown = 0f;
    private float lastAction = 1;

    private enum Side { TOP, RIGHT, BOTTOM, LEFT}

    private class TileWrapper
    {
        public Tile tile;

        //[TOP, RIGHT, BOTTOM, LEFT] if not rotated
        public string[,] sides = new string[4,3];

        public TileWrapper(Tile tile)
        {
            this.tile = tile;
        }

        public string[,] GetSides(Quaternion rotation)
        {
            if (rotation == Quaternion.identity)
            {
                return sides;
            }
            else if (rotation == Quaternion.Euler(0, 0, 90f))
            {
                return ShiftArrayRight(sides, 1);
            }
            else if (rotation == Quaternion.Euler(0, 0, 180))
            {
                return ShiftArrayRight(sides, 2);
            }
            else if (rotation == Quaternion.Euler(0, 0, 270))
            {
                return ShiftArrayRight(sides, 3);
            }
            else throw new System.Exception("Not a valid rotation");
        }

        // [TOP, RIGHT, BOTTOM, LEFT]
        public void SetSides(string[,] sides)
        {
            if (sides.GetLength(0) != 4) return;
            this.sides = (string[,])sides.Clone();
        }

        private string[,] ShiftArrayRight(string[,] strings, int amount)
        {
            string[,] shiftStrings = new string[strings.GetLength(0), strings.GetLength(1)];

            for (int row = 0; row < strings.GetLength(0); row++)
            {
                for (int column = 0; column < strings.GetLength(1); column++)
                {
                    shiftStrings[row,column] = strings[(row + amount) % strings.GetLength(0),column];
                }
            }
            return shiftStrings;
        }
    }
    private class Cell
    {
        public WaveFunctionCollapse wfc;

        public bool collapsed = false;
        public List<int> options = new List<int>();

        public int pick = -1;
        public Quaternion rotation = Quaternion.identity;

        public int row;
        public int column;

        public List<int> wrongPicks = new List<int>();

        //[TOP, RIGHT, BOTTOM, LEFT] no rotation
        // Each side will have 3 points that connect to other points
        public string[,] rules = new string[4,3];

        public Cell(WaveFunctionCollapse wfc, int row, int column)
        {
            this.wfc = wfc;

            this.options = new List<int>();
            for(int i = 0; i < wfc.tiles.Count; i++)
            {
                this.options.Add(i);
            }

            this.collapsed = false;
            this.pick = -1;

            this.row = row;
            this.column = column;

            for(int i = 0; i < rules.GetLength(0); i++)
            {
                for (int j = 0; j < rules.GetLength(1); j++)
                {
                    this.rules[i, j] = "";
                }
            }
        }

        public string Collapse()
        {
            if (collapsed) return "ERROR";
            if (options.Count == 0)
            {
                Vector3Int position = new Vector3Int(column, wfc.grid.GetLength(0) - row, 0);
                wfc.DisplayError(position);
                return "BACKTRACK";
            }
            else
            {
                int r = Random.Range(0, options.Count);

                this.pick = options[r];
                this.rotation = CalculateValidRotation(this.pick);
                this.collapsed = true;
                wfc.AddToCollapsedCells(this);

                UpdateNeighborCells();

                return "STEP";
            }
        }
        public string ReCollapse()
        {
            DebugOptions();

            Debug.Log("Last pick: "+ this.pick);

            int lastPick = this.pick;

            UnCollapse();

            wrongPicks.Add(lastPick);

            foreach(int wrongPick in wrongPicks)
            {
                this.options.Remove(wrongPick);
            }

            string status = Collapse();

            Debug.Log("New pick: " + this.pick);

            return status;
        }

        public void Reset()
        {
            this.wrongPicks = new List<int>();
            UnCollapse();
        }

        private string UnCollapse()
        {
            if (!collapsed) return "ERROR";

            this.pick = -1;
            this.collapsed = false;
            this.rotation = Quaternion.identity;
            ResetOptions();
            UpdateOptions();

            wfc.RemoveFromCollapsedCells(this);

            // TOP
            int newRow = this.row - 1;
            int newColumn = this.column;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];
                if (!neighborCell.collapsed)
                {
                    neighborCell.RemoveRule(Side.BOTTOM);

                    //DebugCell(neighborCell);
                }
            }

            // RIGHT
            newRow = this.row;
            newColumn = this.column + 1;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];
                if (!neighborCell.collapsed)
                {
                    neighborCell.RemoveRule(Side.LEFT);

                    //DebugCell(neighborCell);
                }
            }

            // BOTTOM
            newRow = this.row + 1;
            newColumn = this.column;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];
                if (!neighborCell.collapsed)
                {
                    neighborCell.RemoveRule(Side.TOP);

                    //DebugCell(neighborCell);
                }
            }

            // LEFT
            newRow = this.row;
            newColumn = this.column - 1;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];
                if (!neighborCell.collapsed)
                {
                    neighborCell.RemoveRule(Side.RIGHT);

                    //DebugCell(neighborCell);
                }
            }

            return "STEP";
        }

        private void UpdateNeighborCells()
        {
            if (!collapsed) return;

            TileWrapper tileWrapper = wfc.tiles[this.pick];

            string[,] rotatedSides = tileWrapper.GetSides(this.rotation);

            // TOP
            int newRow = this.row - 1;
            int newColumn = this.column;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];
                if (!neighborCell.collapsed)
                {
                    string[] rule = new string[3];
                    for (int i = 0; i < rule.Length; i++)
                    {
                        rule[i] = rotatedSides[0, i];
                    }

                    neighborCell.AddRule(Side.BOTTOM, rule);

                    //DebugCell(neighborCell);
                }
            }

            // RIGHT
            newRow = this.row;
            newColumn = this.column + 1;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];

                if (!neighborCell.collapsed)
                {
                    string[] rule = new string[3];
                    for (int i = 0; i < rule.Length; i++)
                    {
                        rule[i] = rotatedSides[1, i];
                    }

                    neighborCell.AddRule(Side.LEFT, rule);

                    //DebugCell(neighborCell);
                }
            }

            // BOTTOM
            newRow = this.row + 1;
            newColumn = this.column;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];

                if (!neighborCell.collapsed)
                {
                    string[] rule = new string[3];
                    for (int i = 0; i < rule.Length; i++)
                    {
                        rule[i] = rotatedSides[2, i];
                    }

                    neighborCell.AddRule(Side.TOP, rule);

                    //DebugCell(neighborCell);
                }
            }

            // LEFT
            newRow = this.row;
            newColumn = this.column - 1;
            if (newRow >= 0 && newRow < wfc.grid.GetLength(0)
                && newColumn >= 0 && newColumn < wfc.grid.GetLength(1))
            {
                Cell neighborCell = wfc.grid[newRow, newColumn];

                if (!neighborCell.collapsed)
                {
                    string[] rule = new string[3];
                    for (int i = 0; i < rule.Length; i++)
                    {
                        rule[i] = rotatedSides[3, i];
                    }

                    neighborCell.AddRule(Side.RIGHT, rule);

                    //DebugCell(neighborCell);
                }
            }

            //Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }

        private Quaternion CalculateValidRotation(int pick)
        {
            if (CheckRules(pick, 0)) return Quaternion.identity;
            else if (CheckRules(pick, 1)) return Quaternion.Euler(0, 0, 90);
            else if (CheckRules(pick, 2)) return Quaternion.Euler(0, 0, 180);
            else if (CheckRules(pick, 3)) return Quaternion.Euler(0, 0, 270);
            else throw new System.Exception("Selected tile has no valid rotations");
        }

        public int GetPick()
        {
            if (!collapsed) throw new System.Exception("Cell has not been collapsed");

            return this.pick;
        }

        public void AddRule(Side side, string[] rule)
        {
            if (rule.Length != 3) return;
            if (side == Side.TOP)
            {
                for(int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[0, i] = rule[i];
                }
            }
            if (side == Side.RIGHT)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[1, i] = rule[i];
                }
            }
            if (side == Side.BOTTOM)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[2, i] = rule[i];
                }
            }
            if (side == Side.LEFT)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[3, i] = rule[i];
                }
            }

            UpdateOptions();
        }

        public void RemoveRule(Side side)
        {
            if (side == Side.TOP)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[0, i] = "";
                }
            }
            if (side == Side.RIGHT)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[1, i] = "";
                }
            }
            if (side == Side.BOTTOM)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[2, i] = "";
                }
            }
            if (side == Side.LEFT)
            {
                for (int i = 0; i < rules.GetLength(1); i++)
                {
                    rules[3, i] = "";
                }
            }

            ResetOptions();

            UpdateOptions();
        }

        private void ResetOptions()
        {
            this.options = new List<int>();
            for (int i = 0; i < wfc.tiles.Count; i++)
            {
                this.options.Add(i);
            }
        }

        private void UpdateOptions()
        {
            List<int> newOptions = new List<int>();

            foreach(int option in this.options)
            {
                bool valid = false;
                int counter = 0;
                while(!valid && counter < 4)
                {
                    valid = CheckRules(option, counter);
                    counter++;
                }

                if (valid == true) newOptions.Add(option);
            }

            this.options = newOptions;
        }

        private bool CheckRules(int option, int rotation)
        {
            TileWrapper tileWrapper = wfc.getTileWrapper(option);
            bool valid = true;

            string[,] rotatedSides = ShiftArrayRight(tileWrapper.sides, rotation);
            for (int i = 0; i < rules.GetLength(0); i++)
            {
                for (int j = 0; j < rules.GetLength(1); j++)
                {
                    string currentRule = rules[i, j];
                    string marker = rotatedSides[i, rules.GetLength(1) - 1 - j];
                    if (currentRule.Contains("-"))
                    {
                        string s = currentRule.Remove(0, 1);
                        if (wfc.markers[marker].Contains(s)) valid = false;
                    }
                    else
                    {
                        if (currentRule != "" && !wfc.markers[marker].Contains(currentRule)) valid = false;
                    }
                }
            }

            return valid;
        }

        private string[,] ShiftArrayRight(string[,] strings, int amount)
        {
            string[,] shiftStrings = new string[strings.GetLength(0), strings.GetLength(1)];

            for (int row = 0; row < strings.GetLength(0); row++)
            {
                for (int column = 0; column < strings.GetLength(1); column++)
                {
                    shiftStrings[row, column] = strings[(row + amount) % strings.GetLength(0), column];
                }
            }
            return shiftStrings;
        }

        private void DebugOptions()
        {
            string optionsString = "";
            foreach(int option in this.options)
            {
                optionsString += option + "/";
            }
            Debug.Log(optionsString);
        }
    }

    private TileWrapper getTileWrapper(int i)
    {
        return tiles[i];
    }

    private void AddToCollapsedCells(Cell cell)
    {
        this.collapsedCells.Insert(0,cell);
    }

    private void RemoveFromCollapsedCells(Cell cell)
    {
        this.collapsedCells.Remove(cell);
    }

    private Cell NextCell()
    {
        int shortestAmount = int.MaxValue;
        List<Cell> cells = new List<Cell>();

        for(int row = 0; row < grid.GetLength(0); row++)
        {
            for (int column = 0; column < grid.GetLength(1); column++)
            {
                Cell currentCell = grid[row, column];

                if(!currentCell.collapsed && currentCell.options.Count < shortestAmount)
                {
                    cells = new List<Cell>();
                    cells.Add(currentCell);

                    shortestAmount = currentCell.options.Count;
                }
                else if(!currentCell.collapsed && currentCell.options.Count == shortestAmount)
                {
                    cells.Add(currentCell);
                }
            }
        }

        if (cells.Count == 0) return null;

        int r = Random.Range(0, cells.Count);

        return cells[r];
    }

    private string CollapseNextCell()
    {
        Cell nextCell = NextCell();

        if (nextCell == null)
        {
            return "FINISHED";
        }

        string status = nextCell.Collapse();

        return status;
    }

    private void DebugCell(Cell cell)
    {
        string options = "";
        foreach(int option in cell.options)
        {
            options += tiles[option].tile.name + " / ";
        }


        Debug.Log("Cell: ");
        Debug.Log("Row: " + cell.row);
        Debug.Log("Column: " + cell.column);
        Debug.Log("New Options: " + options);
        Debug.Log("-----------------------------------------------------------");
    }

    private void DisplayGrid()
    {
        for (int row = 0; row < grid.GetLength(0); row++)
        {
            for (int column = 0; column < grid.GetLength(1); column++)
            {
                Cell currentCell = grid[row, column];

                DisplayCell(currentCell, new Vector3Int(column, grid.GetLength(0) - row, 0));
            }
        }
    }

    private void DisplayCell(Cell cell, Vector3Int position)
    {
        if (cell.collapsed)
        {
            Tile tile = tiles[cell.pick].tile;

            tilemap.SetTransformMatrix(position, Matrix4x4.Rotate(cell.rotation));

            tilemap.SetTile(position, tile);
        }
        else
        {
            tilemap.SetTile(position, empty);
        }
    }

    private void DisplayError(Vector3Int position)
    {
        this.tilemap.SetTile(position, error);
    }

    private void InitializeTiles()
    {
        this.tiles = new Dictionary<int, TileWrapper>();

        Object[] tiles = Resources.LoadAll("Tiles", typeof(TileBase));

        int counter = 0;
        foreach(Object tile in tiles)
        {
            this.tiles.Add(counter, new TileWrapper((Tile)tile));
            counter++;
        }
    }
    private void InitializeRules()
    {
        foreach(TileWrapper tileWrapper in tiles.Values)
        {
            Tile tile = tileWrapper.tile;
            if(tile.name == "GreySquare")
            {
                tileWrapper.SetSides(new string[,] { 
                    { "","","" }, 
                    { "","","" }, 
                    { "","","" }, 
                    { "","","" } });
            }
            if (tile.name == "grasstile")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" } });
            }
            else if (tile.name == "rivertile2")
            {
                tileWrapper.SetSides(new string[,] {
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile3")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","WaterGrass" },
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile4")
            {
                tileWrapper.SetSides(new string[,] {
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" } });
            }
            else if (tile.name == "rivertile5")
            {
                tileWrapper.SetSides(new string[,] {
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" } });
            }
            else if (tile.name == "rivertile6")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Water","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Water" } });
            }
            else if (tile.name == "rivertile7")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" } });
            }
            else if (tile.name == "rivertile8")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile9")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Water","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","Water" } });
            }
            else if (tile.name == "rivertile10")
            {
                tileWrapper.SetSides(new string[,] {
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile11")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile12")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile13")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","WaterGrass" },
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","WaterGrass" } });
            }
            else if (tile.name == "rivertile14")
            {
                tileWrapper.SetSides(new string[,] {
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" } });
            }
            else if (tile.name == "rivertile15")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Water","Water","WaterGrass" },
                    { "WaterGrass","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","Water" } });
            }
            else if (tile.name == "rivertile16")
            {
                tileWrapper.SetSides(new string[,] {
                    { "WaterGrass","Water","Water" },
                    { "Water","Water","WaterGrass" },
                    { "ShortGrass","ShortGrass","ShortGrass" },
                    { "WaterGrass","Water","WaterGrass" } });
            }
        }
    }
    private void InitializeGrid()
    {
        for (int row = 0; row < grid.GetLength(0); row++)
        {
            for (int column = 0; column < grid.GetLength(1); column++)
            {
                grid[row, column] = new Cell(this, row, column);
            }
        }
    }

    private void InitializeMarkers()
    {
        markers = new Dictionary<string, string[]>();

        markers.Add("Water", new string[] { "Water" });
        markers.Add("ShortGrass", new string[] { "Grass" });
        markers.Add("Grass", new string[] { "ShortGrass" , "Grass"});
        markers.Add("WaterGrass", new string[] { "WaterGrass" });
    }

    private void Initialize()
    {
        this.tilemap.ClearAllTiles();

        InitializeTiles();
        InitializeGrid();
        InitializeRules();
        InitializeMarkers();
    }

    private void Update()
    {
        if (state == State.Idle) return;
        if(Time.time - lastAction > actionCooldown)
        {
            lastAction = Time.time;

            if (state == State.Collapsing)
            {
                string status = CollapseNextCell();

                if (status == "FINISHED")
                {
                    state = State.Idle;
                }
                else if (status == "BACKTRACK")
                {
                    state = State.Backtracking;
                }
                else if (status == "ERROR")
                {
                    Debug.Log("error");
                }
            }

            else if (state == State.Backtracking)
            {
                if(collapsedCells[0].options.Count > 1)
                {
                    collapsedCells[0].ReCollapse();
                    this.state = State.Collapsing;
                }
                else
                {
                    collapsedCells[0].Reset();
                }
            }

            DisplayGrid();
        }
    }

    public void Run(int width, int height, float speed)
    {
        this.grid = new Cell[height, width];
        this.actionCooldown = speed;

        Initialize();

        this.state = State.Collapsing;
    }
}
