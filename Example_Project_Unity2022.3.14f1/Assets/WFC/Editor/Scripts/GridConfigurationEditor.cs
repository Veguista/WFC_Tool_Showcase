using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using WFC.TileTypes;

namespace WFC
{
    namespace EditorPack
    {
        [CustomEditor(typeof(GridConfiguration))]
        public class GridConfigurationEditor : WFC_EditorTools
        {
            private const string uxmlFileName = "GridConfigurationInspectorUXMLEditor";
            public VisualTreeAsset MyUXML
            {
                get
                {
                    if (_myStoredUXML != null)
                        return _myStoredUXML;

                    _myStoredUXML = ObtainVisualTreeAssetByName(uxmlFileName);

                    return _myStoredUXML;
                }
            }
            [Tooltip("Reference to the " + uxmlFileName + ".uxml asset that manages the custom inspector layout. " +
                "\nIn case of being left empty, the script will try to find the asset in the inside the project's files.")]
            [SerializeField] private VisualTreeAsset _myStoredUXML = null;

            public StyleSheet myWFCStyleSheet;

            GridConfiguration castedScript;

            private void OnEnable()
            {
                castedScript = target as GridConfiguration;
            }


            Button openEditorButton;
            Label folderPathRequiredLabel;

            // Creating the inspector.
            public override VisualElement CreateInspectorGUI()
            {
                VisualElement rootVisualElement = new VisualElement();
                rootVisualElement.Add(MyUXML.Instantiate());

                Label nameLabel = rootVisualElement.Q<Label>("nameLabel");
                nameLabel.text = castedScript.name;

                EnumField gridTypeEnumField = rootVisualElement.Q<EnumField>("typeOfGridSelect");
                gridTypeEnumField.RegisterValueChangedCallback(OnGridTypeChanged);

                PropertyField foldersToSearchPropertyField = rootVisualElement.Q<PropertyField>("foldersToSearch");
                foldersToSearchPropertyField.RegisterCallback<SerializedPropertyChangeEvent>(ChangeEventFoldersToSearch,
                    TrickleDown.TrickleDown);

                folderPathRequiredLabel = rootVisualElement.Q<Label>("folderPathRequiredLabel");
                gridTypeImageVisualElement = rootVisualElement.Q<VisualElement>("typeOfGridImage");

                openEditorButton = rootVisualElement.Q<Button>("openEditorButton");
                openEditorButton.RegisterCallback<ClickEvent>(ClickedOpenGridConfigurationWindow);

                return rootVisualElement;
            }


            private void ChangeEventFoldersToSearch(SerializedPropertyChangeEvent evt)
            {
                SerializedObject mySerializedObject = evt.changedProperty.serializedObject;
                mySerializedObject.Update();
                SerializedProperty foldersToSearchProp = mySerializedObject.FindProperty("availibleTileFolderPaths");

                if (foldersToSearchProp.arraySize == 0)
                {
                    SetIfUsersCanOpenGridMenu(false);
                    folderPathRequiredLabel.text = "Folder Path Required";
                    return;
                }

                List<string> validFolderPaths = new List<string>();

                for (int i = 0; i < foldersToSearchProp.arraySize; i++)
                {
                    string path
                        = foldersToSearchProp.GetArrayElementAtIndex(i).FindPropertyRelative("folderPath").stringValue;

                    if (AssetDatabase.IsValidFolder(path))
                        validFolderPaths.Add(path);
                }

                if (validFolderPaths.Count == 0)
                {
                    SetIfUsersCanOpenGridMenu(false);
                    folderPathRequiredLabel.text = "No Valid Folder Path Provided";
                    return;
                }

                SetIfUsersCanOpenGridMenu(true);
            }

            private void SetIfUsersCanOpenGridMenu(bool canOpenMenu)
            {
                if (folderPathRequiredLabel == null || openEditorButton == null)
                {
                    Debug.LogError("FolderPathLabel or OpenEditorButton are equal to null.");
                    return;
                }

                if (canOpenMenu)
                {
                    folderPathRequiredLabel.style.visibility = Visibility.Hidden;
                    openEditorButton.SetEnabled(true);
                    return;
                }

                folderPathRequiredLabel.style.visibility = Visibility.Visible;
                openEditorButton.SetEnabled(false);
            }

            private void ClickedOpenGridConfigurationWindow(ClickEvent evt)
            {
                GridConfigurationWindowEditor.lastGridToEdit = castedScript;
                GridConfigurationWindowEditor.OpenWindow(castedScript);
            }


            // Event Handler for changing TilePrefab.
            // It manages the displayed Tile Type Texture on the custom inspector.
            private void OnGridTypeChanged(ChangeEvent<Enum> evt)
            {
                int typeOfGridID = (int)((TileType)evt.newValue);
                Texture2D newTexture = ObtainTileTypeTextureForID(typeOfGridID);
                ChangeTileTypeTextureOnCustomInspector(newTexture);
            }

            VisualElement gridTypeImageVisualElement;
            private void ChangeTileTypeTextureOnCustomInspector(Texture2D newTexture)
            {
                gridTypeImageVisualElement.style.backgroundImage = newTexture;
            }
        }
    }
}

