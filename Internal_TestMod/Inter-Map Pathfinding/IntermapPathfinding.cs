using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.ShortestPath;
using NinMods.Logging;

namespace NinMods.InterMapPathfinding
{
    public static class IntermapPathfinding
    {
        // key is the map ID, value is a collection of map IDs that are reachable from the key
        // implicitly, the key is also reachable from each value, so it is bidirectional, i think? is that what bidirectional means in math?
        static AdjacencyGraph<int, Edge<int>> adjacencyMatrix = new AdjacencyGraph<int, Edge<int>>();
        // we'll be using this to calculate the shortest path to each vertex from each other vertex
        static FloydWarshallAllShortestPathAlgorithm<int, Edge<int>> allShortestPathAlgo = null;

        static double GetWeightForEdge(Edge<int> edge)
        {
            // just weigh all edges equally for now.
            // if the bot ends up having trouble with any specific map we could weigh those edges higher to avoid them.
            return 1.0d;
        }

        public static void Initialize()
        {
            #region adjacency graph initialization
            Edge<int>[] edges = new Edge<int>[]
            {
                new Edge<int>(1, 4), // leaf village to hospital
                new Edge<int>(4, 1),
                new Edge<int>(1, 18), // leaf village to leaf village entrance
                new Edge<int>(18, 1),
                new Edge<int>(18, 79), // leaf village entrance to southwest outskirts
                new Edge<int>(79, 18),
                new Edge<int>(18, 15), // leaf village entrance to leaf village southern outskirts
                new Edge<int>(15, 18),
                new Edge<int>(15, 26), // leaf village southern outskirts to larva road
                new Edge<int>(26, 15),
                new Edge<int>(26, 99), // larva road to moist plains
                new Edge<int>(99, 26),
                new Edge<int>(99, 90), // moist plains to striped lake
                new Edge<int>(90, 99),
                new Edge<int>(90, 187), // striped lake to valley of the end
                new Edge<int>(187, 90),
                new Edge<int>(187, 186), // valley of the end to west of the valley
                new Edge<int>(186, 187),
                new Edge<int>(186, 188), // west of the valley to end of the valley
                new Edge<int>(188, 186),
                new Edge<int>(188, 189), // end of the valley to tanzaku quarters entrance
                new Edge<int>(189, 188),
                new Edge<int>(189, 190), // tanzaku quarters entrance to tanzaku quarters
                new Edge<int>(190, 189),
                new Edge<int>(190, 192), // tanzaku quarters to hospital
                new Edge<int>(192, 190),
                new Edge<int>(192, 193), // tanzaku hospital to hospital 2F
                new Edge<int>(193, 192),
                new Edge<int>(15, 16), // leaf village southern outskirts to third training ground
                new Edge<int>(16, 15),
                new Edge<int>(16, 130), // third training ground to outskirts dead end
                new Edge<int>(130, 16),
                new Edge<int>(15, 17), // leaf village southern outskirts to water crossing
                new Edge<int>(17, 15),
                new Edge<int>(17, 88), // water crossing to forest of ambushes
                new Edge<int>(88, 17),
                new Edge<int>(88, 21), // forest of ambushes to into the valley
                new Edge<int>(21, 88),
                new Edge<int>(21, 19), // into the valley to near the valley
                new Edge<int>(19, 21),
                new Edge<int>(19, 22), // near the valley to forest near the ledge
                new Edge<int>(22, 19),
                new Edge<int>(22, 38), // forest near the ledge to ledge forest
                new Edge<int>(38, 22),
                new Edge<int>(38, 32), // ledge forest to ledge forest encampment
                new Edge<int>(32, 38),
                new Edge<int>(32, 66), // ledge forest encampment to dark clearing
                new Edge<int>(66, 32),
                new Edge<int>(66, 215), // dark clearing to mountain sea path
                new Edge<int>(215, 66),
                new Edge<int>(19, 66), // near the valley to dark clearing
                new Edge<int>(66, 19),
                new Edge<int>(19, 215), // near the valley to mountain sea path
                new Edge<int>(215, 19),
                // sand village starter area
                new Edge<int>(57, 56), // sand village hospital 2f to 1f
                new Edge<int>(56, 57),
                new Edge<int>(56, 51), // sand village hospital 1f to sand village
                // NOTE: sand village to hospital doesn't have the usual warp data. it's a no-name walkthrough event.
                new Edge<int>(51, 50), // sand village to sand village northern entrance
                new Edge<int>(50, 51),
                new Edge<int>(50, 85), // sand village northern entrance to sand nesting ground
                new Edge<int>(85, 50),
                new Edge<int>(85, 86), // sand nesting ground to sand hive grounds
                new Edge<int>(86, 85),
                new Edge<int>(86, 87), // sand hive grounds to desert cave entrance
                new Edge<int>(87, 86)
            };
            adjacencyMatrix.AddVerticesAndEdgeRange(edges);
            #endregion

            allShortestPathAlgo = new FloydWarshallAllShortestPathAlgorithm<int, Edge<int>>(adjacencyMatrix, GetWeightForEdge);
            allShortestPathAlgo.Compute();
            Logger.Log.Write("Initialized intermap pathfinding algorithm");
        }

        public static bool GetPathFromTo(int fromMapID, int toMapID, out IEnumerable<Edge<int>> path)
        {
            return allShortestPathAlgo.TryGetPath(fromMapID, toMapID, out path);
        }
    }
}