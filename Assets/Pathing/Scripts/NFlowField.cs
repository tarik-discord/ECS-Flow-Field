using System.Collections;
using System.Collections.Generic;
using Shooter.Utility;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Shooter.Pathing
{
    public struct NFlowField
    {
        public struct FlowTile
        {
            public byte direction;
            public byte state;

            public FlowTile(byte state)
            {
                this.state = state;
                this.direction = 0;
            }
        }

        public NGrid2D<FlowTile> field;

        public NFlowField(int2 dimensions)
        {
            this.field = new NGrid2D<FlowTile>(dimensions, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }


        public void Solve(int2 destTile)
        {
            SimpleFieldSolver solver = new SimpleFieldSolver(field, destTile);
            solver.Run();
            solver.Dispose();
        }

        public void ConvertToMesh(Mesh mesh, float scale)
        {
            Vector3[] positions = new Vector3[field.Width * field.Height];
            Vector3[] normals = new Vector3[field.Width * field.Height];
            for (int y = 0; y < field.Height; y++)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    positions[y * field.Width + x] = new Vector3(x, 0, y) * scale;
                }
            }

            List<int> indices = new List<int>();
            for (int y = 0; y < field.Height; y++)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    FlowTile tile = field[x, y];
                    if (tile.state == 0)
                    {
                        indices.Add(y * field.Width + x);
                        int2 pos = new int2(x, y) + directions[tile.direction];
                        if (math.all(pos >= 0 & pos < field.Dimensions))
                        {
                            indices.Add(pos.y * field.Width + pos.x);
                        }
                    }
                }
            }
            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
        }

        public void Dispose()
        {
            field.Dispose();
        }

        public static readonly int2[] directions = new int2[] { new int2(-1, -1), new int2(-1, 0), new int2(-1, 1), new int2(0, 1), new int2(1, 1), new int2(1, 0), new int2(1, -1), new int2(0, -1) };

        [BurstCompile(CompileSynchronously = true)]
        private struct SimpleFieldSolver : IJob
        {

            struct SearchNode
            {
                public int2 root;
                public uint cost;

                public SearchNode(int2 root, uint cost)
                {
                    this.root = root;
                    this.cost = cost;
                }
            }

            private NGrid2D<FlowTile> field;
            private NGrid2D<uint> state;
            private NativeQueue<SearchNode> search;
            private int2 dest;



            public SimpleFieldSolver(NGrid2D<FlowTile> tiles, int2 dest)
            {
                this.dest = dest;
                this.field = tiles;
                this.state = new NGrid2D<uint>(tiles.Dimensions, Allocator.TempJob, NativeArrayOptions.ClearMemory);
                this.search = new NativeQueue<SearchNode>(Allocator.TempJob);
            }

            public void Dispose()
            {
                if (search.IsCreated)
                {
                    search.Dispose();
                    state.Dispose();
                }
            }

            public void Execute()
            {
                int2 mapSize = field.Dimensions;
                int dirLength = directions.Length;
                state[dest] = 1;
                search.Enqueue(new SearchNode(dest, 1));
                //create the costs all valid tile will be >=1
                //this probably can't be parallel
                while (search.Count > 0)
                {
                    SearchNode node = search.Dequeue();
                    for (int i = 0; i < dirLength; i++)
                    {
                        int2 searchPos = node.root + directions[i];
                        if (math.all(searchPos > 0 & searchPos < mapSize))
                        {
                            uint currentCost = state[searchPos];
                            //if a cheaper parent is already written skip
                            if (currentCost <= node.cost)
                            {
                                //keep in map bounds

                                FlowTile tile = field[searchPos];
                                uint tileCost = node.cost + 1;
                                //a blocked tile don't search it
                                if (tile.state != 0)
                                {
                                    state[searchPos] = uint.MaxValue;
                                    continue;
                                }
                                //not searched or cheaper add to search
                                if (state[searchPos] == 0 || state[searchPos] > tileCost)
                                {
                                    state[searchPos] = tileCost;
                                    search.Enqueue(new SearchNode(searchPos, tileCost));
                                }
                            }
                        }
                    }
                }
                //make the field
                //this could definitely be done in parallel
                int2 mPos = new int2();
                for (mPos.y = 0; mPos.y < mapSize.y; mPos.y++)
                {
                    for (mPos.x = 0; mPos.x < mapSize.x; mPos.x++)
                    {
                        byte mask = 0;
                        uint highValue = uint.MaxValue;
                        //uint currentCost = state[mPos];
                        for (int i = 0; i < dirLength; i++)
                        {
                            int2 searchPos = mPos + directions[i];
                            if (math.all(searchPos > 0 & searchPos < mapSize))
                            {
                                uint searchVal = state[searchPos];
                                if (searchVal < highValue)
                                {
                                    mask = (byte)i;
                                    highValue = searchVal;
                                }
                            }
                        }
                        FlowTile tile = field[mPos];
                        tile.direction = mask;
                        field[mPos] = tile;
                    }
                }
            }
        }
    }
}