using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static WaveFunctionCollapse;

public class Cell : System.ICloneable
{
    public class Options : System.ICloneable
    {
        private Dictionary<int, List<Quaternion>> options = new Dictionary<int, List<Quaternion>>();

        public Options() { }

        public void RemoveOption(int option, Quaternion rotation)
        {
            if (this.options.ContainsKey(option))
            {
                if (this.options[option].Contains(rotation))
                {
                    this.options[option].Remove(rotation);

                    if(this.options[option].Count == 0)
                    {
                        this.options.Remove(option);
                    }
                }
            }
        }

        public void RemoveOption(int option, List<Quaternion> rotations)
        {
            if (this.options.ContainsKey(option))
            {
                foreach(Quaternion rotation in rotations)
                {
                    if (this.options[option].Contains(rotation))
                    {
                        this.options[option].Remove(rotation);
                    }
                }
                if (this.options[option].Count == 0)
                {
                    this.options.Remove(option);
                }
            }
        }

        public void RemoveOptions(Options options)
        {
            foreach(int option in options.GetOptions())
            {
                List<Quaternion> rotations = options.getRotations(option);

                RemoveOption(option, rotations);
            }
        }

        public void AddOption(int option, Quaternion rotation)
        {
            if (this.options.ContainsKey(option))
            {
                if (!this.options[option].Contains(rotation))
                {
                    this.options[option].Add(rotation);
                }
            }
            else
            {
                this.options.Add(option, new Quaternion[] { rotation }.ToList());
            }
        }

        public void AddOption(int option, List<Quaternion> rotations)
        {
            if (this.options.ContainsKey(option))
            {
                foreach(Quaternion rotation in rotations)
                {
                    if (!this.options[option].Contains(rotation))
                    {
                        this.options[option].Add(rotation);
                    }
                }
            }
            else
            {
                this.options.Add(option, rotations);
            }
        }

        public List<int> GetOptions()
        {
            List<int> cloneOptions = new List<int>();
            foreach (int option in this.options.Keys)
            {
                cloneOptions.Add(option);
            }

            return cloneOptions;
        }

        public List<Quaternion> getRotations(int option)
        {
            if (!this.options.ContainsKey(option)) return new List<Quaternion>();

            List<Quaternion> rotationsClone = new List<Quaternion>();
            foreach (Quaternion rotation in this.options[option])
            {
                rotationsClone.Add(rotation);
            }
            return rotationsClone;
        }
        
        public int GetCount()
        {
            return this.options.Count;
        }

        public KeyValuePair<int, List<Quaternion>> ElementAt(int i)
        {
            KeyValuePair<int, List<Quaternion>> elem = this.options.ElementAt(i);

            return new KeyValuePair<int, List<Quaternion>>(elem.Key, getRotations(elem.Key));
        }

        public object Clone()
        {
            Options clone = new Options();

            foreach (KeyValuePair<int, List<Quaternion>> keyVal in this.options)
            {
                int option = keyVal.Key;
                List<Quaternion> rotations = keyVal.Value;

                List<Quaternion> cloneRotations = new List<Quaternion>();
                foreach (Quaternion rotation in rotations)
                {
                    cloneRotations.Add(rotation);
                }

                clone.AddOption(option, cloneRotations);
            }

            return clone;
        }

        public override string ToString()
        {
            string optionsString = "";
            foreach (int option in options.Keys)
            {
                string rotationsString = "";
                foreach (Quaternion rotation in this.options[option])
                {
                    rotationsString += rotation.eulerAngles.z + ".";
                }

                optionsString += option + ":" + rotationsString + "/";
            }
            return optionsString;
        }
    }

    public WaveFunctionCollapse wfc;

    public bool collapsed = false;
    public Options options = new Options();
    public Options optionsBeforeCollapse = new Options();
    public Options wrongOptions = new Options();

    public int pick = -1;
    public Quaternion rotation = Quaternion.identity;

    public int row;
    public int column;

    

    // [TOP, RIGHT, BOTTOM, LEFT] no rotation
    // Each side will have 3 points that connect to other points
    public string[,] rules = new string[4, 3];

    public Cell(WaveFunctionCollapse wfc, int row, int column)
    {
        this.wfc = wfc;
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

        Options newOptions = new Options();
        for (int i = 0; i < wfc.getTileCount(); i++)
        {
            List<Quaternion> rotations = wfc.getTileWrapper(i).GetRotations();
            newOptions.AddOption(i, rotations);
        }

        UpdateOptions(newOptions);
    }

