using WFC.Directions;
using UnityEngine;

namespace WFC
{
    namespace TileTypes
    {
        public enum TileType
        {
            square2d, square3d, hexagon2d, hexagon3d
        }

        public static class TileTypeFunctions
        {
            /// <summary>
            /// Obtains the int value (from the Directions enum) of the valid sides from the selected TileType.
            /// </summary>
            /// <param name="type"></param>
            /// <returns>An Int[] array with all the valid values.</returns>
            public static byte[] ObtainValidDirections(TileType type)
            {
                byte[] result;  // Using byte for performance reasons, there shouldn't be more than 255 directions.

                switch (type)
                {
                    case TileType.square2d:
                        result = new byte[] { (byte) Direction.right, (byte) Direction.left, (byte) Direction.up, (byte) Direction.down };
                        break;
                    case TileType.square3d:
                        result = new byte[] { (byte) Direction.right, (byte) Direction.left, (byte) Direction.up, (byte) Direction.down, (byte) Direction.above, (byte) Direction.below };
                        break;
                    case TileType.hexagon2d:
                        result = new byte[] { (byte) Direction.up, (byte) Direction.down, (byte) Direction.hex_right_up, (byte) Direction.hex_left_up, (byte) Direction.hex_right_down, (byte) Direction.hex_left_down };
                        break;
                    case TileType.hexagon3d:
                        result = new byte[] { (byte) Direction.up, (byte) Direction.down, (byte) Direction.hex_right_up, (byte) Direction.hex_left_up, (byte) Direction.hex_right_down, (byte) Direction.hex_left_down, (byte) Direction.above, (byte) Direction.below };
                        break;

                    default:
                        Debug.LogError("TileType " + type + " not yet implemented. Returning an empty array.");
                        result = new byte[0];
                        break;
                }

                /////////////////////////////////////////////////////////////////////////////////////////////

                // INFO: This section is currently disabled cause the arrays are already ordered by default.
                // An unsorted array will result in unwanted behavior.
                // Uncomment this line if changes to the codebase make the "result" array not Sorted by default.

                // Array.Sort(result);

                /////////////////////////////////////////////////////////////////////////////////////////////

                return result;
            }

            /// <summary>
            /// Obtains an array of the Unit Vector of each direction for the selected TileType. 
            /// This unit Vectors are used to calculate the position of a tile in that direction.
            /// </summary>
            /// <param name="type"></param>
            /// <returns>A Vector3Int[] with all the unit Vectors. 
            /// These are sorted in the same order as the Directions coming out of the ObtainValidDirections() method.</returns>
            public static Vector3Int[] ObtainDirectionsUnitValue(TileType type)
            {
                switch (type)
                {
                    case TileType.square2d:
                        return new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
                    case TileType.square3d:
                        return new Vector3Int[] { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0) };
                    
                    // One big difference between Square and Hexagon grids is that in Hexagon Grids, Up directions returns a value of (0, 2, 0), not (0, 1, 0).
                    // The value of UP (and viceversa, down) is doubled in HEX grids cause we need to make space for intermidiate rows (all the left-up and derivatives).
                    case TileType.hexagon2d:
                        return new Vector3Int[] { new Vector3Int(0, 0, 2), new Vector3Int(0,  0, -2), new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1) };
                    case TileType.hexagon3d:
                        return new Vector3Int[] { new Vector3Int(0, 0, 2), new Vector3Int(0,  0, -2), new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0) };

                    default:
                        Debug.LogError("TileType " + type + " not yet implemented. Returning an empty array.");
                        return new Vector3Int[0];
                }
            }
        }
    }
}

