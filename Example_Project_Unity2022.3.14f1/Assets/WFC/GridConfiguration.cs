using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WFC.TileTypes;
using WFC.ConnectionsBetweenTiles;

namespace WFC
{
    [CreateAssetMenu(fileName = "NewGridConfiguration", menuName = "WFC Tools/Create New Grid Configuration")]
    public class GridConfiguration : ScriptableObject
    {
        ///////////////////////////
        // Serialized variables. //
        ///////////////////////////
        
        [SerializeField] private ScriptableTile[] availibleScriptableTiles;
        [SerializeField] private List<TileInstanceConfiguration> availibleTileInstanceConfigs = new();
        [SerializeField] public List<ConnectionsBtwnTwoScriptableTiles> myConnectionsBetweenTiles = new();

        [Serializable]  // Used to store the paths to our tiles.
        public class FolderToSearch
        {
            public string folderPath = "";
            public bool searchSubFolders = true;
        }
        public FolderToSearch[] availibleTileFolderPaths;

        [SerializeField] public TileType typeOfGrid = TileType.square2d;


        ///////////////////////////
        // NOT Serialized stuff. //
        ///////////////////////////

        // This Dictionary will link each TileConfigCombination with a determined index (to be used in the above list).
        // This is used to speed up accessing those connections from the Editor Side of things.
        // The Dictionary needs to be rebuilt often as it cannot be Serialized.
        Dictionary<TwoScriptableTiles, int> connectionsDictionary = new();

        // This dictionary is updated automatically every time we use it with ObtainTileInstanceConfigForScriptableTile(). 
        // I was forced to used this solution after a Unity Bug.
        private Dictionary<ScriptableTile, TileInstanceConfiguration> tileInstancesConfigDictionary = new();


        #region Public Methods

        /// <summary>
        /// Refreshes the information about what it is not stored in this class.
        /// <para>For example, which Scriptable Tile files are located inside our indicated folder paths.</para>
        /// </summary>
        public void RefreshDependantInfo()
        {
            ScriptableTile[] refreshedAvailibleScriptableTiles = CheckAvailibleScriptableTilesInFolderPaths();

            // If no changes were found, we exit.
            if (refreshedAvailibleScriptableTiles == availibleScriptableTiles)
                return;

            // If no tiles were found, we delete any info we could have prior to this point.
            if (refreshedAvailibleScriptableTiles.Length == 0)
            {
                ResetStoredTileInfo();
                return;
            }
            else
            {
                // Removing Scriptable Tiles that are no longer inside our target folders.
                foreach (ScriptableTile storedScriptableTiles in availibleScriptableTiles)
                {
                    if (!refreshedAvailibleScriptableTiles.Contains(storedScriptableTiles))
                        RemoveScriptableTileFromAvailible(storedScriptableTiles);
                }
            }

            List<ScriptableTile> scriptableTilesToInitialize 
                = new List<ScriptableTile>(refreshedAvailibleScriptableTiles);

            foreach (ScriptableTile newScriptableTile in refreshedAvailibleScriptableTiles)
            {
                if (availibleScriptableTiles.Contains(newScriptableTile))
                    scriptableTilesToInitialize.Remove(newScriptableTile);
            }

            availibleScriptableTiles = refreshedAvailibleScriptableTiles;

            // Populating our tileInstancesConfigDictionary with our already existing TileTypeConfigurations.
            tileInstancesConfigDictionary = new(); // (We start from scratch because Dictionaries don't have Serialization)


            foreach (var alreadyExistingConfig in availibleTileInstanceConfigs)
                tileInstancesConfigDictionary.Add(alreadyExistingConfig.scriptableTile, alreadyExistingConfig);

            foreach (ScriptableTile newTileType in scriptableTilesToInitialize)
                InitializeNewTileType(newTileType);

            RebuildConnectionsDictionary();

            // Sorting our arrays of ScriptableTiles and TileInstanceConfigs so they are in Alphabetical order when used.
            availibleTileInstanceConfigs.Sort();
            Array.Sort(availibleScriptableTiles);


            /////////////////////
            // Local Functions //
            /////////////////////

            void RemoveScriptableTileFromAvailible(ScriptableTile scriptableTileToRemove)
            {
                // Removing the tile from the availibleTileInstanceConfigs list.
                availibleTileInstanceConfigs.Remove(tileInstancesConfigDictionary[scriptableTileToRemove]);

                // Removing Connection to other Tiles.
                ConnectionsBtwnTwoScriptableTiles[] connectionsToRemove
                    = ExistingConnectionsForScriptableTile(scriptableTileToRemove);

                foreach (var connection in connectionsToRemove)
                    myConnectionsBetweenTiles.Remove(connection);
            }

            void InitializeNewTileType(ScriptableTile newTileType)
            {
                TileInstanceConfiguration newConfig = new TileInstanceConfiguration(newTileType);
                availibleTileInstanceConfigs.Add(newConfig);

                tileInstancesConfigDictionary.Add(newTileType, newConfig);

                foreach (TileInstanceConfiguration tileTypeConfig in availibleTileInstanceConfigs)
                {
                    myConnectionsBetweenTiles.Add(
                        new ConnectionsBtwnTwoScriptableTiles(tileTypeConfig.scriptableTile, newTileType));
                }
            }
        }

