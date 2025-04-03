using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

[System.Serializable]
public class Voxel
{

    public Voxel(float size, int x, int y)
    {
        position.x = (x + 0.5f) * size;
        position.y = (y + 0.5f) * size;

        xEdgePosition = position;
        xEdgePosition.x += size * 0.5f;
        yEdgePosition = position;
        yEdgePosition.y = size * 0.5f;
    }
    public bool state;
    public Vector2 xEdgePosition, yEdgePosition;
    public Vector2 position;
}

[SelectionBase]
public class VoxelGrid : LunarScript
{
    [Tooltip("The array of voxels")]
    public Voxel[] voxels;
    [Tooltip("The size of each square. Lower numbers means higher fidelity.")]
    public int resolution;
    public GameObject voxelPrefab;
    public float voxelSize;
    public Color gizmoColour;
    public float baseIntegrity;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    Mesh mesh;

    List<Vector3> vertices = new();
    List<int> triangles = new();
    public void Initialise(int resolution, float size)
    {
        this.resolution = resolution;
        voxelSize = size / resolution;
        print($"Voxel Size: {voxelSize}");
        voxels = new Voxel[resolution * resolution];
        for (int i = 0, y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++, i++)
            {
                CreateVoxel(i, x, y);
            }
        }
        
        meshFilter = GetComponent<MeshFilter>();
        mesh = new()
        {
            name = "VoxelGrid-" + gameObject.name,
        };
        meshFilter.mesh = mesh;
        vertices = new();
        triangles = new();
        Refresh();
    }
    void CreateVoxel(int i, int x, int y)
    {
        voxels[i] = new(voxelSize, x, y);
    }
    public void Apply(VoxelStencil stencil)
    {
        int xStart = stencil.XStart;
        int yStart = stencil.YStart;
        int xEnd = stencil.XEnd;
        int yEnd = stencil.YEnd;
        if (stencil.XStart < 0)
        {
            xStart = 0;
        }
        if (stencil.XEnd >= resolution)
        {
            xEnd = resolution - 1;
        }
        if (stencil.YStart < 0)
        {
            yStart = 0;
        }
        if (stencil.YEnd >= resolution)
        {
            yEnd = resolution - 1;
        }

        //voxels[y * resolution + x].state = stencil.Apply(x, y);
        for (int y = yStart; y <= yEnd; y++)
        {
            int i = y * resolution + xStart;
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                print(i);
                voxels[i].state = stencil.Apply(x, y, voxels[i].state);
            }
        }

        Refresh();

    }

    void Refresh()
    {
        Triangulate();
    }
    void Triangulate()
    {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();

        TriangulateCellRows();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
    }
    void TriangulateCellRows()
    {
        int cells = resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            for (int x = 0; x < cells; x++, i++)
            {
                TriangulateCell(voxels[i], voxels[i + 1], voxels[i + resolution], voxels[i + resolution + 1]);
            }
        }
    }
    void TriangulateCell(Voxel a, Voxel b, Voxel c, Voxel d)
    {
        int cellType = 0;
        if(a.state)
        {
            cellType |= 1;
        }
        if(b.state)
        {
            cellType |= 2;
        }
        if(c.state)
        {
            cellType |= 4;
        }
        if(d.state)
        {
            cellType |= 8;
        }

        switch (cellType)
        {
            case 0:
                return;
            case 1:
                AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                break;
            case 2:
                AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                break;
            case 4:
                AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                break;
            case 8:
                AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                break;
            case 3:
                AddQuad(a.position, a.yEdgePosition, b.yEdgePosition, b.position);
                break;
            case 5:
                AddQuad(a.position, c.position, c.xEdgePosition, a.xEdgePosition);
                break;
            case 10:
                AddQuad(a.xEdgePosition, c.xEdgePosition, d.position, b.position);
                break;
            case 12:
                AddQuad(a.yEdgePosition, c.position, d.position, b.yEdgePosition);
                break;
            case 15:
                AddQuad(a.position, c.position, d.position, b.position);
                break;
            case 7:
                AddPentagon(a.position, c.position, c.xEdgePosition, b.yEdgePosition, b.position);
                break;
            case 11:
                AddPentagon(b.position, a.position, a.yEdgePosition, c.xEdgePosition, d.position);
                break;
            case 13:
                AddPentagon(c.position, d.position, b.yEdgePosition, a.xEdgePosition, a.position);
                break;
            case 14:
                AddPentagon(d.position, b.position, a.xEdgePosition, a.yEdgePosition, c.position);
                break;
            case 6:
                AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                break;
            case 9:
                AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                break;
        }
    }
    void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

    }
    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    private void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        vertices.Add(e);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 4);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        for (int i = 0; i < voxels.Length; i++)
        {
            var item = voxels[i];
            Gizmos.color = item.state ? Color.green : Color.red;
            Gizmos.DrawCube(item.position, 0.05f * voxelSize * Vector3.one);
        }
    }

    //void CreateVoxel(int i, int x, int y)
    //{
    //    GameObject o = Instantiate(voxelPrefab, transform);
    //    o.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize);
    //    o.name = "voxel" + i;
    //    o.transform.localScale = voxelSize * voxelSizeScale * Vector3.one;
    //}

}
