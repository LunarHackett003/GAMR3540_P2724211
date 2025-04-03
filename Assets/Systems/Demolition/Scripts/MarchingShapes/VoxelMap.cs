using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelMap : LunarScript
{
    public VoxelGrid voxelGridPrefab;
    public float size = 2f;
    public int voxelResolution = 8, chunkResolution = 2;

    private VoxelGrid[] chunks;
    private float chunkSize, voxelSize, halfSize;

    BoxCollider boxCollider;

    public VoxelStencil stencil;

    private void Awake()
    {
        halfSize = size * 0.5f;
        chunkSize = size / chunkResolution;
        voxelSize = chunkSize / voxelResolution;
        //whoops, this was broken for a while :p



        chunks = new VoxelGrid[chunkResolution * chunkResolution];
        for (int i = 0, y = 0; y < chunkResolution; y++)
        {
            for (int x = 0; x < chunkResolution; x++, i++)
            {
                CreateChunk(i, x, y);
            }
        }

        boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.size = new(size, size);
    }
    void CreateChunk(int i, int x, int y)
    {
        VoxelGrid chunk = Instantiate(voxelGridPrefab);
        chunk.Initialise(voxelResolution, chunkSize);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
        chunks[i] = chunk;
    }

    public override void LUpdate()
    {
        base.LUpdate();

        if (Mouse.current.leftButton.ReadValue() > 0.5f)
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    EditVoxels(transform.InverseTransformPoint(hit.point));
                }
                Debug.DrawRay(hit.point - Vector3.up * 0.5f + hit.normal * 0.1f, Vector3.up, Color.red, 0.2f);
                Debug.DrawRay(hit.point - Vector3.right * 0.5f + hit.normal * 0.1f, Vector3.right, Color.red, 0.2f);
            }
        }
    }

    private void EditVoxels(Vector3 point)
    {
        int centerX = (int)((point.x + halfSize) / voxelSize);
        int centerY = (int)((point.y + halfSize) / voxelSize);

        int xStart = (centerX - stencil.radius) / voxelResolution;
        if (xStart < 0)
        {
            xStart = 0;
        }
        int xEnd = (centerX + stencil.radius) / voxelResolution;
        if (xEnd >= chunkResolution)
        {
            xEnd = chunkResolution - 1;
        }
        int yStart = (centerY - stencil.radius) / voxelResolution;
        if (yStart < 0)
        {
            yStart = 0;
        }
        int yEnd = (centerY + stencil.radius) / voxelResolution;
        if (yEnd >= chunkResolution)
        {
            yEnd = chunkResolution - 1;
        }

        int voxelYOffset = yStart * voxelResolution;
        for (int y = yStart; y <= yEnd; y++)
        {
            int i = y * chunkResolution + xStart;
            int voxelXOffset = xStart * voxelResolution;
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                stencil.SetCentre(centerX - voxelXOffset, centerY - voxelYOffset);
                chunks[i].Apply(stencil);
                voxelXOffset += voxelResolution;
            }
            voxelYOffset += voxelResolution;
        }
    }

    private void OnValidate()
    {
        stencil.Initialise();
    }
}
