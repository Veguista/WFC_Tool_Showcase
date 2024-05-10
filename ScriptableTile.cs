using System;
using UnityEngine;
using WFC.TileTypes;

namespace WFC
{
    [CreateAssetMenu(fileName = "NewTile", menuName = "WFC Tools/Create New Tile")]
    public class ScriptableTile : ScriptableObject, IComparable<ScriptableTile>
    {
        public TileType tileType = TileType.square2d;
        public GameObject tilePrefab;

        public bool askForRepaint = false;

        public int CompareTo(ScriptableTile other)
        {
            return String.Compare(name, other.name);
        }
    }
}