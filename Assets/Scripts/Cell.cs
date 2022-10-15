using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WaveFunctionCollapse;

public class Cell
{
    public WaveFunctionCollapse wfc;

    public bool collapsed = false;
    public Dictionary<int,List<Quaternion>> options = new Dictionary<int, List<Quaternion>>();

    public int pick = -1;
    public Quaternion rotation = Quaternion.identity;

    public int row;
    public int column;

    public List<int> wrongPicks = new List<int>();

    // [TOP, RIGHT, BOTTOM, LEFT] no rotation
    // Each side will have 3 points that connect to other points
    public string[,] rules = new string[4, 3];

    public Cell(WaveFunctionCollapse wfc, int row, int column)
    {
        this.wfc = wfc;

        this.options = new Dictionary<int, List<Quaternion>>();
        List<Quaternion> rotations = new Quaternion[] { Quaternion.identity, Quaternion.Euler(0, 0, 90), Quaternion.Euler(0, 0, 180), Quaternion.Euler(0, 0, 270) }.ToList();
        for (int i = 0; i < wfc.getTileCount(); i++)
        {
            this.options.Add(i, rotations);
        }

        this.collapsed = false;
        this.pick = -1;

        this.row = row;
        this.column = column;

        for (int i = 0; i < rules.GetLength(0); i++)
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
            wfc.DisplayError(row,column);
            return "BACKTRACK";
        }
        else
        {
            int r = Random.Range(0, options.Count);
            KeyValuePair<int, List<Quaternion>> keyValuePair = options.ElementAt(r);

            this.pick = keyValuePair.Key;

            r = Random.Range(0, keyValuePair.Value.Count);
            this.rotation = keyValuePair.Value[r];

            this.collapsed = true;
            wfc.AddToCollapsedCells(this);

            UpdateNeighborAfterCollapse();

            return "STEP";
        }
    }
    public string ReCollapse()
    {
        DebugOptions();

        Debug.Log("Last pick: " + this.pick);

        int lastPick = this.pick;

        UnCollapse();

        wrongPicks.Add(lastPick);

        foreach (int wrongPick in wrongPicks)
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
        if (wfc.InsideBounds(newRow,newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.RemoveRule(Side.BOTTOM);

                //DebugCell(neighborCell);
            }
        }

        // RIGHT
        newRow = this.row;
        newColumn = this.column + 1;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.RemoveRule(Side.LEFT);

                //DebugCell(neighborCell);
            }
        }

        // BOTTOM
        newRow = this.row + 1;
        newColumn = this.column;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.RemoveRule(Side.TOP);

                //DebugCell(neighborCell);
            }
        }

        // LEFT
        newRow = this.row;
        newColumn = this.column - 1;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.RemoveRule(Side.RIGHT);

                //DebugCell(neighborCell);
            }
        }

        return "STEP";
    }

    private void UpdateNeighborAfterCollapse()
    {
        if (!collapsed) return;

        TileWrapper tileWrapper = wfc.getTileWrapper(this.pick);

        string[,] rotatedSides = tileWrapper.GetSides(this.rotation);

        // TOP
        int newRow = this.row - 1;
        int newColumn = this.column;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
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
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
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
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
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
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
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

    private void UpdateNeighborAfterOptionReduce()
    {
        if (collapsed) return;

        // TOP
        int newRow = this.row - 1;
        int newColumn = this.column;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.UpdateOptions(Side.BOTTOM, this.options);
            }
        }

        // RIGHT
        newRow = this.row;
        newColumn = this.column + 1;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.UpdateOptions(Side.LEFT, this.options);
            }
        }

        // BOTTOM
        newRow = this.row + 1;
        newColumn = this.column;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.UpdateOptions(Side.TOP, this.options);
            }
        }

        // LEFT
        newRow = this.row;
        newColumn = this.column - 1;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.UpdateOptions(Side.RIGHT, this.options);
            }
        }

        //Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
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
            for (int i = 0; i < rules.GetLength(1); i++)
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
        this.options = new Dictionary<int, List<Quaternion>>();
        List<Quaternion> rotations = new Quaternion[] { Quaternion.identity, Quaternion.Euler(0, 0, 90), Quaternion.Euler(0, 0, 180), Quaternion.Euler(0, 0, 270) }.ToList();
        for (int i = 0; i < wfc.getTileCount(); i++)
        {
            this.options.Add(i, rotations);
        }
    }

    private void UpdateOptions()
    {
        Dictionary<int, List<Quaternion>> newOptions = new Dictionary<int, List<Quaternion>>();

        foreach (int option in this.options.Keys)
        {
            List<Quaternion> validRotations = new List<Quaternion>();

            Quaternion rotation = Quaternion.identity;
            if(CheckRules(option, rotation))
            {
                validRotations.Add(rotation);
            }

            rotation = Quaternion.Euler(0, 0, 90);
            if (CheckRules(option, rotation))
            {
                validRotations.Add(rotation);
            }

            rotation = Quaternion.Euler(0, 0, 180);
            if (CheckRules(option, rotation))
            {
                validRotations.Add(rotation);
            }

            rotation = Quaternion.Euler(0, 0, 270);
            if (CheckRules(option, rotation))
            {
                validRotations.Add(rotation);
            }

            if (validRotations.Count > 0) newOptions.Add(option, validRotations);
        }

        int previousOptionsCount = this.options.Count;
        this.options = newOptions;
        if (this.options.Count != previousOptionsCount) UpdateNeighborAfterOptionReduce();

    }
    private void UpdateOptions(Side side, Dictionary<int, List<Quaternion>> neighborOptions)
    {
        Dictionary<int, List<Quaternion>> newOptions = new Dictionary<int, List<Quaternion>>();

        foreach (int neighborOption in neighborOptions.Keys)
        {
            TileWrapper neighborTileWrapper = wfc.getTileWrapper(neighborOption);
            foreach (Quaternion neighborRotation in neighborOptions[neighborOption])
            {
                string[] neighborSideMarkers = neighborTileWrapper.GetSide(side, rotation);

                foreach (int option in this.options.Keys)
                {
                    List<Quaternion> validRotations = new List<Quaternion>();

                    TileWrapper tileWrapper = wfc.getTileWrapper(option);

                    Quaternion rotation = Quaternion.identity;
                    string[] sideMarkers = tileWrapper.GetSide(side, rotation);
                    if (CheckRule(sideMarkers, neighborSideMarkers))
                    {
                        validRotations.Add(rotation);
                    }

                    rotation = Quaternion.Euler(0,0,90);
                    sideMarkers = tileWrapper.GetSide(side, rotation);
                    if (CheckRule(sideMarkers, neighborSideMarkers))
                    {
                        validRotations.Add(rotation);
                    }

                    rotation = Quaternion.Euler(0, 0, 180);
                    sideMarkers = tileWrapper.GetSide(side, rotation);
                    if (CheckRule(sideMarkers, neighborSideMarkers))
                    {
                        validRotations.Add(rotation);
                    }

                    rotation = Quaternion.Euler(0, 0, 270);
                    sideMarkers = tileWrapper.GetSide(side, rotation);
                    if (CheckRule(sideMarkers, neighborSideMarkers))
                    {
                        validRotations.Add(rotation);
                    }

                    if (validRotations.Count > 0)
                    {
                        if (!newOptions.ContainsKey(option)) newOptions.Add(option, validRotations);
                        else
                        {
                            foreach (Quaternion validRotation in validRotations)
                            {
                                if (!newOptions[option].Contains(validRotation)) newOptions[option].Add(validRotation);
                            }
                        }
                    }
                }
            }
        }
        
        int previousOptionsCount = this.options.Count;
        this.options = newOptions;
        if (this.options.Count != previousOptionsCount) UpdateNeighborAfterOptionReduce();
    }

    private bool CheckRules(int option, Quaternion rotation)
    {
        int shiftAmount = Mathf.FloorToInt(rotation.eulerAngles.z / 90);

        TileWrapper tileWrapper = wfc.getTileWrapper(option);
        bool valid = true;

        string[,] rotatedSides = ShiftArrayRight(tileWrapper.sides, shiftAmount);
        for (int i = 0; i < rules.GetLength(0); i++)
        {
            for (int j = 0; j < rules.GetLength(1); j++)
            {
                string currentRule = rules[i, j];
                string marker = rotatedSides[i, rules.GetLength(1) - 1 - j];
                if (currentRule.Contains("-"))
                {
                    string s = currentRule.Remove(0, 1);
                    if (wfc.GetMarker(marker).Contains(s)) valid = false;
                }
                else
                {
                    if (currentRule != "" && !wfc.GetMarker(marker).Contains(currentRule)) valid = false;
                }
            }
        }

        return valid;
    }

    private bool CheckRule(string[] sideRule, string[] otherSideRules)
    {
        for (int j = 0; j < rules.GetLength(1); j++)
        {
            string currentRule = sideRule[j];
            string marker = otherSideRules[otherSideRules.Length - 1 - j];
            if (currentRule.Contains("-"))
            {
                string s = currentRule.Remove(0, 1);
                if (wfc.GetMarker(marker).Contains(s)) return false;
            }
            else
            {
                if (currentRule != "" && !wfc.GetMarker(marker).Contains(currentRule)) return false;
            }
        }

        return true;
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
        Debug.Log(GetOptionsString());
    }

    public string GetOptionsString()
    {
        string optionsString = "";
        foreach (int option in this.options.Keys)
        {
            optionsString += option + "/";
        }
        return optionsString;
    }
}