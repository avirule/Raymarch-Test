using System;

public static class StaticMath
{
    public static int Project1D_XZY(int x, int y, int z, int size) => x + (size * (z + (size * y)));

    public static int Project1D_XYZ(int x, int y, int z, int size) => x + (size * (y + (size * z)));

    public static void Project3D_XZY(int index, int size, out int x, out int y, out int z)
    {
        int xQuotient = Math.DivRem(index, size, out x);
        int zQuotient = Math.DivRem(xQuotient, size, out z);
        y = zQuotient % size;
    }

    public static void Project3D_XYZ(int index, int size, out int x, out int y, out int z)
    {
        int zQuotient = Math.DivRem(index, size, out z);
        int yQuotient = Math.DivRem(zQuotient, size, out y);
        x = yQuotient % size;
    }
}