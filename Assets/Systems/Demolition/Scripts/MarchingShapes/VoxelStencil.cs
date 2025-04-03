using UnityEngine;

[System.Serializable]
public class VoxelStencil
{
    public int radius;
    public int centreX, centreY;
    public int XStart => centreX - radius;
    public int XEnd => centreX + radius;
    public int YStart => centreY - radius;
    public int YEnd => centreY + radius;

    public bool isCircular;
    public int sqrRadius;

    public bool fillType;

    public void Initialise()
    {
        if (isCircular)
        {
            sqrRadius = radius * radius;
        }
    }
    public void SetCentre(int x, int y)
    {
        centreX = x;
        centreY = y;
    }
    public bool Apply(int x, int y, bool voxel)
    {
        if (isCircular)
        {
            x -= centreX;
            y -= centreY;
            if(x * x + y * y <= sqrRadius)
            {
                return fillType;
            }
            else
            {
                return voxel;
            }
        }
        return fillType;
    }
}
