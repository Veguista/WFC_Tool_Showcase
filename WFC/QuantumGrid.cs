using WFC.ConnectionsBetweenTiles;
using Unity.VisualScripting;
using System.Linq;
using WFC.Directions;
using System.Collections.Generic;
using UnityEngine;
using WFC.TileTypes;


namespace WFC
{
    public class QuantumGrid
    {
        protected ScriptableTile[] scriptableTilesArray;

        protected Vector3Int gridDimensions;
        protected QuantumTile[][][] positionToTileArray; // Each array's value determines a coordinate of its Tile (X, Y, Z).
                                                         // The coordinate system used here is:
        protected bool[][][] validTilePositions;         // (X is Right/Left, Y is Above/Below, Z is Forward/Backwards)
                                                         // BEWARE: In 2D scenarios, Unity changes how its coordinate system is framed, making Z values no longer forward/backward but above/below.
                                                         //         This tool does not perform this change, and will always require input and give output following the previous format.
        protected ulong tilesLeftToCollapse;
        protected ushort[] tileTypeWeights;

        // sideDependencies[stateID][direction] = ushort[] with IDs that could be connected to the stateID in the Direction provided.
        protected ushort[][][] sideDependencies;

        // For simplicity, all directions are converted to a new set of consecutive increasing values starting at 0.
        // Ex. If the Valid Directions in our Grid are [1,3,4,6], these are then converted to [0,1,2,3] (as they can easily be used in arrays.)
        // The old values are stored in this array. To access one, just use realDirections["newValue"].
        protected byte[] realDirections;
        // Since we have abstracted real directions, this array is used to store the opposite of each abstracted directions.
        // To obtain the opposite of an abstracted direction, use oppositeDirec["abstract-direction"].
        protected byte[] oppositeDirections;
        // We also store the unit vector of each direction to be able to find new positions with them.
        // [newPosition] = [originalPosition] + [unitVectorOfDirection]
        protected Vector3Int[] directionsUnitVectors;

        // Reference to a script that implements the IGridDrawable interface that will manage how we draw our Tiles.
        protected IGridDrawable drawInterfaceScript;
        public TileType typeOfGrid { get; private set; }


        #region Public Control Methods

        /// <summary>
        /// Tries to collapse the current Grid through randomly picking tiles and then maintaining the Arc Consistency of the Grid. 
        /// If the collapse process is successful, it sends the Grid to be drawn.
        /// </summary>
        /// <returns>TRUE if the Grid could not be collapsed given the states given and/or its rollback limitations.</returns>
        public bool CollapseGrid()
        {
            // If we are using GridRollback, we need to store values before starting the GridCollapse process.
            if (isGridRollbackEnabled)
                AdvanceRollbackStack(); // We don't need to call AdvanceRollbackStack() here cause the backup stack already defaults to NULL.

            while (tilesLeftToCollapse != 0)
            {
                Vector3Int nextTilePosition = ObtainNextTileToCollapse();
                ushort stateToCollapseInto = SelectRandomStateFromGroup(
                    positionToTileArray[nextTilePosition.x][nextTilePosition.y][nextTilePosition.z].statesLeft);

                // Removing the State we just collapsed into from a hipothetical rollback.
                if (isGridRollbackEnabled)
                {
                    dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack] = nextTilePosition;

                    List<ushort> possibleStatesInTile = new List<ushort>(positionToTileArray[nextTilePosition.x][nextTilePosition.y][nextTilePosition.z].statesLeft);

                    possibleStatesInTile.Remove(stateToCollapseInto);

                    positionToTileArray[nextTilePosition.x][nextTilePosition.y][nextTilePosition.z].stateBackups[topOfTheRollbackStack] = possibleStatesInTile.ToArray();
                }

                CollapseTileIntoState(nextTilePosition, stateToCollapseInto);
                bool arcConsistent = LoopUntilArcConsistent();

                // If we are arc-consistent now, we get ready to reset the loop.
                if (arcConsistent)
                {
                    if (isGridRollbackEnabled)
                    {
                        AdvanceRollbackStack();
                        DefaultStateBackupToNull();
                    }

                    continue; // Next loop!
                }

                // Else, if we aren't Arc-Consistent, we get ready for a Grid Rollback.
                // We check if our current rollback might be invalid. If it is, we go up-stack to search for a valid one.
                if (AttemptToRollback())
                    continue;

                // Else, the Grid Collapse has failed.
                return true;
            }

            // Successful Grid Collapse.
            SendGridToDraw();
            return false;
        }