    private Cell(WaveFunctionCollapse wfc, int row, int column, bool collapsed, Options options, Options optionsBeforeCollapse, Options wrongOptions, int pick, Quaternion rotation)
    {
        this.wfc = wfc;

        Options newOptions = new Options();
        for (int i = 0; i < wfc.getTileCount(); i++)
        {
            List<Quaternion> rotations = wfc.getTileWrapper(i).GetRotations();
            newOptions.AddOption(i, rotations);
        }

        UpdateOptions(newOptions);

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
        Options realOptions = (Options)this.options.Clone();
        realOptions.RemoveOptions(wrongOptions);
        return realOptions.GetCount();
    }

    public string Collapse()
    {
        if (collapsed) throw new System.Exception("Tried to collapse collapsed cell");

        // Make sure when recollapsing that previous options are not repicked
        Options realOptions = (Options)this.options.Clone();
        realOptions.RemoveOptions(wrongOptions);

        // If no options remain, time to backtrack
        if (realOptions.GetCount() == 0)
        {
            wfc.DisplayError(row,column);
            return "BACKTRACK";
        }

        // Select random pick from options and update neighbours
        else
        {
            int r = Random.Range(0, realOptions.GetCount());
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
                Debug.Log(this.options.ToString());
                Debug.Log(realOptions.ToString());
                Debug.Log("-------------------");
            }

            this.collapsed = true;
            optionsBeforeCollapse = (Options)this.options.Clone();
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

        wrongOptions.AddOption(lastPick, lastRotation);

        string status = Collapse();

        return status;
    }

