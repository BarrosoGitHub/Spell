using UnityEngine;
using System;
using System.Collections.Generic;

class Cell
{
    public Vector3Int c;
    public int chunkId;

    public readonly List<V> vertices = new();
    public readonly List<int> triangles = new();

    public CapFace[] caps;

    public bool surface;
    public bool solid;
    public bool removed;
}