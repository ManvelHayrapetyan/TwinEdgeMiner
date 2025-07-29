using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class VoxelShellData : VoxelData
{
    private float _angleScore;
    private float _distanceScore;
    private float _surfaceDistanceScore;

    public VoxelShellData(int width, int height, int depth, float voxelSize, float maxStability, float maxDurability,
        float angleScore, float distanceScore, float surfaceDistanceScore) : 
        base(width, height, depth, voxelSize, maxStability, maxDurability)
    {
        _angleScore = angleScore;
        _distanceScore = distanceScore;
        _surfaceDistanceScore = surfaceDistanceScore;
    }

    public void BreakShellLayer(Vector3Int start, Vector3 direction, int targetCount)
    {
        HashSet<Vector3Int> visited = new();
        Utils.PriorityQueue<Vector3Int, float> queue = new();
        TryAdd(start);

        int destroyed = 0;
        Vector3Int newStart = start;
        while (queue.Count == 0)
        {
            newStart += DominantDirection(direction);
            if (!InBounds(newStart))
            {
                Debug.Log("Mesh Break Shell bad realization");
                break;
            }
            TryAdd(newStart);
        }

        while (queue.Count > 0 && destroyed < targetCount)
        {
            var current = queue.Dequeue();

            this[current.x, current.y, current.z] = 0f;
            destroyed++;

            foreach (var neighbor in Neighbors(current))
                TryAdd(neighbor);
        }

        Vector3Int DominantDirection(Vector3 direction)
        {
            Vector3 abs = new(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));

            if (abs.x >= abs.y && abs.x >= abs.z)
                return new Vector3Int(Mathf.RoundToInt(Mathf.Sign(direction.x)), 0, 0);
            else if (abs.y >= abs.x && abs.y >= abs.z)
                return new Vector3Int(0, Mathf.RoundToInt(Mathf.Sign(direction.y)), 0);
            else
                return new Vector3Int(0, 0, Mathf.RoundToInt(Mathf.Sign(direction.z)));
        }

        void TryAdd(Vector3Int pos)
        {
            if (visited.Contains(pos)) return;
            if (!InBounds(pos)) return;
            if (this[pos.x, pos.y, pos.z] <= IsoLevel) return;

            visited.Add(pos);
            float angle = Vector3.Dot(((Vector3)(pos - start)).normalized, direction.normalized); // [-1, 1]
            angle = (angle + 1f) * 0.5f; // [0, 1]
            float distance = (pos - start).magnitude / Mathf.Max(Width, Height, Depth);
            float surfaceDistance = SurfaceDistance(pos);
            queue.Enqueue(pos, angle * _angleScore + distance * _distanceScore + surfaceDistance * _surfaceDistanceScore);
        }
    }

    private IEnumerable<Vector3Int> Neighbors(Vector3Int pos)
    {
        yield return pos + Vector3Int.right;
        yield return pos + Vector3Int.left;
        yield return pos + Vector3Int.up;
        yield return pos + Vector3Int.down;
        yield return pos + new Vector3Int(0, 0, 1);
        yield return pos + new Vector3Int(0, 0, -1);
    }

    private int SurfaceDistance(Vector3Int pos)
    {
        int toLeft = pos.x;
        int toRight = Width - 1 - pos.x;
        int toBottom = pos.y;
        int toTop = Height - 1 - pos.y;
        int toBack = pos.z;
        int toFront = Depth - 1 - pos.z;

        return Mathf.Min(toLeft, toRight, toBottom, toTop, toBack, toFront) / Mathf.Max(Width, Height, Depth);
    }
    private bool InBounds(Vector3Int pos) =>
        pos.x >= 0 && pos.y >= 0 && pos.z >= 0 &&
        pos.x < Width && pos.y < Height && pos.z < Depth;
}