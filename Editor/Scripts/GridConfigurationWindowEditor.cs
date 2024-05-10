using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using WFC.TileTypes;
using WFC.ConnectionsBetweenTiles;
using WFC.Directions;
using System.Linq;

namespace WFC
{
    namespace EditorPack
    {
        public class GridConfigurationWindowEditor : EditorWindow
        {
            private const string uxmlFileName = "GridConfigurationWindowUXMLEditor";
            public VisualTreeAsset MyUXML
            {
                get
                {
                    if (_myStoredUXML != null)
                        return _myStoredUXML;

                    _myStoredUXML = WFC_EditorTools.ObtainVisualTreeAssetByName(uxmlFileName);

                    return _myStoredUXML;
                }
            }
            [Tooltip("Reference to the " + uxmlFileName + ".uxml asset that manages the custom inspector layout. " +
                "\nIn case of being left empty, the script will try to find the asset inside the project's files.")]
            [SerializeField] private VisualTreeAsset _myStoredUXML = null;

            private const string connectionCustomDrawerUXMLfileName = "ConnectionUXMLDrawer";
            public VisualTreeAsset MyConnectionCustomDrawerUXML
            {
                get
                {
                    if (_myConnectionCustomDrawerUXML != null)
                        return _myConnectionCustomDrawerUXML;

                    _myConnectionCustomDrawerUXML 
                        = WFC_EditorTools.ObtainVisualTreeAssetByName(connectionCustomDrawerUXMLfileName);

                    return _myConnectionCustomDrawerUXML;
                }
            }
            [Tooltip("Reference to the " + connectionCustomDrawerUXMLfileName + ".uxml asset that manages " +
                "the custom Property Drawer used for Connections. " +
                "\nIn case of being left empty, the script will try to find the asset inside the project's files.")]
            [SerializeField] private VisualTreeAsset _myConnectionCustomDrawerUXML = null;

            public Texture2D NoPreviewAvailibleTexture
            {
                get
                {
                    if (_noPreviewAvailibleTexture != null)
                        return _noPreviewAvailibleTexture;

                    _noPreviewAvailibleTexture = WFC_EditorTools.ObtainNoPreviewAvailibleTexture();

                    return _noPreviewAvailibleTexture;
                }
            }
            [Tooltip("Reference to T_WFC_NoPreviewAvailible.png, used in the TileType Custom Inspector. " +
                "\nIn case of being left empty, the script will try to find the asset inside the project's files.")]
            [SerializeField] private Texture2D _noPreviewAvailibleTexture = null;

            public static GridConfiguration lastGridToEdit = null;
            public static TileType typeOfGrid;
            public static VisualElement selectorSectionContainer;
            public static bool loadingWindow = false;
            private static List<VisualElement> instantiatedTilePropertyFields;

            // Our Selected Tile Sections for each Possible Direction.
            static VisualElement aboveInspectorContainer, upInspectorContainer, leftUpInspectorContainer, leftInspectorContainer,
                leftDownInspectorContainer, rightUpInspectorContainer, rightInspectorContainer, rightDownInspectorContainer,
                downInspectorContainer, belowInspectorContainer;
            static VisualElement[] sectionsToPopulate
            {
                get => new VisualElement[] {
                    aboveInspectorContainer, upInspectorContainer,
                    leftUpInspectorContainer, leftInspectorContainer, leftDownInspectorContainer,
                    rightUpInspectorContainer, rightInspectorContainer, rightDownInspectorContainer,
                    downInspectorContainer, belowInspectorContainer };
            }

            public static void OpenWindow(GridConfiguration grid)
            {
                typeOfGrid = grid.typeOfGrid;

                var window = GetWindow<GridConfigurationWindowEditor>();

                window.titleContent = new GUIContent("Grid Configuration");

                window.minSize = new Vector2(500, 500);
                window.maximized = true;
            }

