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
    public Dictionary<int, List<Quaternion>> optionsBeforeCollapse = new Dictionary<int, List<Quaternion>>();

    public int pick = -1;
    public Quaternion rotation = Quaternion.identity;

    public int row;
    public int column;

    public Dictionary<int, List<Quaternion>> wrongPicks = new Dictionary<int, List<Quaternion>>();

    // [TOP, RIGHT, BOTTOM, LEFT] no rotation
    // Each side will have 3 points that connect to other points
    public string[,] rules = new string[4, 3];

    public Cell(WaveFunctionCollapse wfc, int row, int column)
    {
        this.wfc = wfc;

        this.options = new Dictionary<int, List<Quaternion>>();
        for (int i = 0; i < wfc.getTileCount(); i++)
        {
            List<Quaternion> rotations = new Quaternion[] { Quaternion.identity, Quaternion.Euler(0, 0, 90), Quaternion.Euler(0, 0, 180), Quaternion.Euler(0, 0, 270) }.ToList();
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

    public int getRealOptionsCount()
    {
        // Make sure when recollapsing that previous options are not repicked
        Dictionary<int, List<Quaternion>> realOptions = Clone(this.options);
        foreach (int wrongOption in this.wrongPicks.Keys)
        {
            foreach (Quaternion rotation in this.wrongPicks[wrongOption])
            {
                if (realOptions.ContainsKey(wrongOption) && realOptions[wrongOption].Contains(rotation))
                {
                    realOptions[wrongOption].Remove(rotation);
                    if (realOptions[wrongOption].Count == 0) realOptions.Remove(wrongOption);
                }
            }
        }
        return realOptions.Count;
    }

    private Dictionary<int, List<Quaternion>> Clone(Dictionary<int, List<Quaternion>> options)
    {
        Dictionary<int, List<Quaternion>> cloneOptions = new Dictionary<int, List<Quaternion>>();
        foreach(KeyValuePair<int, List<Quaternion>> keyVal in options)
        {
            int option = keyVal.Key;
            List<Quaternion> rotations = keyVal.Value;

            List<Quaternion> cloneRotations = new List<Quaternion>();
            foreach(Quaternion rotation in rotations)
            {
                cloneRotations.Add(rotation);
            }

            cloneOptions.Add(option, cloneRotations);
        }

        return cloneOptions;
    }

    public string Collapse()
    {
        if (collapsed) return "ERROR";

        // Make sure when recollapsing that previous options are not repicked
        Dictionary<int, List<Quaternion>> realOptions = Clone(this.options);
        foreach (int wrongOption in this.wrongPicks.Keys)
        {
            foreach(Quaternion rotation in this.wrongPicks[wrongOption])
            {
                if(realOptions.ContainsKey(wrongOption) && realOptions[wrongOption].Contains(rotation))
                {
                    realOptions[wrongOption].Remove(rotation);
                    if (realOptions[wrongOption].Count == 0) realOptions.Remove(wrongOption);
                }
            }
        }

        // If no options remain, time to backtrack
        if (realOptions.Count == 0)
        {
            wfc.DisplayError(row,column);
            return "BACKTRACK";
        }

        // Select random pick from options and update neighbours
        else
        {
            int r = Random.Range(0, realOptions.Count);
            KeyValuePair<int, List<Quaternion>> keyValuePair = realOptions.ElementAt(r);

            this.pick = keyValuePair.Key;

            try
            {
                r = Random.Range(0, keyValuePair.Value.Count);
                this.rotation = keyValuePair.Value[r];
            }
            catch(System.Exception e)
            {
                Debug.Log("-------------------");
                Debug.Log(GetLongOptionsString(this.options));
                Debug.Log(GetLongOptionsString(realOptions));
                Debug.Log("-------------------");
            }

            this.collapsed = true;
            optionsBeforeCollapse = Clone(this.options);
            wfc.AddToCollapsedCells(this);

            UpdateNeighborAfterCollapse();

            return "STEP";
        }
    }
    public string ReCollapse()
    {
        int lastPick = this.pick;
        Quaternion lastRotation = this.rotation;

        UnCollapse();

        if (!wrongPicks.ContainsKey(lastPick)) wrongPicks.Add(lastPick, new Quaternion[] { lastRotation }.ToList());
        else
        {
            if (!wrongPicks[lastPick].Contains(lastRotation)) wrongPicks[lastPick].Add(lastRotation);
        }

        string status = Collapse();

        return status;
    }
    public void Reset()
    {
        this.wrongPicks = new Dictionary<int, List<Quaternion>>();
        UnCollapse();
    }
    public string UnCollapse()
    {
        if (!collapsed) return "ERROR";

        this.pick = -1;
        this.collapsed = false;
        this.rotation = Quaternion.identity;

        this.options = new Dictionary<int, List<Quaternion>>(this.optionsBeforeCollapse);
        this.optionsBeforeCollapse = new Dictionary<int, List<Quaternion>>();

        wfc.RemoveFromCollapsedCells(this);

        UpdateNeighborsAfterUnCollapse();

        return "STEP";
    }
    private void UpdateNeighborsAfterUnCollapse()
    {
        // TOP
        int newRow = this.row - 1;
        int newColumn = this.column;
        if (wfc.InsideBounds(newRow, newColumn))
        {
            Cell neighborCell = wfc.GetCell(newRow, newColumn);
            if (!neighborCell.collapsed)
            {
                neighborCell.RemoveRule(Side.BOTTOM, this.options);
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
                neighborCell.RemoveRule(Side.LEFT, this.options);
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
                neighborCell.RemoveRule(Side.TOP, this.options);
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
                neighborCell.RemoveRule(Side.RIGHT, this.options);
            }
        }
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
                int previousNeighborOptionsCount = neighborCell.options.Count;
                neighborCell.UpdateOptionsNeighborOptions(Side.BOTTOM, this.options);
                int currentNeighborOptionsCount = neighborCell.options.Count;
                if (previousNeighborOptionsCount > currentNeighborOptionsCount && currentNeighborOptionsCount != 0) neighborCell.UpdateNeighborAfterOptionReduce();
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
                int previousNeighborOptionsCount = neighborCell.options.Count;
                neighborCell.UpdateOptionsNeighborOptions(Side.LEFT, this.options);
                int currentNeighborOptionsCount = neighborCell.options.Count;
                if (previousNeighborOptionsCount > currentNeighborOptionsCount && currentNeighborOptionsCount != 0) neighborCell.UpdateNeighborAfterOptionReduce();
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
                int previousNeighborOptionsCount = neighborCell.options.Count;
                neighborCell.UpdateOptionsNeighborOptions(Side.TOP, this.options);
                int currentNeighborOptionsCount = neighborCell.options.Count;
                if (previousNeighborOptionsCount > currentNeighborOptionsCount && currentNeighborOptionsCount != 0) neighborCell.UpdateNeighborAfterOptionReduce();
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
                int previousNeighborOptionsCount = neighborCell.options.Count;
                neighborCell.UpdateOptionsNeighborOptions(Side.RIGHT, this.options);
                int currentNeighborOptionsCount = neighborCell.options.Count;
                if (previousNeighborOptionsCount > currentNeighborOptionsCount && currentNeighborOptionsCount != 0) neighborCell.UpdateNeighborAfterOptionReduce();
            }
        }

        //Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
    }
    private void UpdateNeighborAfterOptionIncrease()
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
                int previousOptionsCount = neighborCell.options.Count;
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.BOTTOM, this.options);
                int currentOptionsCount = neighborCell.options.Count;
                if (previousOptionsCount < currentOptionsCount) neighborCell.UpdateNeighborAfterOptionIncrease();
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
                int previousOptionsCount = neighborCell.options.Count;
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.LEFT, this.options);
                int currentOptionsCount = neighborCell.options.Count;
                if (previousOptionsCount < currentOptionsCount) neighborCell.UpdateNeighborAfterOptionIncrease();
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
                int previousOptionsCount = neighborCell.options.Count;
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.TOP, this.options);
                int currentOptionsCount = neighborCell.options.Count;
                if (previousOptionsCount < currentOptionsCount) neighborCell.UpdateNeighborAfterOptionIncrease();
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
                int previousOptionsCount = neighborCell.options.Count;
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.RIGHT, this.options);
                int currentOptionsCount = neighborCell.options.Count;
                if (previousOptionsCount < currentOptionsCount) neighborCell.UpdateNeighborAfterOptionIncrease();
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
        int previousOptionsCount = this.options.Count;
        UpdateOptionsDirectNeighbors();
        int currentOptionsCount = this.options.Count;
        if (previousOptionsCount != currentOptionsCount && currentOptionsCount != 0) UpdateNeighborAfterOptionReduce();
    }

    public void RemoveRule(Side side, Dictionary<int, List<Quaternion>> neighborOptions)
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

        int previousOptionsCount = this.options.Count;
        ResetOptions();
        UpdateOptionsDirectNeighbors();
        UpdateOptionsNeighborOptions(side, neighborOptions);
        int currentOptionsCount = this.options.Count;
        if (previousOptionsCount < currentOptionsCount) UpdateNeighborAfterOptionIncrease();
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
    private void UpdateOptionsDirectNeighbors()
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

        this.options = newOptions;
    }
    private void UpdateOptionsNeighborOptions(Side side, Dictionary<int, List<Quaternion>> neighborOptions)
    {
        Dictionary<int, List<Quaternion>> newOptions = new Dictionary<int, List<Quaternion>>();

        foreach (int neighborOption in neighborOptions.Keys)
        {
            TileWrapper neighborTileWrapper = wfc.getTileWrapper(neighborOption);
            foreach (Quaternion neighborRotation in neighborOptions[neighborOption])
            {
                string[] neighborSideMarkers = neighborTileWrapper.GetSide(wfc.ReverseSide(side), neighborRotation);

                foreach (int option in this.options.Keys)
                {
                    List<Quaternion> validRotations = new List<Quaternion>();
                    TileWrapper tileWrapper = wfc.getTileWrapper(option);

                    List<Quaternion> rotations = this.options[option];
                    foreach(Quaternion rotation in rotations)
                    {
                        string[] sideMarkers = tileWrapper.GetSide(side, rotation);
                        if (CheckRule(sideMarkers, neighborSideMarkers))
                        {
                            validRotations.Add(rotation);
                        }
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
        this.options = newOptions;
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

    public string GetLongOptionsString(Dictionary<int, List<Quaternion>> options)
    {
        string optionsString = "";
        foreach (int option in options.Keys)
        {
            string rotationsString = "";
            foreach(Quaternion rotation in this.options[option])
            {
                rotationsString += rotation.eulerAngles.z + ".";
            }

            optionsString += option + ":" + rotationsString + "/";
        }
        return optionsString;
    }

    public string GetDisplayOptionsString()
    {
        string optionsString = "All Possible Options: \n";
        foreach (int option in options.Keys)
        {
            TileWrapper tileWrapper = wfc.getTileWrapper(option);
            optionsString += tileWrapper.tile.name + ":\n";

            string rotationsString = "Rotations: ";
            for (int i = 0; i < this.options[option].Count; i++)
            {
                Quaternion rotation = this.options[option][i];

                if (i == 0) rotationsString += "{ ";
                rotationsString += rotation.eulerAngles.z;
                if (i == this.options[option].Count - 1) rotationsString += " }";
                else
                {
                    rotationsString += "/";
                }
            }

            optionsString += rotationsString + "\n";
        }

        return optionsString;
    }
}