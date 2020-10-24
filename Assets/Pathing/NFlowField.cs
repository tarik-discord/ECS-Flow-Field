using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct NFlowField
{


    struct FlowTile
    {
        public byte direction;
        public byte state;
    }

    private NGrid2D<FlowTile> field;

    public NFlowField(int2 dimensions)
    {
        this.field = new NGrid2D<FlowTile>(dimensions, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.UninitializedMemory);
    }


    public void Solve()
    {

    }

    private void Solve(JobHandle dependancy = default(JobHandle))
    {

    }

    public void Dispose()
    {
        field.Dispose();
    }


    [BurstCompile]
    private struct SimpleFieldSolve : IJob
    {

        struct SearchNode
        {
            public int2 root;
            public int cost;

            public SearchNode(int2 root, int cost)
            {
                this.root = root;
                this.cost = cost;
            }
        }

        private NGrid2D<FlowTile> field;
        private NGrid2D<byte> state;
        private NativeQueue<SearchNode> search;
        private int2 dest;

        private static readonly int2[] directions = new int2[] { new int2(-1, -1), new int2(-1, 0), new int2(-1, 1), new int2(0, 1), new int2(1, 1), new int2(1, 0), new int2(1, -1), new int2(0, -1) };

        public SimpleFieldSolve(NGrid2D<FlowTile> tiles, int2 dest)
        {
            this.dest = dest;
            this.field = tiles;
            this.state = new NGrid2D<byte>(tiles.Dimensions, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            this.search = new NativeQueue<SearchNode>(Allocator.TempJob);
        }

        public void Execute()
        {
            int dirLength = directions.Length;
            search.Enqueue(new SearchNode(dest, 0));
            while (search.Count > 0)
            {
                SearchNode node = search.Dequeue();
                state[node.root] = 1;
                for (int i = 0; i < dirLength; i++)
                {
                    int2 searchPos = node.root + directions[i];
                }
            }
        }
    }
}