            private void CreateGUI()
            {
                MyUXML.CloneTree(rootVisualElement);

                loadingWindow = true;

                // Refreshing the information in our GridConfiguration class.
                lastGridToEdit.RefreshDependantInfo();

                // Storing references to our UXML elements.
                selectedTileInspectorSection = rootVisualElement.Q<VisualElement>("selectedTileInspectorSection");
                selectedTileDisabledMessage = rootVisualElement.Q<VisualElement>("selectedTileDisabledMessage");
                tileSelectorNoFilesFoundMessage = rootVisualElement.Q<VisualElement>("tileSelectorNoFilesFoundMessage");
                selectedTilePreviewImage = rootVisualElement.Q<VisualElement>("previewImage");
                selectedTilePreviewName = rootVisualElement.Q<Label>("tileTypeName");

                VisualElement tileSelectorSection = rootVisualElement.Q<VisualElement>("tileSelectorSection");
                selectorSectionContainer = rootVisualElement.Q<VisualElement>("selectorContainer");
                VisualElement tileSelectorDisabledMessage = rootVisualElement.Q<VisualElement>("tileSelectorDisabledMessage");


                // Checking for availible Tiles to enable the Grid EditorPack.
                TileInstanceConfiguration[] availibleTiles = lastGridToEdit.ObtainAvailibleTileInstanceConfigurations();

                // Setting the visibility of our selected elements.
                WFC_EditorTools.HideVisualElement(selectedTileInspectorSection);
                WFC_EditorTools.HideVisualElement(selectedTileDisabledMessage);
                WFC_EditorTools.HideVisualElement(tileSelectorNoFilesFoundMessage);

                // If we don't have enough tiles availible to the Grid EditorPack.
                if (availibleTiles.Length <= 1)
                {
                    WFC_EditorTools.HideVisualElement(tileSelectorSection);
                    Label messageLabel = tileSelectorDisabledMessage.Q<Label>();

                    if (availibleTiles.Length == 1)
                    {
                        messageLabel.text = "Only 1 Tile of type <" + lastGridToEdit.typeOfGrid + "> was found " +
                            "[" + availibleTiles[0].scriptableTile.name + "] in the selected folders."
                            + "\n\nYou need at least 2 Tiles to use the Grid Editor.";
                    }
                    else
                    {
                        messageLabel.text = "No Tiles of type <" + lastGridToEdit.typeOfGrid + "> were found " +
                            "in the selected folders."
                            + "\n\nYou need at least 2 Tiles to use the Grid Editor.";
                    }

                    return;
                }

                // If we DO have enough tiles:
                WFC_EditorTools.HideVisualElement(tileSelectorDisabledMessage);

                // Setting up our Toolbar.
                ToolbarSearchField toolbarSearchField = rootVisualElement.Q<ToolbarSearchField>("searchToolbar");
                toolbarSearchField.RegisterValueChangedCallback(ChangedSearchToolbarValue);

                // Populating our Selector Section
                SerializedObject serializedGridObject = new SerializedObject(lastGridToEdit);
                SerializedProperty gridConfigurationArrayProperty
                    = serializedGridObject.FindProperty("availibleTileInstanceConfigs");

                for (int i = 0; i < availibleTiles.Length; i++)
                {
                    SerializedProperty tileTypeConfigProperty 
                        = gridConfigurationArrayProperty.GetArrayElementAtIndex(i);

                    PropertyField tileTypeConfigPropertyField = new PropertyField();
                    tileTypeConfigPropertyField.BindProperty(tileTypeConfigProperty);

                    selectorSectionContainer.Add(tileTypeConfigPropertyField);

                    myAvailibleTileTypeConfigFields.Add(tileTypeConfigProperty, tileTypeConfigPropertyField);
                }

                // Storing what are current tilePropertyFields are.
                instantiatedTilePropertyFields = (List<VisualElement>)selectorSectionContainer.Children();

                // Identifying our "selected tile inspector" sections & their respective containers.
                VisualElement aboveInspectorSection = selectedTileInspectorSection.Q<VisualElement>("above");
                aboveInspectorContainer = aboveInspectorSection.Q<VisualElement>("aboveContainer");

                VisualElement upInspectorSection = selectedTileInspectorSection.Q<VisualElement>("up");
                upInspectorContainer = upInspectorSection.Q<VisualElement>("upContainer");

                VisualElement leftUpInspectorSection = selectedTileInspectorSection.Q<VisualElement>("leftUp");
                leftUpInspectorContainer = leftUpInspectorSection.Q<VisualElement>("leftUpContainer");

                VisualElement leftInspectorSection = selectedTileInspectorSection.Q<VisualElement>("left");
                leftInspectorContainer = leftInspectorSection.Q<VisualElement>("leftContainer");

                VisualElement leftDownInspectorSection = selectedTileInspectorSection.Q<VisualElement>("leftDown");
                leftDownInspectorContainer = leftDownInspectorSection.Q<VisualElement>("leftDownContainer");

                VisualElement rightUpInspectorSection = selectedTileInspectorSection.Q<VisualElement>("rightUp");
                rightUpInspectorContainer = rightUpInspectorSection.Q<VisualElement>("rightUpContainer");

                VisualElement rightInspectorSection = selectedTileInspectorSection.Q<VisualElement>("right");
                rightInspectorContainer = rightInspectorSection.Q<VisualElement>("rightContainer");

                VisualElement rightDownInspectorSection = selectedTileInspectorSection.Q<VisualElement>("rightDown");
                rightDownInspectorContainer = rightDownInspectorSection.Q<VisualElement>("rightDownContainer");

                VisualElement downInspectorSection = selectedTileInspectorSection.Q<VisualElement>("down");
                downInspectorContainer = downInspectorSection.Q<VisualElement>("downContainer");

                VisualElement belowInspectorSection = selectedTileInspectorSection.Q<VisualElement>("below");
                belowInspectorContainer = belowInspectorSection.Q<VisualElement>("belowContainer");


                // Disabling and enabling sections in the inspector based on the TilePrefab
                // (Ex. Square2D disables the above section as it has no 3D depth).
                switch (typeOfGrid)
                {
                    case TileType.square2d:
                        WFC_EditorTools.HideVisualElement(aboveInspectorSection);
                        WFC_EditorTools.HideVisualElement(leftUpInspectorSection);
                        WFC_EditorTools.HideVisualElement(leftDownInspectorSection);
                        WFC_EditorTools.HideVisualElement(rightUpInspectorSection);
                        WFC_EditorTools.HideVisualElement(rightDownInspectorSection);
                        WFC_EditorTools.HideVisualElement(belowInspectorSection);

                        WFC_EditorTools.HideVisualElement(aboveInspectorContainer);
                        WFC_EditorTools.HideVisualElement(leftUpInspectorContainer);
                        WFC_EditorTools.HideVisualElement(leftDownInspectorContainer);
                        WFC_EditorTools.HideVisualElement(rightUpInspectorContainer);
                        WFC_EditorTools.HideVisualElement(rightDownInspectorContainer);
                        WFC_EditorTools.HideVisualElement(belowInspectorContainer);
                        break;
                    case TileType.square3d:
                        WFC_EditorTools.HideVisualElement(leftUpInspectorSection);
                        WFC_EditorTools.HideVisualElement(leftDownInspectorSection);
                        WFC_EditorTools.HideVisualElement(rightUpInspectorSection);
                        WFC_EditorTools.HideVisualElement(rightDownInspectorSection);

                        WFC_EditorTools.HideVisualElement(leftUpInspectorContainer);
                        WFC_EditorTools.HideVisualElement(leftDownInspectorContainer);
                        WFC_EditorTools.HideVisualElement(rightUpInspectorContainer);
                        WFC_EditorTools.HideVisualElement(rightDownInspectorContainer);
                        break;
                    case TileType.hexagon2d:
                        WFC_EditorTools.HideVisualElement(aboveInspectorSection);
                        WFC_EditorTools.HideVisualElement(leftInspectorSection);
                        WFC_EditorTools.HideVisualElement(rightInspectorSection);
                        WFC_EditorTools.HideVisualElement(belowInspectorSection);

                        WFC_EditorTools.HideVisualElement(aboveInspectorContainer);
                        WFC_EditorTools.HideVisualElement(leftInspectorContainer);
                        WFC_EditorTools.HideVisualElement(rightInspectorContainer);
                        WFC_EditorTools.HideVisualElement(belowInspectorContainer);
                        break;
                    case TileType.hexagon3d:
                        WFC_EditorTools.HideVisualElement(leftInspectorSection);
                        WFC_EditorTools.HideVisualElement(rightInspectorSection);

                        WFC_EditorTools.HideVisualElement(leftInspectorContainer);
                        WFC_EditorTools.HideVisualElement(rightInspectorContainer);
                        break;

                    default:
                        Debug.LogWarning("Type of Grid not implemented in GridConfigurationWindowEditor.cs");
                        break;
                }

                // Creating an array of our names and Preview Textures.
                Texture2D[] previewTextures = new Texture2D[availibleTiles.Length];
                string[] tileNames = new string[availibleTiles.Length];

                for(ushort i = 0; i < availibleTiles.Length; i++)
                {
                    previewTextures[i] = WFC_EditorTools.GetGameObjectPreview(availibleTiles[i].scriptableTile.tilePrefab);
                    tileNames[i] = availibleTiles[i].scriptableTile.name;
                }

                // For efficiency, we only populate those sections that are visible.
                foreach(VisualElement section in sectionsToPopulate)
                {
                    if (section.style.display == DisplayStyle.None)
                        continue;

                    PopulateWithConnectionPropertyFields(section, availibleTiles.Length);

                    for (int i = 0; i < availibleTiles.Length; i++)
                    {
                        SetConnectionsNameAndPreview(((List<VisualElement>)section.Children())[i], tileNames[i],
                            previewTextures[i]);
                    }
                }

                loadingWindow = false;
            }


