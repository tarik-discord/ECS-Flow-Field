using System.Collections;
using System.Collections.Generic;
using Shooter.Pathing;
using Unity.Mathematics;
using UnityEngine;

public class NFlowTester : MonoBehaviour
{
    public int2 dimensions = new int2(100, 100);

    public int2 destination = new int2(50, 50);

    private Mesh wireResultMesh;

    public float tileTestSize = 1;

    public List<NBlocker> blockers = new List<NBlocker>();

    [System.Serializable]
    public struct NBlocker
    {
        public int2 min;
        public int2 max;
    }

    [ContextMenu("Generate")]
    private void DebugGenerate()
    {
        if (wireResultMesh == null)
        {
            wireResultMesh = new Mesh();
        }
        NFlowField field = new NFlowField(dimensions);
        for (int i = 0; i < blockers.Count; i++)
        {
            NBlocker block = blockers[i];
            int2 max = math.min(block.max, dimensions);
            for (int y = block.min.y; y < max.y; y++)
            {
                for (int x = block.min.x; x < max.x; x++)
                {
                    NFlowField.FlowTile tile = new NFlowField.FlowTile();
                    tile.state = 1;
                    field.field[x, y] = tile;
                }
            }
        }
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        field.Solve(destination);
        long ms = watch.ElapsedMilliseconds;
        long ticks = watch.ElapsedTicks;
        Debug.Log("Solve time ms:" + ms + " ticks:" + ticks);
        field.ConvertToMesh(wireResultMesh, tileTestSize);
        field.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 d = new Vector3(destination.x + 0.5f, 0, destination.y + 0.5f) * tileTestSize;
        Gizmos.DrawLine(d, d + Vector3.up);
        Bounds bounds = new Bounds();
        bounds.SetMinMax(Vector3.zero, new Vector3(dimensions.x, 0, dimensions.y) * tileTestSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.color = Color.red;

        for (int i = 0; i < blockers.Count; i++)
        {
            NBlocker block = blockers[i];
            bounds = new Bounds();
            bounds.SetMinMax(new Vector3(block.min.x - 0.5f, 0, block.min.y - 0.5f) * tileTestSize, new Vector3(block.max.x - 0.5f, 0, block.max.y - 0.5f) * tileTestSize);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireMesh(wireResultMesh);
    }
}
