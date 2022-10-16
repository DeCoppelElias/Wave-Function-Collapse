using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class WaveFunctionCollapse : MonoBehaviour
{
    private enum State { Idle, Collapsing, Backtracking}
    private State state = State.Idle;

    public enum Side { TOP, RIGHT, BOTTOM, LEFT }

    private Dictionary<string, string[]> markers = new Dictionary<string, string[]>();

    private Cell[,] grid = new Cell[0, 0];
    private Dictionary<int, TileWrapper> tiles = new Dictionary<int, TileWrapper>();

    private TextMeshPro[,] textMeshGrid;

    private List<Cell> collapsedCells = new List<Cell>();

    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private Tile empty;
    [SerializeField]
    private Tile error;
    [SerializeField]
    private GameObject debugTextPrefab;

    [SerializeField]
    private float actionCooldown = 0f;
    [SerializeField]
    private float lastAction = 1;
    [SerializeField]
    private int stepsPerFrame = 1;

    public TileWrapper getTileWrapper(int i)
    {
        return tiles[i];
    }

    public Side ReverseSide(Side s)
    {
        if (s == Side.TOP) return Side.BOTTOM;
        else if (s == Side.RIGHT) return Side.LEFT;
        else if (s == Side.BOTTOM) return Side.TOP;
        else return Side.RIGHT;
    }

    public Cell GetCell(int row, int column)
    {
        if (row < 0 || row >= this.grid.GetLength(0)) throw new System.Exception("Row out of bounds");
        if (column < 0 || column >= this.grid.GetLength(1)) throw new System.Exception("Column out of bounds");
        return this.grid[row,column];
    }

    public Cell GetCell(Vector3 position)
    {
        Vector3Int roundedPosition = Vector3Int.FloorToInt(position);
        if (!InsideBounds(roundedPosition)) return null;
        return this.grid[this.grid.GetLength(0) - 1 - roundedPosition.y, roundedPosition.x];
    }

    public bool InsideBounds(int row, int column)
    {
        if (row < 0 || row >= this.grid.GetLength(0)) return false;
        if (column < 0 || column >= this.grid.GetLength(1)) return false;
        return true;
    }

    public bool InsideBounds(Vector3 position)
    {
        if (position.x < 0 || position.x >= this.grid.GetLength(1)) return false;
        if (position.y < 0 || position.y >= this.grid.GetLength(0)) return false;
        return true;
    }

    public void AddToCollapsedCells(Cell cell)
    {
        this.collapsedCells.Insert(0,cell);
    }

    public void RemoveFromCollapsedCells(Cell cell)
    {
        this.collapsedCells.Remove(cell);
    }

    public string[] GetMarker(string key)
    {
        return this.markers[key];
    }

    public int getTileCount()
    {
        return this.tiles.Count;
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
        foreach(int option in cell.options.Keys)
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

                DisplayCell(currentCell, new Vector3Int(column, grid.GetLength(0) - 1 - row, 0));
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

            this.textMeshGrid[cell.row, cell.column].text = "";
        }
        else
        {
            tilemap.SetTile(position, empty);

            this.textMeshGrid[cell.row, cell.column].text = cell.GetOptionsString();
        }
    }

    public void DisplayError(int row, int column)
    {
        Vector3Int position = new Vector3Int(column, this.grid.GetLength(0) - 1 - row);
        this.tilemap.SetTile(position, error);
    }

    private void InitializeTiles()
    {
        this.tiles = new Dictionary<int, TileWrapper>();

        Object[] tiles = Resources.LoadAll("Tiles", typeof(TileBase));

        int counter = 0;
        foreach(Object tileObject in tiles)
        {
            Tile tile = (Tile)tileObject;
            this.tiles.Add(counter, new TileWrapper(tile));
            Debug.Log("Tile: " + tile.name + " = " + counter);
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
        InitializeRules();
        InitializeMarkers();
    }

    private void ResetDebugUi()
    {
        textMeshGrid = new TextMeshPro[this.grid.GetLength(0), this.grid.GetLength(1)];

        GameObject parent = GameObject.Find("TextObjects");
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Destroy(parent.transform.GetChild(i).gameObject);
        }

        for (int row = 0; row < this.grid.GetLength(0); row++)
        {
            for (int column = 0; column < this.grid.GetLength(1); column++)
            {
                GameObject textGameObject = Instantiate(debugTextPrefab, new Vector3(column + 0.5f, this.grid.GetLength(0) - 1 - row + 0.5f, 0), Quaternion.identity, parent.transform);
                TextMeshPro textMesh = textGameObject.GetComponent<TextMeshPro>();
                textMesh.text = "";

                textMeshGrid[row, column] = textMesh;
            }
        }
    }
    private void ResetGrid(int width, int height)
    {
        this.grid = new Cell[height, width];

        for (int row = 0; row < grid.GetLength(0); row++)
        {
            for (int column = 0; column < grid.GetLength(1); column++)
            {
                grid[row, column] = new Cell(this, row, column);
            }
        }
    }
    private void ResetWaveFunctionCollapse(int width, int height)
    {
        this.tilemap.ClearAllTiles();
        ResetGrid(width,height);
        ResetDebugUi();
    }
    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (state == State.Idle) return;
        if(Time.time - lastAction > actionCooldown)
        {
            int counter = 0;
            while(counter < stepsPerFrame)
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
                    if (collapsedCells[0].getRealOptionsCount() > 1)
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

                counter++;
            }
        }
    }

    public void Pause()
    {
        this.state = State.Idle;
    }

    public void Continue()
    {
        this.state = State.Collapsing;
    }

    public void Run(int width, int height, float speed)
    {
        this.actionCooldown = speed;

        ResetWaveFunctionCollapse(width,height);

        this.state = State.Collapsing;

        /*CollapseNextCell();
        DisplayGrid();
        Invoke("Test", 1);*/
    }

    public void Test()
    {
        collapsedCells[0].UnCollapse();
        DisplayGrid();
    }
}