        /// <summary>
        /// Reduces the possible states of the given tiles to those provided. 
        /// Then, it checks that the new states have not changed the Grid's Arc-Consistency. 
        /// <para></para>This function should only be called before collapsing the Grid.
        /// </summary>
        /// <param name="tilePositions"></param>
        /// <param name="setStatesToThisScriptableTiles">The Scriptable Tiles that determine the new possible states for the altered tiles.</param>
        /// <returns>FALSE if everything is ok. TRUE if the changes make the Grid Arc-Inconsistent.</returns>
        public bool SetTiles(Vector3Int[] tilePositions, ScriptableTile[] setStatesToThisScriptableTiles)
        {
            if(tilePositions == null || setStatesToThisScriptableTiles == null)
            {
                Debug.LogError("Cannot SetTiles() if the input arrays are equal to NULL.");
                return true;
            }

            ushort[] newPossibleStates = new ushort[setStatesToThisScriptableTiles.Length];

            for(ushort i = 0; i < newPossibleStates.Length; i++)
            {
                for(ushort j = 0; j < scriptableTilesArray.Length; j++)
                {
                    // Iterating through our stored scriptable Tiles to find a match.
                    // This is used to find the ushort state that equates to the provided Scriptable Tile.
                    if (setStatesToThisScriptableTiles[i] == scriptableTilesArray[j])
                    {
                        newPossibleStates[i] = j;
                        break;
                    }
                }
            }

            if(newPossibleStates.Length != newPossibleStates.Distinct().ToArray().Length)
            {
                Debug.LogWarning("The list of states contained duplicated Scriptable Tiles. Tile duplicates will be ignored.");
                newPossibleStates = newPossibleStates.Distinct().ToArray();
            }

            // The array needs to be sorted for our calculation of common arrays to work.
            System.Array.Sort(newPossibleStates);

            foreach(Vector3Int position in tilePositions)
            {
                if (!validTilePositions[position.x][position.y][position.z])
                {
                    Debug.LogWarning("Position [" + position + "] is not valid in this grid. " +
                        "As such, it cannot be set to any state.\nInvalid positions will be ignored.");
                    continue;
                }

                ushort[] commonStates = CalculateCommonValuesBetweenSortedArrays(newPossibleStates,
                    positionToTileArray[position.x][position.y][position.z].statesLeft);

                if (commonStates.Length < newPossibleStates.Length)
                {
                    if (commonStates.Length == 0)
                    {
                        Debug.LogWarning("Tile [" + position + "] does not contain any common tiles " +
                            "with the states provided for it. Thus, it will be skipped." +
                            "\nIf you wish to override it's value, you can do it after the Grid is collapsed.");
                        continue;
                    }
                    // Else
                    Debug.LogWarning("Trying to reduce the states of Tile [" + position + "] but it does not " +
                        "contain all the states we are reducing it to. Only common states will be left at the tile." +
                        "\nIf you wish to override it's value, you can do it after the Grid is collapsed.");
                }

                positionToTileArray[position.x][position.y][position.z].statesLeft = commonStates;
                arcConsistencyQueue.AddRange(CalculateSurroundingTiles(position));

                if (commonStates.Length == 1)
                    tilesLeftToCollapse--;
            }

            return !LoopUntilArcConsistent();
        }

        /// <summary>
        /// Reduces the possible states of the given tiles to those provided. 
        /// Then, it checks that the new states have not changed the Grid's Arc-Consistency. 
        /// <para></para>This function should only be called before collapsing the Grid.
        /// </summary>
        /// <param name="tilePositions"></param>
        /// <param name="setStatesToThisScriptableTile">The Scriptable Tiles that determine the new possible states for the altered tiles.</param>
        /// <returns>FALSE if everything is ok. TRUE if the changes make the Grid Arc-Inconsistent.</returns>
        public bool SetTiles(Vector3Int[] tilePositions, ScriptableTile setStatesToThisScriptableTile)
        {
            if (tilePositions == null || setStatesToThisScriptableTile == null)
            {
                Debug.LogError("Cannot SetTiles() if the input arrays are equal to NULL.");
                return true;
            }

            return SetTiles(tilePositions, new ScriptableTile[] { setStatesToThisScriptableTile });
        }