        /// <summary>
        /// Returns all Tile Instance configurations in the availibleScriptableTiles array.
        /// </summary>
        /// <returns></returns>
        public TileInstanceConfiguration[] ObtainAvailibleTileInstanceConfigurations()
        {
            if (availibleTileInstanceConfigs == null)
                return null;

            return availibleTileInstanceConfigs.ToArray();
        }

        /// <summary>
        /// Returns all Tile Instance Configurations that are availible, enabled and have at least 1 active connection.
        /// </summary>
        /// <returns></returns>
        public List<TileInstanceConfiguration> ObtainEnabledAndValidTileTypes()
        {
            List<TileInstanceConfiguration> enabledTileTypes  = new List<TileInstanceConfiguration>();

            foreach (TileInstanceConfiguration tileTypeConfig in availibleTileInstanceConfigs)
            {
                if (tileTypeConfig.isEnabled)
                    enabledTileTypes.Add(tileTypeConfig);
            }

            TileInstanceConfiguration[] enabledTilesBeforeCorrection = enabledTileTypes.ToArray();

            // We now trim any tile that does not have AT LEAST one connection enabled in a direction
            // (as we don't consider them to be really enabled).
            foreach(TileInstanceConfiguration tileInstanceConfig in enabledTilesBeforeCorrection)
            {
                if(!SearchForActiveConnection(tileInstanceConfig))
                    enabledTileTypes.Remove(tileInstanceConfig);
            }

            return enabledTileTypes;

            ///////////////////////////////////////////////////////////////////////////////
            // Local Methods
            ///////////////////////////////////////////////////////////////////////////////

            bool SearchForActiveConnection(TileInstanceConfiguration tileInstanceConfig)
            {
                ConnectionsBtwnTwoScriptableTiles[] connectionsAvailibleForTile
                    = ExistingConnectionsForScriptableTile(tileInstanceConfig.scriptableTile, true);

                for (int i = 0; i < connectionsAvailibleForTile.Length; i++)
                {
                    for (int j = 0; j < connectionsAvailibleForTile[i].myConnectionsInAlldirections.Length; j++)
                    {
                        if (connectionsAvailibleForTile[i].myConnectionsInAlldirections[j] == true)
                            return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns all Tile Instance Configurations in the availibleScriptableTiles array 
        /// that pass the provided lambda expression.
        /// </summary>
        /// <param name="Filter">Lambda expression that determines if a Tile Instance Configuration is returned.</param>
        /// <returns></returns>
        public TileInstanceConfiguration[] ObtainAvailibleTileTypesWhere
            (Func<TileInstanceConfiguration, bool> Filter)
        {
            List<TileInstanceConfiguration> filteredTileTypes = new();

            foreach (TileInstanceConfiguration tileInstanceConfig in availibleTileInstanceConfigs)
            {
                if (Filter(tileInstanceConfig))
                    filteredTileTypes.Add(tileInstanceConfig);
            }

            return filteredTileTypes.ToArray();
        }

        /// <summary>
        /// Returns the TileInstanceConfiguration associated with the given Scriptable Tile in THIS GRID CONFIGURATION
        /// (Tile Instance Configurations are not shared between Grid Configurations)
        /// </summary>
        /// <param name="scriptableTile"></param>
        /// <returns></returns>
        public TileInstanceConfiguration ObtainTileInstanceConfigForScriptableTile(ScriptableTile scriptableTile)
        {
            if(tileInstancesConfigDictionary.ContainsKey(scriptableTile))
                return tileInstancesConfigDictionary[scriptableTile];

            // If the Scriptable Tile is not found in our Dictionary, we place it ourselves.
            TileInstanceConfiguration resultTileInstanceConfig = null;

            for(int i = 0; i < availibleTileInstanceConfigs.Count; i++)
            {
                if (availibleTileInstanceConfigs[i].scriptableTile == scriptableTile)
                {
                    resultTileInstanceConfig = availibleTileInstanceConfigs[i];
                    break;
                }
            }

            if(resultTileInstanceConfig == null)
            {
                Debug.LogError("Could not find the correspondent TileInstanceConfig for ScriptableTile ["
                    + scriptableTile.name + "]. As a result, scriptableTileToTileConfigDictionary could not be completed.");
                return null;
            }

            tileInstancesConfigDictionary.Add(scriptableTile, resultTileInstanceConfig);
            return resultTileInstanceConfig;
        }

        public int ObtainIndexForConnectionBetweenTiles(ScriptableTile tileOne, ScriptableTile tileTwo)
        {
            TwoScriptableTiles connection = new TwoScriptableTiles(tileOne, tileTwo);
            if (connectionsDictionary.ContainsKey(connection))
                return connectionsDictionary[connection];

            // Because of how connections have an orientation, they can sometimes need to change orientation to be found.
            connection.SwapTiles();
            if (connectionsDictionary.ContainsKey(connection))
                return connectionsDictionary[connection];

            Debug.LogError("Index could not be found for tiles [" + tileOne.name + "] & [" + tileTwo.name + "].");
            return -1;
        }

        /// <summary>
        /// Returns an array of ConnectionsBetweenTwoScriptableTiles containing all connections with input Scriptable Tile.
        /// </summary>
        /// <param name="tileType"></param>
        public ConnectionsBtwnTwoScriptableTiles[] ExistingConnectionsForScriptableTile
            (ScriptableTile tileType, bool returnOnlyEnabledConnections = false)
        {
            List<ConnectionsBtwnTwoScriptableTiles> existingConnectionsList = new();

            foreach (var connection in myConnectionsBetweenTiles)
            {
                if (connection.ContainsScriptableTile(tileType))
                {
                    if (!returnOnlyEnabledConnections)
                        existingConnectionsList.Add(connection);
                    else if (CheckIfConnectionIsEnabled(connection))
                        existingConnectionsList.Add(connection);

                    // We set the orientation of the Connection to follow the given tile. This makes our .Sort() work.
                    connection.SetOrientationOfConnections(tileType);
                }
            }

            return existingConnectionsList.ToArray();
        }

        #endregion

        #region Private Methods

        private ScriptableTile[] CheckAvailibleScriptableTilesInFolderPaths()
        {
            List<ScriptableTile> result = new List<ScriptableTile>();

            for (int i = 0; i < availibleTileFolderPaths.Length; i++)
            {
                FolderToSearch folderInfo = availibleTileFolderPaths[i];

                List<ScriptableTile> tilesFound = (WFC_Tools.FindAllScriptableObjectsOfType<ScriptableTile>
                    (folderInfo.folderPath, folderInfo.searchSubFolders));

                if (tilesFound.Count == 0)
                    continue;

                result.AddRange(tilesFound);
            }

            // If no tiles were found, we don't need to filter the tiles.
            if (result.Count == 0)
                return new ScriptableTile[0];

            // Removing those tiles that are not of the specified type.
            var copyOfResults = result.ToArray();
            foreach (var tile in copyOfResults)
            {
                if (tile.tileType != typeOfGrid)
                    result.Remove(tile);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Deletes any Serialized Tile information stored in this class.
        /// <para>This does not affect the source of that information if it is originally stored in a different class.</para>
        /// </summary>
        private void ResetStoredTileInfo()
        {
            availibleScriptableTiles = new ScriptableTile[0];
            availibleTileInstanceConfigs = new();
            myConnectionsBetweenTiles = new();
            tileInstancesConfigDictionary = new();
            connectionsDictionary = new();
        }

        internal void RebuildConnectionsDictionary()
        {
            connectionsDictionary = new();

            for(int i = 0; i < myConnectionsBetweenTiles.Count; i++)
            {
                connectionsDictionary.Add(new TwoScriptableTiles(myConnectionsBetweenTiles[i].twoScriptableTiles), i);
            }
        }

        private bool CheckIfConnectionIsEnabled(ConnectionsBtwnTwoScriptableTiles connection)
        {
            if(!ObtainTileInstanceConfigForScriptableTile(connection.twoScriptableTiles.tileOne).isEnabled)
                return false;

            if(!ObtainTileInstanceConfigForScriptableTile(connection.twoScriptableTiles.tileTwo).isEnabled)
                return false;

            return true;
        }

        #endregion


        public GridConfiguration()
        {
            availibleTileFolderPaths = new FolderToSearch[1];
            availibleTileFolderPaths[0] = new FolderToSearch();
        }        
    }

    [Serializable]
    public class TileInstanceConfiguration : IComparable<TileInstanceConfiguration>
    {
        public ScriptableTile scriptableTile;
        public bool isEnabled = false;
        public ushort weight = 1;

        public TileInstanceConfiguration(ScriptableTile newScriptableTile, bool isEnabled = false)
        {
            scriptableTile = newScriptableTile;
            this.isEnabled = isEnabled;
        }

        public int CompareTo(TileInstanceConfiguration other)
        {
            return String.Compare(scriptableTile.name, other.scriptableTile.name);
        }
    }
}
