using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaveFunctionCollapse : MonoBehaviour
{
    private enum State { Idle, Running}
    private State state = State.Idle;

    private Cell[,] grid = new Cell[10, 10];
    private Dictionary<int, TileWrapper> tiles = new Dictionary<int, TileWrapper>();

    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private Tile empty;

    private float collapseCooldown = 0f;
    private float lastCollapse = 1;

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

        public void Collapse()
        {
            this.collapsed = true;
            if (options.Count == 0) throw new System.Exception("No fitting option was found");

            int r = Random.Range(0, options.Count);

            this.pick = options[r];

            this.rotation = CalculateValidRotation(this.pick);
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

        private void AddRule(Side side, string[] rule)
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
        }

        public void UpdateOptions(Side side, string[] rule)
        {
            AddRule(side, rule);

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
                        if (marker == s) valid = false;
                    }
                    else
                    {
                        if (currentRule != "" && marker != currentRule) valid = false;
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

        private int ContainsTile(string name)
        {
            foreach(int option in this.options)
            {
                if (wfc.tiles[option].tile.name == name) return option;
            }

            return -1;
        }
    }

    private TileWrapper getTileWrapper(int i)
    {
        return tiles[i];
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

    private void CollapseNextCell()
    {
        Cell nextCell = NextCell();

        if (nextCell == null)
        {
            state = State.Idle;
            return;
        }

        nextCell.Collapse();

        UpdateNeighborCells(nextCell);

        DisplayGrid();
    }

    private void UpdateNeighborCells(Cell cell)
    {
        TileWrapper tileWrapper = tiles[cell.pick];

        string[,] rotatedSides = tileWrapper.GetSides(cell.rotation);

        // TOP
        int newRow = cell.row - 1;
        int newColumn = cell.column;
        if(newRow >= 0 && newRow < grid.GetLength(0)
            && newColumn >= 0 && newColumn < grid.GetLength(1))
        {
            Cell neighborCell = grid[newRow, newColumn];
            if (!neighborCell.collapsed)
            {
                string[] rule = new string[3];
                for(int i = 0; i < rule.Length; i++)
                {
                    rule[i] = rotatedSides[0, i];
                }

                neighborCell.UpdateOptions(Side.BOTTOM, rule);

                //DebugCell(neighborCell);
            }
        }

        // RIGHT
        newRow = cell.row;
        newColumn = cell.column + 1;
        if (newRow >= 0 && newRow < grid.GetLength(0)
            && newColumn >= 0 && newColumn < grid.GetLength(1))
        {
            Cell neighborCell = grid[newRow, newColumn];

            if (!neighborCell.collapsed)
            {
                string[] rule = new string[3];
                for (int i = 0; i < rule.Length; i++)
                {
                    rule[i] = rotatedSides[1, i];
                }

                neighborCell.UpdateOptions(Side.LEFT, rule);

                //DebugCell(neighborCell);
            }
        }

        // BOTTOM
        newRow = cell.row + 1;
        newColumn = cell.column;
        if (newRow >= 0 && newRow < grid.GetLength(0)
            && newColumn >= 0 && newColumn < grid.GetLength(1))
        {
            Cell neighborCell = grid[newRow, newColumn];

            if (!neighborCell.collapsed)
            {
                string[] rule = new string[3];
                for (int i = 0; i < rule.Length; i++)
                {
                    rule[i] = rotatedSides[2, i];
                }

                neighborCell.UpdateOptions(Side.TOP, rule);

                //DebugCell(neighborCell);
            }
        }

        // LEFT
        newRow = cell.row;
        newColumn = cell.column - 1;
        if (newRow >= 0 && newRow < grid.GetLength(0)
            && newColumn >= 0 && newColumn < grid.GetLength(1))
        {
            Cell neighborCell = grid[newRow, newColumn];

            if (!neighborCell.collapsed)
            {
                string[] rule = new string[3];
                for (int i = 0; i < rule.Length; i++)
                {
                    rule[i] = rotatedSides[3, i];
                }

                neighborCell.UpdateOptions(Side.RIGHT, rule);

                //DebugCell(neighborCell);
            }
        }

        //Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
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
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Grass" } });
            }
            else if (tile.name == "rivertile3")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Water","Water" },
                    { "Water","Water","Grass" },
                    { "Grass","Water","Water" },
                    { "Water","Water","Grass" } });
            }
            else if (tile.name == "rivertile4")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Grass","Grass" } });
            }
            else if (tile.name == "rivertile5")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Water" },
                    { "Water","Water","Grass" },
                    { "Grass","Grass","Grass" } });
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
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" } });
            }
            else if (tile.name == "rivertile8")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Grass" } });
            }
            else if (tile.name == "rivertile9")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Water","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Water" } });
            }
            else if (tile.name == "rivertile10")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Grass" } });
            }
            else if (tile.name == "rivertile11")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Grass" } });
            }
            else if (tile.name == "rivertile12")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Grass" } });
            }
            else if (tile.name == "rivertile13")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Water","Grass" },
                    { "Grass","Water","Water" },
                    { "Water","Water","Water" },
                    { "Water","Water","Grass" } });
            }
            else if (tile.name == "rivertile14")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Grass","Grass" } });
            }
            else if (tile.name == "rivertile15")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Water","Water","Grass" },
                    { "Grass","Water","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Water" } });
            }
            else if (tile.name == "rivertile16")
            {
                tileWrapper.SetSides(new string[,] {
                    { "Grass","Water","Water" },
                    { "Water","Water","Grass" },
                    { "Grass","Grass","Grass" },
                    { "Grass","Water","Grass" } });
            }
        }
    }
    private void InitialzeGrid()
    {
        for (int row = 0; row < grid.GetLength(0); row++)
        {
            for (int column = 0; column < grid.GetLength(1); column++)
            {
                grid[row, column] = new Cell(this, row, column);
            }
        }
    }

    private void Initialize()
    {
        this.tilemap.ClearAllTiles();

        InitializeTiles();
        InitialzeGrid();
        InitializeRules();
    }

    private void Update()
    {
        if(state == State.Running && Time.time - lastCollapse > collapseCooldown)
        {
            CollapseNextCell();

            lastCollapse = Time.time;
        }
    }

    public void Run(int width, int height)
    {
        this.grid = new Cell[height, width];

        Initialize();

        this.state = State.Running;
    }
}
