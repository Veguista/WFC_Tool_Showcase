using UnityEngine;

namespace WFC
{
    public interface IGridDrawable
    {
        public void SendTiles(ushort[][][] states, GameObject[] prefabs);

        public void DrawTiles();
    }
}