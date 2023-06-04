
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