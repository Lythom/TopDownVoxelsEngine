using System.Collections.Generic;
using System.Numerics;

public struct CellPosition {
    public int X;
    public int Y;
    public int Z;

    public CellPosition(int x, int y, int z) {
        X = x;
        Y = y;
        Z = z;
    }

    public void Deconstruct(out int x, out int y, out int z) {
        x = X;
        y = Y;
        z = Z;
    }
}

public struct Cell {
    public string BlockDefinition;
    public int DamageLevel;
    public Vector3[] Neighbours;

    public Cell(string idx) {
        BlockDefinition = idx;
        DamageLevel = 0;
        Neighbours = new Vector3[6];
    }
}