using UnityEngine;
using WFC;
using System.Collections.Generic;

public class GridCreatorTest : MonoBehaviour
{
    [SerializeField] GridConfiguration gridConfiguration;
    [SerializeField] Vector3Int gridDimensions = new Vector3Int(3, 3, 3);
    [SerializeField] GridInterface_Test gridDrawingScript;
    [SerializeField][Range(0, 10000)] int seed;
    [SerializeField] ScriptableTile borderTile;

    private void Awake()
    {
        bool[][][] enabledTiles = new bool[gridDimensions.x][][];
        List<Vector3Int> borderPositions = new List<Vector3Int>();

        for (int x = 0; x < gridDimensions.x; x++)
        {
            enabledTiles[x] = new bool[gridDimensions.y][];

            for (int y = 0; y < gridDimensions.y; y++)
            {
                enabledTiles[x][y] = new bool[gridDimensions.z];

                for (int z = 0; z < gridDimensions.z; z++)
                {
                    enabledTiles[x][y][z] = true;

                    if(x == 0 || x == gridDimensions.x - 1 || z == 0 || z == gridDimensions.z - 1)
                        borderPositions.Add(new Vector3Int(x, y, z));
                }
            }
        }

        QuantumGrid quantumGrid = new QuantumGrid(gridConfiguration, enabledTiles, gridDrawingScript, seed, 1, true);

        if (quantumGrid.SetTiles(borderPositions.ToArray(), new ScriptableTile[] { borderTile }))
            Debug.LogWarning("Failed to set tiles.");

        /*
         * You can also use quantumGrid.RemoveStatesFromTiles() to remove certain states from your grid.
         */

        if (quantumGrid.CollapseGrid())
            Debug.LogWarning("Grid failed.");
        else
            gridDrawingScript.DrawTiles();
    }
}
