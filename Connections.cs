using System;
using UnityEngine;
using WFC.Directions;

namespace WFC
{
    namespace ConnectionsBetweenTiles
    {
        [Serializable]
        public class ConnectionsBtwnTwoScriptableTiles
        {
            [SerializeField] public TwoScriptableTiles twoScriptableTiles;

            // In this array, the (byte) direction enum of each connection is also the position they occupy in the array.
            [SerializeField] public bool[] myConnectionsInAlldirections;


            public bool ContainsScriptableTile(ScriptableTile scriptableTile)
                => IsScriptableTileInFirstTile(scriptableTile) || IsScriptableTileInSecondTile(scriptableTile);
            public bool IsScriptableTileInFirstTile(ScriptableTile scriptableTile)
                => twoScriptableTiles.tileOne == scriptableTile;
            public bool IsScriptableTileInSecondTile(ScriptableTile scriptableTile)
                => twoScriptableTiles.tileTwo == scriptableTile;

            /// <summary>
            /// Used by our CustomPropertyDrawer to access the Target Scriptable Tile's information.
            /// </summary>
            /// <returns>The second Scriptable Tile stored in our Scriptable Tile Config array.</returns>
            public ScriptableTile ObtainSecondScriptableTile() => twoScriptableTiles.tileTwo;

            /// <summary>
            /// Ensures that all of the connections in this struct have the given tile as their First Tile.
            /// </summary>
            /// <param name="tileToBeFirst"></param>
            public void SetOrientationOfConnections(ScriptableTile tileToBeFirst)
            {
                if (IsScriptableTileInFirstTile(tileToBeFirst))
                    return;

                if (!IsScriptableTileInSecondTile(tileToBeFirst))
                {
                    Debug.LogError("Trying to orientate connections to a Tile that is not present in this struct.");
                    return;
                }

                SwapTilePerspective();
            }

            /// <summary>
            /// Changes the perspective from which the connections are seen.
            /// <para>Tile_1 becomes Tile_2 and viceversa. 
            /// All connections change their internal direction to their opposite.</para>
            /// </summary>
            private void SwapTilePerspective()
            {
                twoScriptableTiles.SwapTiles();

                // We create a replacement bool array and place the inverted connection info there.
                bool[] newConnectionsArray = new bool[myConnectionsInAlldirections.Length];
                for(int i = 0; i < myConnectionsInAlldirections.Length; i++)
                {
                    byte oppositeDirection = (byte) DirectionFunctions.ReturnOppositeDirection((Direction)i);
                    newConnectionsArray[oppositeDirection] = myConnectionsInAlldirections[i];
                }
                myConnectionsInAlldirections = newConnectionsArray;
            }


            public ConnectionsBtwnTwoScriptableTiles(ScriptableTile firstScriptableTile, ScriptableTile secondScriptableTile)
            {
                twoScriptableTiles = new TwoScriptableTiles(firstScriptableTile, secondScriptableTile);

                int numberOfDirections = Enum.GetNames(typeof(Direction)).Length;

                // The array is set to false by default. This means that all directions are disabled by default.
                myConnectionsInAlldirections = new bool[numberOfDirections];
            }
        }


        [Serializable]
        public struct TwoScriptableTiles
        {
            [SerializeField] public ScriptableTile tileOne, tileTwo;

            internal void SwapTiles()
            {
                ScriptableTile temporaryScriptableTile = tileOne;
                tileOne = tileTwo;
                tileTwo = temporaryScriptableTile;
            }

            public TwoScriptableTiles(ScriptableTile tileOne, ScriptableTile tileTwo)
            {
                this.tileOne = tileOne;
                this.tileTwo = tileTwo;
            }

            public TwoScriptableTiles(TwoScriptableTiles combination)
            {
                tileOne = combination.tileOne;
                tileTwo = combination.tileTwo;
            }
        }
    }
}