        /// <summary>
        /// Loops through the indicated tiles removing all possible states that match the ones provided. 
        /// Then, it checks that the changes have not made the Grid Arc-Inconsistent. 
        /// <para></para>This function should only be called before collapsing the Grid.
        /// </summary>
        /// <param name="tilePositions"></param>
        /// <param name="tileStatesToRemove">The Scriptable Tiles that determine the states to be removed from tiles.</param>
        /// <returns>FALSE if everything is ok. TRUE if the changes make the Grid Arc-Inconsistent.</returns>
        public bool RemoveStatesFromTiles(Vector3Int[] tilePositions, ScriptableTile[] tileStatesToRemove)
        {
            if (tilePositions == null || tileStatesToRemove == null)
            {
                Debug.LogError("Cannot RemoveStatesFromTiles() if the input arrays are equal to NULL.");
                return true;
            }

            ushort[] removableStates = new ushort[tileStatesToRemove.Length];

            for (ushort i = 0; i < removableStates.Length; i++)
            {
                for (ushort j = 0; j < scriptableTilesArray.Length; j++)
                {
                    // Iterating through our stored scriptable Tiles to find a match.
                    // This is used to find the ushort state that equates to the provided Scriptable Tile.
                    if (tileStatesToRemove[i] == scriptableTilesArray[j])
                    {
                        removableStates[i] = j;
                        break;
                    }
                }
            }

            if (removableStates.Length != removableStates.Distinct().ToArray().Length)
            {
                Debug.LogWarning("The list of states contained duplicated Scriptable Tiles. Tile duplicates will be ignored.");
                removableStates = removableStates.Distinct().ToArray();
            }

            foreach (Vector3Int position in tilePositions)
            {
                if (!validTilePositions[position.x][position.y][position.z])
                {
                    Debug.LogWarning("Position [" + position + "] is not valid in this grid. " +
                        "As such, it cannot be set to any state.\nInvalid positions will be ignored.");
                    continue;
                }

                List<ushort> statesLeft = positionToTileArray[position.x][position.y][position.z].statesLeft.ToList();
                bool removedAState = false;

                foreach(ushort state in removableStates)
                {
                    if(statesLeft.Contains(state))
                    {
                        statesLeft.Remove(state);
                        removedAState = true;
                    }
                }

                if (removedAState)
                {
                    if (statesLeft.Count == 1)
                        tilesLeftToCollapse--;
                    else if (statesLeft.Count == 0)
                        Debug.LogWarning("Removing those states from tile [" + position + "] " +
                            "has left it without any possible states.");

                    positionToTileArray[position.x][position.y][position.z].statesLeft = statesLeft.ToArray();
                    arcConsistencyQueue.AddRange(CalculateSurroundingTiles(position));
                }
            }

            return !LoopUntilArcConsistent();
        }

        /// <summary>
        /// Loops through the indicated tiles removing all possible states that match the ones provided. 
        /// Then, it checks that the changes have not made the Grid Arc-Inconsistent. 
        /// <para></para>This function should only be called before collapsing the Grid.
        /// </summary>
        /// <param name="tilePositions"></param>
        /// <param name="tileStateToRemove">The Scriptable Tiles that determine the states to be removed from tiles.</param>
        /// <returns>FALSE if everything is ok. TRUE if the changes make the Grid Arc-Inconsistent.</returns>
        public bool RemoveStatesFromTiles(Vector3Int[] tilePositions, ScriptableTile tileStateToRemove)
        {
            if (tilePositions == null || tileStateToRemove == null)
            {
                Debug.LogError("Cannot RemoveStatesFromTiles() if the input arrays are equal to NULL.");
                return true;
            }

            return RemoveStatesFromTiles(tilePositions, new ScriptableTile[] { tileStateToRemove });
        }

        #endregion

        #region Virtual Methods (Methods that can be overriden)

        /// <summary>
        /// Transfers the grid's relevant information to the IGridDrawable script for the grid to be drawn. Called after a Grid is Collapsed.
        /// </summary>
        protected virtual void SendGridToDraw()
        {
            ushort[][][] tileStates = new ushort[gridDimensions.x][][];
            ushort nullTileState = (ushort)scriptableTilesArray.Length; // Our NULL otherTilesPosition state will be our last state +1.

            for (int x = 0; x < gridDimensions.x; x++)
            {
                tileStates[x] = new ushort[gridDimensions.y][];

                for (int y = 0; y < gridDimensions.y; y++)
                {
                    tileStates[x][y] = new ushort[gridDimensions.z];

                    for (int z = 0; z < gridDimensions.z; z++)
                    {
                        if (validTilePositions[x][y][z])
                            tileStates[x][y][z] = positionToTileArray[x][y][z].statesLeft[0];
                        else
                            tileStates[x][y][z] = nullTileState;
                    }
                }
            }

            GameObject[] prefabArray = new GameObject[scriptableTilesArray.Length + 1]; // +1 for our NULL state.

            for (ushort i = 0; i < scriptableTilesArray.Length; i++)
                prefabArray[i] = scriptableTilesArray[i].tilePrefab;

            // And adding our NULL state last.
            prefabArray[prefabArray.Length - 1] = null;

            drawInterfaceScript.SendTiles(tileStates, prefabArray);
        }

        /// <summary>
        /// Creates a Random Generation Seed using the current date and time.
        /// </summary>
        /// <returns></returns>
        protected virtual int CreateRandomGenerationSeed()
        {
            return int.Parse(System.DateTime.Now.Millisecond.ToString()
                + System.DateTime.Now.Minute.ToString()
                + System.DateTime.Now.Second.ToString());
        }

        #endregion

        #region Grid Rollback

