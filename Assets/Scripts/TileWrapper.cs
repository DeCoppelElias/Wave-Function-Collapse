using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static WaveFunctionCollapse;

public class TileWrapper
{
    public Tile tile;

    //[TOP, RIGHT, BOTTOM, LEFT] if not rotated
    public string[,] sides = new string[4, 3];

    public TileWrapper(Tile tile)
    {
        this.tile = tile;
    }

    // Rotation is counter clockwise!!
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

    public string[] GetSide(Side side, Quaternion rotation)
    {
        string[,] sides = GetSides(rotation);

        int rowNumber = 0;
        if (side == Side.RIGHT) rowNumber = 1;
        else if (side == Side.BOTTOM) rowNumber = 2;
        else if (side == Side.LEFT) rowNumber = 3;

        return Enumerable.Range(0, sides.GetLength(1))
                .Select(x => sides[rowNumber, x])
                .ToArray();
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
                shiftStrings[row, column] = strings[(row + amount) % strings.GetLength(0), column];
            }
        }
        return shiftStrings;
    }
}