    private void UpdateOptions(Options options)
    {
        try
        {
            wfc.RemoveCellFromTree(this);
        }
        catch(System.Exception e)
        {
            Debug.Log("Crashed on cell: ");
            wfc.DebugCell(this);
            throw e;
        }
        this.options = options;
        wfc.AddCellToTree(this);
    }
    public void Reset()
    {
        this.wrongOptions = new Options();
        UnCollapse();
    }
    public string UnCollapse()
    {
        if (!collapsed) throw new System.Exception("Tried to uncollapse cell that has not been collapsed"); ;

        this.pick = -1;
        this.collapsed = false;
        this.rotation = Quaternion.identity;

        UpdateOptions((Options)this.optionsBeforeCollapse.Clone());
        this.optionsBeforeCollapse = new Options();

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
                int previousNeighborOptionsCount = neighborCell.options.GetCount();
                neighborCell.UpdateOptionsNeighborOptions(Side.BOTTOM, this.options);
                int currentNeighborOptionsCount = neighborCell.options.GetCount();
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
                int previousNeighborOptionsCount = neighborCell.options.GetCount();
                neighborCell.UpdateOptionsNeighborOptions(Side.LEFT, this.options);
                int currentNeighborOptionsCount = neighborCell.options.GetCount();
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
                int previousNeighborOptionsCount = neighborCell.options.GetCount();
                neighborCell.UpdateOptionsNeighborOptions(Side.TOP, this.options);
                int currentNeighborOptionsCount = neighborCell.options.GetCount();
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
                int previousNeighborOptionsCount = neighborCell.options.GetCount();
                neighborCell.UpdateOptionsNeighborOptions(Side.RIGHT, this.options);
                int currentNeighborOptionsCount = neighborCell.options.GetCount();
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
                int previousOptionsCount = neighborCell.options.GetCount();
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.BOTTOM, this.options);
                int currentOptionsCount = neighborCell.options.GetCount();
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
                int previousOptionsCount = neighborCell.options.GetCount();
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.LEFT, this.options);
                int currentOptionsCount = neighborCell.options.GetCount();
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
                int previousOptionsCount = neighborCell.options.GetCount();
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.TOP, this.options);
                int currentOptionsCount = neighborCell.options.GetCount();
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
                int previousOptionsCount = neighborCell.options.GetCount();
                neighborCell.ResetOptions();
                neighborCell.UpdateOptionsDirectNeighbors();
                neighborCell.UpdateOptionsNeighborOptions(Side.RIGHT, this.options);
                int currentOptionsCount = neighborCell.options.GetCount();
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

        // Add rule to rules class variable
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

        // Update the options after adding a rule.
        // Options decreased => update neighbors of this cell.
        int previousOptionsCount = this.options.GetCount();
        UpdateOptionsRule(side, rule);
        int currentOptionsCount = this.options.GetCount();
        if (previousOptionsCount != currentOptionsCount && currentOptionsCount != 0) UpdateNeighborAfterOptionReduce();


        /*if (side == Side.TOP)
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
        if (previousOptionsCount != currentOptionsCount && currentOptionsCount != 0) UpdateNeighborAfterOptionReduce();*/
    }

    public void RemoveRule(Side side, Options neighborOptions)
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

        int previousOptionsCount = this.options.GetCount();
        ResetOptions();
        UpdateOptionsDirectNeighbors();
        UpdateOptionsNeighborOptions(side, neighborOptions);
        int currentOptionsCount = this.options.GetCount();
        if (previousOptionsCount < currentOptionsCount) UpdateNeighborAfterOptionIncrease();
    }

    private void ResetOptions()
    {
        Options newOptions = new Options();
        for (int i = 0; i < wfc.getTileCount(); i++)
        {
            List<Quaternion> rotations = wfc.getTileWrapper(i).GetRotations();
            newOptions.AddOption(i, rotations);
        }

        UpdateOptions(newOptions);
    }

    private void UpdateOptionsRule(Side side, string[] rule)
    {
        Options newOptions = new Options();

        foreach (int option in this.options.GetOptions())
        {
            TileWrapper tileWrapper = wfc.getTileWrapper(option);
            List<Quaternion> rotations = this.options.getRotations(option);

            List<Quaternion> validRotations = new List<Quaternion>();

            foreach (Quaternion rotation in rotations)
            {
                string[] sideRule = tileWrapper.GetSide(side, rotation);
                if (CheckRule(sideRule, rule))
                {
                    validRotations.Add(rotation);
                }
            }

            if (validRotations.Count > 0) newOptions.AddOption(option, validRotations);
        }

        UpdateOptions(newOptions);
    }
    private void UpdateOptionsDirectNeighbors()
    {
        Options newOptions = new Options();

        foreach (int option in this.options.GetOptions())
        {
            TileWrapper tileWrapper = wfc.getTileWrapper(option);
            List<Quaternion> rotations = this.options.getRotations(option);

            List<Quaternion> validRotations = new List<Quaternion>();

            foreach (Quaternion rotation in rotations)
            {
                if (CheckRules(option, rotation))
                {
                    validRotations.Add(rotation);
                }
            }

            if (validRotations.Count > 0) newOptions.AddOption(option, validRotations);
        }

        UpdateOptions(newOptions);
    }
    private void UpdateOptionsNeighborOptions(Side side, Options neighborOptions)
    {
        Options newOptions = new Options();

        Options optionsCopy = (Options)this.options.Clone();

        // Check for every neighbor option which options can be placed next to these options
        // If an options cannot be placed next to all neighbor options then that option is no longer possible and will be removed
        foreach (int neighborOption in neighborOptions.GetOptions())
        {
            TileWrapper neighborTileWrapper = wfc.getTileWrapper(neighborOption);

            List<Quaternion> neighborRotations = neighborOptions.getRotations(neighborOption);
            foreach (Quaternion neighborRotation in neighborRotations)
            {
                string[] neighborSideMarkers = neighborTileWrapper.GetSide(wfc.ReverseSide(side), neighborRotation);

                foreach (int option in optionsCopy.GetOptions())
                {
                    List<Quaternion> validRotations = new List<Quaternion>();
                    TileWrapper tileWrapper = wfc.getTileWrapper(option);

                    List<Quaternion> rotations = optionsCopy.getRotations(option);
                    foreach(Quaternion rotation in rotations)
                    {
                        string[] sideMarkers = tileWrapper.GetSide(side, rotation);
                        if (CheckRule(sideMarkers, neighborSideMarkers))
                        {
                            validRotations.Add(rotation);
                            optionsCopy.RemoveOption(option, rotation);
                        }
                    }

                    if (validRotations.Count > 0)
                    {
                        newOptions.AddOption(option, validRotations);
                    }
                }
            }
        }
        UpdateOptions(newOptions);
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

    public string GetOptionsString()
    {
        string optionsString = "";
        foreach (int option in this.options.GetOptions())
        {
            optionsString += option + "/";
        }
        return optionsString;
    }

    public string GetDisplayOptionsString()
    {
        string optionsString = "All Possible Options: \n";
        foreach (int option in options.GetOptions())
        {
            TileWrapper tileWrapper = wfc.getTileWrapper(option);
            optionsString += tileWrapper.tile.name + ":\n";

            string rotationsString = "Rotations: ";
            List<Quaternion> rotations = this.options.getRotations(option);
            for (int i = 0; i < rotations.Count; i++)
            {
                Quaternion rotation = rotations[i];

                if (i == 0) rotationsString += "{ ";
                rotationsString += rotation.eulerAngles.z;
                if (i == rotations.Count - 1) rotationsString += " }";
                else
                {
                    rotationsString += "/";
                }
            }

            optionsString += rotationsString + "\n";
        }

        return optionsString;
    }

    public object Clone()
    {
        Cell cloneCell = new Cell(this.wfc, this.row, this.column, this.collapsed, (Options)this.options.Clone(), (Options)this.optionsBeforeCollapse.Clone(), (Options)this.wrongOptions.Clone(), this.pick, this.rotation);
        return cloneCell;
    }
}