        // We are storing a Drop Out Stack of how many tiles are left to be collapsed and within every tile. Those two types of Stacks share the same top of the stack, stored here.
        // (Drop-Out Stacks are limited-size Stacks where pushing a member when it's full results in the oldest member being removed).
        private ulong[] dropOutStackOfTilesLeftToCollapse;
        private Vector3Int[] dropOutStackOfLastTileToBeEdited;
        private byte numberOfActiveBackups = 0;
        private byte topOfTheRollbackStack = 0;
        private bool isGridRollbackEnabled = true;

        /// <summary>
        /// Attempts to find a valid Backup for our Tile information.
        /// </summary>
        /// <returns>TRUE if the attempt was successful. FALSE if the attempt was unsuccesful and our Grid Collapse failed.</returns>
        private bool AttemptToRollback()
        {
            while (numberOfActiveBackups != 0)
            {
                // We check if the current backup is ok to Rollback into.
                if (positionToTileArray[dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack].x]
                    [dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack].y]
                    [dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack].z]
                    .stateBackups[topOfTheRollbackStack].Length != 0)
                {
                    RollbackToLastBackup();

                    // If our rollback produces a Tile that is collapsed, we need to record it.
                    if (positionToTileArray[dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack].x]
                        [dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack].y]
                        [dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack].z]
                        .stateBackups[topOfTheRollbackStack].Length == 1)
                    {
                        tilesLeftToCollapse--;
                        dropOutStackOfTilesLeftToCollapse[topOfTheRollbackStack]--;
                    }

                    // Once we Rollback into a backup, we need to check the Arc-Consistency of the Backup
                    // (because the Backup is an altered version without the state that was originally picked).
                    arcConsistencyQueue.AddRange(CalculateSurroundingTiles(dropOutStackOfLastTileToBeEdited[topOfTheRollbackStack]));

                    // If we are succesful, we break from this loop and continue
                    if (LoopUntilArcConsistent())
                        return true;
                }

                // Otherwise we repeat our loop and go to an EVEN EARLIER Backup.
                ReceedToPreviousBackup();
            }

            // Rollback failed.
            return false;
        }

        // Moves the topOfTheRollbackStack forward and stores the number of Tiles left to collapse in its Drop-Out Stack.
        // This is normally (but not always) called together with DefaultStateBackupToNull().
        private void AdvanceRollbackStack()
        {
            numberOfActiveBackups++;
            // We have limited backups.
            numberOfActiveBackups = (byte)Mathf.Clamp(numberOfActiveBackups, 0, dropOutStackOfTilesLeftToCollapse.Length);

            // Progressing the Top of the stack.
            topOfTheRollbackStack = (byte)((topOfTheRollbackStack + 1) % dropOutStackOfTilesLeftToCollapse.Length);

            // Storing the value of our TilesLeftToCollapse.
            dropOutStackOfTilesLeftToCollapse[topOfTheRollbackStack] = tilesLeftToCollapse;
        }

        // Should ALWAYS be called in tandem with AdvanceRollbackStack(). Since we are only storing changes in our Rollback Stack when a tile is altered,
        // we need to default those tiles topStack value to NULL when moving the top of the stack.
        // (A NULL value in our Backup Stack signifies that NO CHANGES were made to that tile during this iteration of the arc-consistency checks).
        private void DefaultStateBackupToNull()
        {
            // Defaulting the value of the stateBackups to NULL.
            for (uint x = 0; x < gridDimensions.x; x++)
            {
                for (uint y = 0; y < gridDimensions.y; y++)
                {
                    for (uint z = 0; z < gridDimensions.z; z++)
                    {
                        if (validTilePositions[x][y][z])
                            positionToTileArray[x][y][z].stateBackups[topOfTheRollbackStack] = null;
                    }
                }
            }
        }

        private void RollbackToLastBackup()
        {
            // Restoring our grid values.
            tilesLeftToCollapse = dropOutStackOfTilesLeftToCollapse[topOfTheRollbackStack];

            for (uint x = 0; x < gridDimensions.x; x++)
            {
                for (uint y = 0; y < gridDimensions.y; y++)
                {
                    for (uint z = 0; z < gridDimensions.z; z++)
                    {
                        if (validTilePositions[x][y][z] && positionToTileArray[x][y][z].stateBackups[topOfTheRollbackStack] != null)
                            positionToTileArray[x][y][z].statesLeft = (ushort[])positionToTileArray[x][y][z].stateBackups[topOfTheRollbackStack].Clone();
                    }
                }
            }
        }

        private void ReceedToPreviousBackup()
        {
            numberOfActiveBackups--;    // Since it is a byte, the number of Active Backups cannot be set to less than 0.
            topOfTheRollbackStack = (byte)((dropOutStackOfTilesLeftToCollapse.Length + topOfTheRollbackStack - 1) % dropOutStackOfTilesLeftToCollapse.Length);
        }

        #endregion

        #region Arc-Consistency

        List<Vector3Int> arcConsistencyQueue = new List<Vector3Int>();

        /// <summary>
        /// Loops the Arc Consistency Queue, constantly clearing it until no new loops can be enacted 
        /// or until an inconsistency is found.
        /// </summary>
        /// <returns>A bool representing whether the Grid is Arc Consistent. 
        /// (Ex. Will return FALSE if the Grid cannot be collapsed due to an inconsistency).</returns>
        private bool LoopUntilArcConsistent()
        {
            while (arcConsistencyQueue.Count != 0)
            {
                if (ClearArcConsistencyQueue())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Using an algorhythm named AC-3, we ensure that all otherTilesPosition states are consistent with their adjacent counterparts.
        /// </summary>
        /// <param name="originalPosition"></param>
        /// <param name="newPossibleStates"></param>
        /// <returns>TRUE if a problem occurred that would trigger a GridRollback. FALSE if no problem was found.</returns>
        private bool CheckArcConsistencyOfTile(Vector3Int originalPosition)
        {
            ushort[] activeStates = positionToTileArray[originalPosition.x][originalPosition.y][originalPosition.z].statesLeft;

            Vector3Int[] surroundingTiles = CalculateSurroundingTiles(originalPosition);

            for (byte direction = 0; direction < realDirections.Length; direction++)
            {
                if (IsGridPositionValid(surroundingTiles[direction]) == false)
                    continue;

                List<ushort> newPossibleStates = new List<ushort>();

                foreach (ushort possibleState in positionToTileArray[surroundingTiles[direction].x][surroundingTiles[direction].y][surroundingTiles[direction].z].statesLeft)
                {
                    newPossibleStates.AddRange(sideDependencies[possibleState][oppositeDirections[direction]]);
                }

                List<ushort> unrepeatedList = newPossibleStates.Distinct().ToList();
                unrepeatedList.Sort();

                activeStates = CalculateCommonValuesBetweenSortedArrays(activeStates, unrepeatedList.ToArray());
            }

            if (positionToTileArray[originalPosition.x][originalPosition.y][originalPosition.z].statesLeft.Length == activeStates.Length)
                return false;

            if (activeStates.Length == 0)
                return true;

            if (activeStates.Length == 1)
                tilesLeftToCollapse--;

            // Storing a Backup if none is found.
            if (isGridRollbackEnabled && positionToTileArray[originalPosition.x][originalPosition.y][originalPosition.z]
                .stateBackups[topOfTheRollbackStack] == null)
            {
                positionToTileArray[originalPosition.x][originalPosition.y][originalPosition.z].stateBackups[topOfTheRollbackStack]
                    = (ushort[])positionToTileArray[originalPosition.x][originalPosition.y][originalPosition.z].statesLeft.Clone();
            }

            positionToTileArray[originalPosition.x][originalPosition.y][originalPosition.z].statesLeft = activeStates;

            arcConsistencyQueue.AddRange(surroundingTiles);

            // If no issues were found, we return FALSE. This means that no GridRollback is needed.
            return false;
        }

        private bool ClearArcConsistencyQueue()
        {
            Vector3Int[] queue = arcConsistencyQueue.Distinct().ToArray();
            arcConsistencyQueue.Clear();

            foreach (Vector3Int tilePosition in queue)
            {
                if (!IsGridPositionValid(tilePosition))
                    continue;

                if (CheckArcConsistencyOfTile(tilePosition))
                    return true;
            }

            return false;
        }

        #endregion

        #region Entropy Collapse

        // For more information on what "entropy" is in this context, check: https://en.wikipedia.org/wiki/Entropy_(information_theory)
        bool usingEntropyBasedCollapse;

        // Check https://en.wikipedia.org/wiki/Entropy_(information_theory to see how we calculate entropy.
        private float CalculateEntropyFromStatesLeft(ushort[] statesLeft)
        {
            uint maxWeight = 0;
            foreach (ushort state in statesLeft)
                maxWeight += tileTypeWeights[state];

            float sumatory = 0;
            for (ushort i = 0; i < statesLeft.Length; i++)
            {
                sumatory += (tileTypeWeights[i] / maxWeight) * Mathf.Log((float)tileTypeWeights[i] / maxWeight);
            }

            // The entropy is the negative of the sumatory.
            return -sumatory;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// If we are using Entropy-Based collapse, we calculate which tiles have the highest entropy and return one at random.
        /// Otherwise, we get a random tile fron those with the highest possible number of states left.
        /// </summary>
        private Vector3Int ObtainNextTileToCollapse()
        {
            List<Vector3Int> tilesThatAreCloserToCollapse = new List<Vector3Int>();

            if (usingEntropyBasedCollapse)
            {
                // The maximum entropy of a otherTilesPosition is equal to log(n), where n is the number of possible states.
                float minEntropyFound = Mathf.Log(scriptableTilesArray.Length);

                for (int x = 0; x < gridDimensions.x; x++)
                {
                    for (int y = 0; y < gridDimensions.y; y++)
                    {
                        for (int z = 0; z < gridDimensions.z; z++)
                        {
                            if (positionToTileArray[x][y][z].statesLeft.Length == 1)
                                continue;

                            float entropyOfTile = CalculateEntropyFromStatesLeft(positionToTileArray[x][y][z].statesLeft);

                            if (entropyOfTile <= minEntropyFound)
                            {
                                // If we found a new low in our entropy, we clear the previously found tiles.
                                if (entropyOfTile < minEntropyFound)
                                {
                                    minEntropyFound = entropyOfTile;
                                    tilesThatAreCloserToCollapse.Clear();
                                }

                                tilesThatAreCloserToCollapse.Add(new Vector3Int(x, y, z));
                            }
                        }
                    }
                }
            }
            else
            {
                ushort maxPossibleStatesFound = 2;

                for (int x = 0; x < gridDimensions.x; x++)
                {
                    for (int y = 0; y < gridDimensions.y; y++)
                    {
                        for (int z = 0; z < gridDimensions.z; z++)
                        {
                            if (positionToTileArray[x][y][z].statesLeft.Length >= maxPossibleStatesFound)
                            {
                                ushort remainingPossibleStates = (ushort)positionToTileArray[x][y][z].statesLeft.Length;

                                if (remainingPossibleStates > maxPossibleStatesFound)
                                {
                                    tilesThatAreCloserToCollapse.Clear();
                                    maxPossibleStatesFound = remainingPossibleStates;
                                }

                                tilesThatAreCloserToCollapse.Add(new Vector3Int(x, y, z));
                            }
                        }
                    }
                }
            }

            //Random.Range is min.inclusive and max.exclusive.
            return tilesThatAreCloserToCollapse[Random.Range(0, tilesThatAreCloserToCollapse.Count)];
        }

        /// <summary>
        /// States can have different weights. This function determines which state we randomly select.
        /// </summary>
        /// <param name="possibleStates"></param>
        /// <returns></returns>
        private ushort SelectRandomStateFromGroup(ushort[] possibleStates)
        {
            uint maxWeight = 0;
            foreach (ushort state in possibleStates)
            {
                maxWeight += tileTypeWeights[state];
            }

            if (maxWeight == uint.MaxValue)
                Debug.LogWarning("The Grid's weight values are too high. Random Values might not be truly random.\nTry reducing your weight values.");

            float randWeightValue = Random.Range(0, (float)1);

            for (ushort i = 0; i < possibleStates.Length; i++)
            {
                randWeightValue -= (float)tileTypeWeights[possibleStates[i]] / maxWeight;

                if (randWeightValue <= 0)
                    return possibleStates[i];
            }

            return (ushort)(possibleStates.Length - 1);
        }

        private void CollapseTileIntoState(Vector3Int position, ushort state)
        {
            // First, we check if our otherTilesPosition contains the state.
            if (!positionToTileArray[position.x][position.y][position.z].statesLeft.Contains(state))
            {
                Debug.LogWarning("Trying to collapse a tile into a state that it does not contain.");
                return;
            }

            positionToTileArray[position.x][position.y][position.z].statesLeft = new ushort[] { state };
            tilesLeftToCollapse--;

            arcConsistencyQueue.AddRange(CalculateSurroundingTiles(position));
        }

        private Vector3Int[] CalculateSurroundingTiles(Vector3Int originalPosition)
        {
            Vector3Int[] result = (Vector3Int[])directionsUnitVectors.Clone();

            for (byte i = 0; i < result.Length; i++)
                result[i] += originalPosition;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayOne"></param>
        /// <param name="arrayTwo"></param>
        /// <returns>A sorted ushort[] with all of the values found both in array 1 and 2.</returns>
        private ushort[] CalculateCommonValuesBetweenSortedArrays(ushort[] arrayOne, ushort[] arrayTwo)
        {
            List<ushort> result = new List<ushort>();

            ushort indexOne = 0, indexTwo = 0;
            ushort maxIndexOne = (ushort)arrayOne.Length, maxIndexTwo = (ushort)arrayTwo.Length;

            while (indexOne < maxIndexOne && indexTwo < maxIndexTwo)
            {
                if (arrayOne[indexOne] == arrayTwo[indexTwo])
                {
                    result.Add(arrayOne[indexOne]);
                    indexOne++;
                    indexTwo++;
                }
                else
                {
                    if (arrayOne[indexOne] < arrayTwo[indexTwo])
                        indexOne++;
                    else
                        indexTwo++;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Checks if a position is not within the Grid's Dimensions or wasn't enabled at the start.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsGridPositionValid(Vector3Int position)
        {
            // Checking if the position is outside the grid.
            if (position.x >= gridDimensions.x || position.y >= gridDimensions.y || position.z >= gridDimensions.z)
                return false;

            if (position.x < 0 || position.y < 0 || position.z < 0)
                return false;

            // Or if it was not initially identified as valid.
            if (validTilePositions[position.x][position.y][position.z])
                return true;
            else
                return false;
        }

        #endregion


        /// <summary>
        /// Creates a new uncollapsed Grid with the information from the GridConfiguration and the dimensions provided.
        /// </summary>
        /// <param name="gridConfiguration"></param>
        /// <param name="enabledTilePositions">Determines the grid's dimensions and what tiles are contained.
        /// <para>The value of the each array determines a coordinate of the Tile it contains (X, Y, Z).</para>
        /// For Hexagonal Tiles, even values in the Y slot represent rows of in-between tiles.
        /// <para>WARNING: The array of positions needs to have the dimensions of a cube.</para>
        /// At all times, array[X].Lenght == array.[Y].Lenght AND array[X][Y].Lenght == array.[Z][J].Lenght</param>
        /// <param name="drawScript">The Script that implements the IDrawable interface and which will receive the output
        /// of the grid once it is collapsed.</param>
        /// <param name="generationSeed">(Optional) A geneneration seed to make the Procedural Generation of the Grid deterministic. 
        /// The default value [0] makes the Grid generate its own seed based on the time and date of the system.</param>
        /// <param name="gridRollbackDepth">(Optional) The number of steps that are tracked by the Roolback system. 
        /// Storing 1 value should be more than enough in most cases. Bigger values might bloat the memory usage unnecessarily.</param>
        /// <param name="useEntropyBasedCollapse">(Optional) Whether to use an sumatory-based method when deciding which otherTilesPosition to collapse whenever 
        /// the grid runs out of consecuences to propagate. Deactivating it might improve performance.</param>
        /// <param name="skipFirstArcConsistencyCheck">(Optional) Normally, the Grid checks its Arc-Consistency right after it is created. 
        /// This options allows that check to be skipped, which can improve performance slightly in those cases where it is not needed.</param>
        public QuantumGrid(GridConfiguration gridConfiguration, bool[][][] enabledTilePositions, IGridDrawable drawScript,
            int generationSeed = 0, byte gridRollbackDepth = 1, bool useEntropyBasedCollapse = true,
            bool skipFirstArcConsistencyCheck = false)
        {
            TileInstanceConfiguration[] possibleTiles = gridConfiguration.ObtainEnabledAndValidTileTypes().ToArray();
            int numberOfPossibleTiles = possibleTiles.Length;

            // Error checking.
            if (numberOfPossibleTiles == 0)
            {
                Debug.LogError("Cannot create a QuantumGrid with 0 Active Tiles. Please add enabled tiles to the ["
                    + gridConfiguration.name + "] Grid.");
                return;
            }
            if (numberOfPossibleTiles > ushort.MaxValue - 1) // We save one spot always for our NULL value.
            {
                Debug.LogError("Cannot create a QuantumGrid with more than + " + ushort.MaxValue
                    + " different Tiles Types. This is done to improve performace." +
                    "\nTry subdividing your Grid into multiple sub-grids with less Tile Types.");
                return;
            }
            if (drawScript == null)
            {
                Debug.LogError("DrawScript reference is equal to null. Cannot create a grid without a script to " +
                    "draw its tiles. Please create a script that implements the IDrawableGrid interface.");
                return;
            }
            if (enabledTilePositions.Length == 0 || enabledTilePositions[0].Length == 0 || enabledTilePositions[0][0].Length == 0)
            {
                Debug.LogError("Cannot create a QuantumGrid with 0 Tile Positions.");
                return;
            }

            typeOfGrid = gridConfiguration.typeOfGrid;
            drawInterfaceScript = drawScript;
            validTilePositions = enabledTilePositions;
            tileTypeWeights = new ushort[numberOfPossibleTiles];
            usingEntropyBasedCollapse = useEntropyBasedCollapse;

            // Changing the random seed to avoid repetition.
            if (generationSeed == 0) // If users don't input a seed, we create a new one.
                generationSeed = CreateRandomGenerationSeed();

            Random.InitState(generationSeed);

            // Storing our real and opposite Direction's Info (check where these arrays are defined for more info).
            realDirections = TileTypeFunctions.ObtainValidDirections(typeOfGrid);
            directionsUnitVectors = TileTypeFunctions.ObtainDirectionsUnitValue(typeOfGrid);
            oppositeDirections = new byte[realDirections.Length];
            for (byte b = 0; b < realDirections.Length; b++)
            {
                byte realOppositeDirection = (byte)DirectionFunctions.ReturnOppositeDirection((Direction)realDirections[b]);

                for (byte i = 0; i < realDirections.Length; i++)
                {
                    if (realOppositeDirection == realDirections[i])
                        oppositeDirections[b] = i;
                }
            }

            // We rebuild the Connections in the our grid's dictionary, as it is not serialized.
            gridConfiguration.RebuildConnectionsDictionary();

            // Grid Rollback.
            if (gridRollbackDepth == 0)
                isGridRollbackEnabled = false;
            else
            {
                isGridRollbackEnabled = true;
                dropOutStackOfTilesLeftToCollapse = new ulong[gridRollbackDepth];
                dropOutStackOfLastTileToBeEdited = new Vector3Int[gridRollbackDepth];
            }

            // Identifying what we are meant to place inside each of our tiles.
            scriptableTilesArray = new ScriptableTile[numberOfPossibleTiles];
            for (ushort i = 0; i < numberOfPossibleTiles; i++)
                scriptableTilesArray[i] = possibleTiles[i].scriptableTile;

            ushort[] possibleStates = new ushort[numberOfPossibleTiles];
            sideDependencies = new ushort[numberOfPossibleTiles][][];

            for (ushort i = 0; i < numberOfPossibleTiles; i++)
            {
                possibleStates[i] = i;
                sideDependencies[i] = new ushort[realDirections.Length][];

                // Storing the weight of each TileType.
                tileTypeWeights[i] = possibleTiles[i].weight;

                // Because of how we are accessing our connections, temporarily we store our side dependencies in a list.
                List<ushort>[] temporarySideDependenciesForTile = new List<ushort>[realDirections.Length];
                for (int index = 0; index < temporarySideDependenciesForTile.Length; index++)
                    temporarySideDependenciesForTile[index] = new List<ushort>();

                for (ushort j = 0; j < numberOfPossibleTiles; j++)
                {
                    ConnectionsBtwnTwoScriptableTiles connections = gridConfiguration.myConnectionsBetweenTiles
                        [gridConfiguration.ObtainIndexForConnectionBetweenTiles(scriptableTilesArray[i], scriptableTilesArray[j])];

                    // We set the connection to right orientation (Otherwise the directions will be scrambled).
                    connections.SetOrientationOfConnections(scriptableTilesArray[i]);

                    for (byte abstractedDirection = 0; abstractedDirection < realDirections.Length; abstractedDirection++)
                    {
                        // If our connection in that abstractedDirection is enabled, we record it as a possible connection.
                        if (connections.myConnectionsInAlldirections[realDirections[abstractedDirection]])
                            temporarySideDependenciesForTile[abstractedDirection].Add(j);
                    }
                }

                // Converting our temporary sideDependencies list into our final array.
                for (byte direction = 0; direction < realDirections.Length; direction++)
                {
                    sideDependencies[i][direction] = temporarySideDependenciesForTile[direction].ToArray();
                }
            }


            // Populating our grid and arrays.
            gridDimensions.x = enabledTilePositions.Length;
            gridDimensions.y = enabledTilePositions[0].Length;
            gridDimensions.z = enabledTilePositions[0][0].Length;

            positionToTileArray = new QuantumTile[gridDimensions.x][][];
            tilesLeftToCollapse = 0;

            for (int x = 0; x < gridDimensions.x; x++)
            {
                positionToTileArray[x] = new QuantumTile[gridDimensions.y][];

                for (int y = 0; y < gridDimensions.y; y++)
                {
                    positionToTileArray[x][y] = new QuantumTile[gridDimensions.z];

                    for (int z = 0; z < gridDimensions.z; z++)
                    {
                        if (enabledTilePositions[x][y][z])  // If the otherTilesPosition Position is to exist.
                        {
                            positionToTileArray[x][y][z].statesLeft = (ushort[])possibleStates.Clone();
                            tilesLeftToCollapse++;

                            if (isGridRollbackEnabled)
                                positionToTileArray[x][y][z].stateBackups = new ushort[gridRollbackDepth][];
                        }
                    }
                }
            }

            if (tilesLeftToCollapse == 0)
            {
                Debug.LogError("Cannot have a Grid without enabled Tile Positions. Set at least one tile position to " +
                    "enabled in the QuantumGrid's constructor");
                return;
            }

            // Performing an initial Arc-Consistency Check to eliminate impossible states.
            if (!skipFirstArcConsistencyCheck)
            {
                for (int x = 0; x < gridDimensions.x; x++)
                {
                    for (int y = 0; y < gridDimensions.y; y++)
                    {
                        for (int z = 0; z < gridDimensions.z; z++)
                        {
                            if (enabledTilePositions[x][y][z])  // If the otherTilesPosition Position is to exist.
                            {
                                arcConsistencyQueue.Add(new Vector3Int(x, y, z));
                            }
                        }
                    }
                }

                while (arcConsistencyQueue.Count != 0)
                {
                    if (ClearArcConsistencyQueue())
                    {
                        Debug.LogError("First Arc-Consistency Check failed. " +
                            "The tiles provided cannot be Arc-Consistent in the current Grid.");
                        return;
                    }
                }
            }
        }
    }
}