            Dictionary<SerializedProperty, PropertyField> myAvailibleTileTypeConfigFields = new();
            private void ChangedSearchToolbarValue(ChangeEvent<string> evt)
            {
                if (loadingWindow)
                    return;

                string userSearchInput = evt.newValue;

                List<PropertyField> visibleTileTypeConfigFields = new();

                foreach (SerializedProperty property in myAvailibleTileTypeConfigFields.Keys)
                {
                    string tileTypeConfigName =
                        (property.FindPropertyRelative("tileType").boxedValue as ScriptableTile).name;

                    // This function acts as string.Contains() but is <case insensitive>. 
                    if (tileTypeConfigName.IndexOf(userSearchInput, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        PropertyField propertyField = myAvailibleTileTypeConfigFields[property];
                        propertyField.style.visibility = Visibility.Visible;
                        visibleTileTypeConfigFields.Add(propertyField);
                    }
                    else
                        myAvailibleTileTypeConfigFields[property].style.visibility = Visibility.Hidden;
                }

                if (visibleTileTypeConfigFields.Count == 0)
                {
                    ToggleNoFilesFoundSignal(true);
                }
                else
                {
                    ToggleNoFilesFoundSignal(false);
                    for (int i = visibleTileTypeConfigFields.Count - 1; i >= 0; i--)
                        visibleTileTypeConfigFields[i].SendToBack();
                }
            }


            private VisualElement tileSelectorNoFilesFoundMessage;
            void ToggleNoFilesFoundSignal(bool enabled)
            {
                if (tileSelectorNoFilesFoundMessage == null)
                {
                    Debug.LogError("Trying to Enable the NoFilesFoundMessage but it could not be found.");
                    return;
                }

                if (enabled)
                    WFC_EditorTools.RevealVisualElement(tileSelectorNoFilesFoundMessage);
                else
                    WFC_EditorTools.HideVisualElement(tileSelectorNoFilesFoundMessage);
            }

            // Static Visual Elements within the Grid Configuration Window.
            // They are static so that other scripts can alter them.
            public static VisualElement selectedTileInspectorSection, selectedTileDisabledMessage,
                selectedTilePreviewImage, selectedTilePreviewName;
            static public void EnableSelectedTileEditor(bool enabled)
            {                
                if (selectedTileInspectorSection == null || selectedTileDisabledMessage == null)
                {
                    Debug.LogError("Trying to Enable the SelectedTileEditor but the required Visual Elements can't be found.");
                    return;
                }

                if (enabled)
                {
                    WFC_EditorTools.RevealVisualElement(selectedTileInspectorSection);
                    WFC_EditorTools.HideVisualElement(selectedTileDisabledMessage);
                }
                else
                {
                    WFC_EditorTools.RevealVisualElement(selectedTileDisabledMessage);
                    WFC_EditorTools.HideVisualElement(selectedTileInspectorSection);
                }
            }

            static public void RefreshTileWeightValues()
            {
                if(selectorSectionContainer == null)
                {
                    Debug.LogError("Trying to Refresh the Tile Weight Values but the required Selector Section Container " +
                        "Visual Element can't be found.");
                    return;
                }

                TileInstanceConfiguration[] availibleTiles = lastGridToEdit.ObtainAvailibleTileInstanceConfigurations();

                // This escape clause is here to prevent escape the function when we call it while still Building the Window Editor element's.
                if (instantiatedTilePropertyFields.Count != availibleTiles.Length)
                    return;

                uint weightTotal = 0;

                // Calculating the maximum Weight Value.
                for (ushort i = 0; i < availibleTiles.Length; i++)
                {
                    if (availibleTiles[i].isEnabled)
                        weightTotal += availibleTiles[i].weight;
                }

                for (ushort i = 0; i < instantiatedTilePropertyFields.Count; i++)
                {
                    string weightString = "";

                    if (availibleTiles[i].isEnabled)
                    {
                        float weight = availibleTiles[i].weight * 100 / weightTotal;

                        if (weight == 100)
                            weightString = "100%";
                        else
                            weightString = weight.ToString(".0") + "%";
                    }
                    else
                        weightString = " - ";

                    Label myLabel = instantiatedTilePropertyFields[i].Q<Label>("percentage");

                    if (myLabel == null)
                        return;

                    myLabel.text = weightString;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns>The index of the currently selected tile. Returns -1 if no tile is currently selected.</returns>
            static public int WhatTileIsSelected()
            {
                // We first need to identify which of the Tiles Configs is Selected.
                for (int i = 0; i < instantiatedTilePropertyFields.Count(); i++)
                {
                    Button selectedButton = instantiatedTilePropertyFields[i].Q<Button>("button");

                    if (selectedButton == TileInstanceConfigurationDrawer.lastPressedSelectedTileTypeButton)
                        return i;
                }

                return -1; // If no tile is selected, we return -1.
            }

            #region Connection-Related Methods
            private void PopulateWithConnectionPropertyFields(VisualElement rootVisualElement, int numberOfFields)
            {
                for (int i = 0; i < numberOfFields; i++)
                {
                    rootVisualElement.Add(MyConnectionCustomDrawerUXML.Instantiate());
                }
            }

            /// <summary>
            /// Refreshes all information with regards to the selected tile (bindings, connectionsInsideSection, visibility of elements).
            /// </summary>
            public static void RefreshTileInspectorInformation()
            {
                TileInstanceConfiguration[] availibleTileConfigurations 
                    = lastGridToEdit.ObtainAvailibleTileInstanceConfigurations();

                // With one or less Tiles active, the inspector does not work.
                if (availibleTileConfigurations.Length <= 1)
                    return;
                
                int selectedTilePosition = WhatTileIsSelected();

                // If no tile is selected, we stop our operations (it likely means that we have just opened the Window).
                if (selectedTilePosition == -1)
                    return;

                // Orientating our Connections
                // (making all connectionsInsideSection go FROM our select tile TO the others).
                ScriptableTile selectedScriptableTile = availibleTileConfigurations[selectedTilePosition].scriptableTile;

                ConnectionsBtwnTwoScriptableTiles[] connectionsForSelectedTile 
                    = lastGridToEdit.ExistingConnectionsForScriptableTile(selectedScriptableTile);

                foreach (ConnectionsBtwnTwoScriptableTiles connections in connectionsForSelectedTile)
                    connections.SetOrientationOfConnections(selectedScriptableTile);

                // We can now go through every Tile Configuration & Property Field and:
                // - Hide or reveal the connectionsInsideSection in the inspector.
                // - Set up the Property Fields for each connection.
                // - Bind those Property fields.
                SerializedObject gridSerializedObject = new SerializedObject(lastGridToEdit);

                for (int i = 0; i < instantiatedTilePropertyFields.Count; i++)
                {
                    TileInstanceConfiguration tileConfig = availibleTileConfigurations[i];

                    VisualElement[] connectionsWithTile = new VisualElement[sectionsToPopulate.Length];
                    for(byte whichConnection = 0; whichConnection < connectionsWithTile.Length; whichConnection++)
                    {
                        if (sectionsToPopulate[whichConnection].style.display == DisplayStyle.None)
                            continue;
                        
                        connectionsWithTile[whichConnection]
                            = ((List<VisualElement>)sectionsToPopulate[whichConnection].Children())[i];
                    }

                    // Setting up the visibility of Connections.
                    if (tileConfig.isEnabled)
                    {
                        ToggleConnectionsVisibility(connectionsWithTile, true);
                    }
                    else // We can hide the connectionsInsideSection and move on. If the tile config is enabled again,
                    {    // this function will be called again and the connectionsInsideSection will be set up properly then.
                        ToggleConnectionsVisibility(connectionsWithTile, false);
                        continue;
                    }

                    // Binding values.
                    int indexToFindConnection = lastGridToEdit.ObtainIndexForConnectionBetweenTiles
                        (availibleTileConfigurations[selectedTilePosition].scriptableTile, tileConfig.scriptableTile);
                    SerializedProperty connectionsStructSerializedProperty = gridSerializedObject
                        .FindProperty("myConnectionsBetweenTiles").GetArrayElementAtIndex(indexToFindConnection);
                    BindConnections(i, connectionsForSelectedTile[i], connectionsStructSerializedProperty, 
                        selectedTilePosition == i); // This last thing is checking if we are dealing with connections of the Tile to itself.
                }
            }

            /// <summary>
            /// Hides or reveals the indicated child VisualElement in all sections.
            /// </summary>
            /// <param name="enabled"></param>
            /// <param name="propertyFieldIndex">The index that indicates the position of the child in an array.</param>
            private static void ToggleConnectionsVisibility(VisualElement[] targets, bool enabled)
            {
                for(int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null)
                        continue;

                    if (enabled)
                        WFC_EditorTools.RevealVisualElement(targets[i]);
                    else
                        WFC_EditorTools.HideVisualElement(targets[i]);
                }
            }

            /// <summary>
            /// Sets the name and preview textures of the indicated Connections.
            /// </summary>
            /// <param name="propertyFieldIndex">The index that indicates the position of the child in an array.</param>
            /// <param name="name">The name that will be given to all indicated connectionsInsideSection.</param>
            /// <param name="previewTexture">The preview Texture of the Scriptable Tile Prefab</param>
            private static void SetConnectionsNameAndPreview(VisualElement target, string name, Texture2D previewTexture)
            {
                target.Q<Label>("name").text = name;
                target.Q<VisualElement>("preview").style.backgroundImage = previewTexture;
            }

            /// <summary>
            /// Unbinds and rebinds the Toggles of all connectionsInsideSection for the indicated Property Fields.
            /// </summary>
            /// <param name="propertyFieldIndex">The index that indicates the position of the child in an array.</param>
            /// <param name="connections">A struct containing the connection for every direction.</param>
            /// <param name="serializedProperty">A reference to a serialized property of the 
            /// ConnectionsBetweenTwoScriptableTiles Struct passed on the previous parameter.</param>
            private static void BindConnections(int propertyFieldIndex, ConnectionsBtwnTwoScriptableTiles connections,
                SerializedProperty serializedProperty, bool selfConnection)
            {
                if (selfConnection)
                {
                    // Unregistering our old self connection Register Callback events.
                    foreach (Toggle registeredToggle in togglesWithRegisteredEventForSelfConnection)
                    {
                        registeredToggle.UnregisterValueChangedCallback(ToggleTileConnectionWithItself);
                    }
                    togglesWithRegisteredEventForSelfConnection.Clear();
                }

                bool[] connectionsInAllDirections = connections.myConnectionsInAlldirections;
                SerializedProperty connectionsSerializedProperty
                    = serializedProperty.FindPropertyRelative("myConnectionsInAlldirections");

                for (byte i = 0; i < connectionsInAllDirections.Length; i++)
                {
                    VisualElement targetContainer = ObtainContainer(i);

                    // Those directions that are not included in our UI will be skipped over.
                    if (targetContainer == null)
                        continue;

                    if (targetContainer.style.display == DisplayStyle.None)
                        continue;

                    // Once we have our container, we need to find our specific Visual Element.
                    Toggle targetToggle = 
                        ((List<VisualElement>)targetContainer.Children())[propertyFieldIndex].Q<ScalableButton>().myToggle;

                    targetToggle.Unbind();

                    SerializedProperty enabledPropertyForDirection = connectionsSerializedProperty.GetArrayElementAtIndex(i);

                    targetToggle.value = enabledPropertyForDirection.boolValue;
                    targetToggle.BindProperty(enabledPropertyForDirection);

                    // If we are self-connected, we register our event.
                    if (selfConnection)
                    {
                        togglesWithRegisteredEventForSelfConnection.Add(targetToggle);
                        targetToggle.RegisterValueChangedCallback(ToggleTileConnectionWithItself);
                    }
                }
            }

            private static VisualElement ObtainContainer(byte direction)
            {
                switch (direction)
                {
                    case (byte)Direction.above:
                        return aboveInspectorContainer;

                    case (byte)Direction.up:
                        return upInspectorContainer;

                    case (byte)Direction.hex_left_up:
                        return leftUpInspectorContainer;

                    case (byte)Direction.left:
                        return leftInspectorContainer;

                    case (byte)Direction.hex_left_down:
                        return leftDownInspectorContainer;

                    case (byte)Direction.hex_right_up:
                        return rightUpInspectorContainer;

                    case (byte)Direction.right:
                        return rightInspectorContainer;

                    case (byte)Direction.hex_right_down:
                        return rightDownInspectorContainer;

                    case (byte)Direction.down:
                        return downInspectorContainer;

                    case (byte)Direction.below:
                        return belowInspectorContainer;


                    default: // We have no container for other directions.
                        return null;
                }
            }

            // Event that happens when enabling a tile's connection with itself.
            // We need to constantly register and unregister it, so we keep track of which events are currently registered.
            static List<Toggle> togglesWithRegisteredEventForSelfConnection = new(); 

            private static void ToggleTileConnectionWithItself(ChangeEvent<bool> evt)
            {
                if (loadingWindow)
                    return;

                int selectedTile = WhatTileIsSelected();

                if (selectedTile == -1) // If no tile is selected, we exit.
                    return;

                Toggle activatedToggle = (Toggle) evt.currentTarget;

                // We now use a quick loop to find out the direction of the activated toggle.
                byte direction = 0;

                for(byte i = 0; i < togglesWithRegisteredEventForSelfConnection.Count; i++)
                {
                    if (togglesWithRegisteredEventForSelfConnection[i] == activatedToggle)
                    {
                        direction = i;
                        break;
                    }
                }

                // And we make the change manually.
                byte oppositeDirection = (byte) DirectionFunctions.ReturnOppositeDirection((Direction)direction);
                ScriptableTile scriptableTileSelected = lastGridToEdit.ObtainAvailibleTileInstanceConfigurations()[selectedTile].scriptableTile;
                lastGridToEdit.myConnectionsBetweenTiles[lastGridToEdit.ObtainIndexForConnectionBetweenTiles(
                    scriptableTileSelected, scriptableTileSelected)].myConnectionsInAlldirections[oppositeDirection] = evt.newValue;
            }

            #endregion


            private void OnDestroy()
            {
                // Just in case.
                loadingWindow = false;

                // Resetting static values OnDestroy.
                selectedTileInspectorSection    = null;
                selectedTileDisabledMessage     = null;
                selectedTilePreviewImage        = null;
                selectedTilePreviewName         = null;
                selectorSectionContainer        = null;
                instantiatedTilePropertyFields  = null;

                togglesWithRegisteredEventForSelfConnection = new();

                aboveInspectorContainer     = null;
                upInspectorContainer        = null;
                leftUpInspectorContainer    = null;
                leftInspectorContainer      = null;
                leftDownInspectorContainer  = null;
                rightUpInspectorContainer   = null;
                rightInspectorContainer     = null;
                rightDownInspectorContainer = null;
                downInspectorContainer      = null;
                belowInspectorContainer     = null;
            }
        }
    }
}