using UnityEngine;
using WFC;

public class GridInterface_Test : MonoBehaviour, IGridDrawable
{
    ushort[][][] states;
    GameObject[] tilePrefabs;


    public void DrawTiles()
    {
        for (int x = 0; x < states.Length; x++)
        {
            for (int y = 0; y < states[0].Length; y++)
            {
                for (int z = 0; z < states[0][0].Length; z++)
                {
                    ushort tilePrefabID = states[x][y][z];

                    if (tilePrefabs[tilePrefabID] == null)
                        continue;

                    GameObject tile = Instantiate(tilePrefabs[tilePrefabID], transform);
                    tile.transform.localPosition = new Vector3(x*1.6f, z*1.6f, 0);
                    tile.name = "Tile(" + x + "," + y + "," + z + ")";
                }
            }
        }
    }

    public void SendTiles(ushort[][][] states, GameObject[] prefabs)
    {
        this.states = states;
        this.tilePrefabs = prefabs;
    }
}
