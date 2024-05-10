using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace WFC
{
    namespace EditorPack
    {
        [CustomEditor(typeof(ScriptableTile))]
        public class ScriptableTileEditor : WFC_EditorTools
        {
            private const string uxmlFileName = "ScriptableTileUXMLEditor";
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

            public Texture2D NoPreviewAvailibleTexture
            {
                get
                {
                    if (_noPreviewAvailibleTexture != null)
                        return _noPreviewAvailibleTexture;

                    _noPreviewAvailibleTexture = ObtainNoPreviewAvailibleTexture();

                    return _noPreviewAvailibleTexture;
                }
            }
            [Tooltip("Reference to T_WFC_NoPreviewAvailible.png, used in the TileType Custom Inspector. " +
                "\nIn case of being left empty, the script will try to find the asset inside the project's files.")]
            [SerializeField] private Texture2D _noPreviewAvailibleTexture = null;

            ScriptableTile castedScript;


            private void OnEnable()
            {
                if (castedScript == null)
                    castedScript = target as ScriptableTile;
            }

            public override VisualElement CreateInspectorGUI()
            {
                VisualElement rootVisualElement = new VisualElement();
                rootVisualElement.Add(MyUXML.Instantiate());

                Label nameLabel = rootVisualElement.Q<Label>("nameLabel");
                nameLabel.text = castedScript.name;

                RadioButtonGroup tileTypeRadioButton = rootVisualElement.Q<RadioButtonGroup>("typeOfTileRadioButton");
                tileTypeRadioButton.SetValueWithoutNotify((int)castedScript.tileType);
                tileTypeRadioButton.RegisterValueChangedCallback(OnTileTypeChanged);
                tileTypeRadioButton.tooltip = "The type of Grid this Tile is designed to work on.";

                tileTypeImageVisualElement = rootVisualElement.Q<VisualElement>("tileTypeImage");
                Texture2D newTexture = ObtainTileTypeTextureForID((int)castedScript.tileType);
                ChangeTileTypeTextureOnCustomInspector(newTexture);

                PropertyField tilePrefabPropertyField = rootVisualElement.Q<PropertyField>("tilePrefab");
                tilePrefabPropertyField.tooltip = "The object that will be instantiated once the generation finishes.";
                tilePrefabPropertyField.label = "";
                tilePrefabPropertyField.RegisterValueChangeCallback(OnTilePrefabChanged);

                assetPreviewImageVisualElement = rootVisualElement.Q<VisualElement>("assetPreviewImage");
                SetTilePrefabPreviewInInspector();


                rootVisualElement.schedule.Execute(CheckIfPreviewTextureNeedsRepaint)
                    .Every(200);

                return rootVisualElement;
            }


            // Event Handler for changing TilePrefab. It manages the displayed Tile Type Texture on the custom inspector.
            private void OnTileTypeChanged(ChangeEvent<int> evt)
            {
                Texture2D newTexture = ObtainTileTypeTextureForID(evt.newValue);
                ChangeTileTypeTextureOnCustomInspector(newTexture);
            }


            VisualElement tileTypeImageVisualElement;
            private void ChangeTileTypeTextureOnCustomInspector(Texture2D newTexture)
            {
                tileTypeImageVisualElement.style.backgroundImage = newTexture;
            }


            VisualElement assetPreviewImageVisualElement;

            private Texture2D PreviewAssetTexture
            {
                get
                {
                    if (_previewAssetTexture != null)
                        return _previewAssetTexture;

                    if (castedScript == null
                        || castedScript.tilePrefab == null
                        || AssetPreview.IsLoadingAssetPreview(castedScript.tilePrefab.GetInstanceID()))
                    {
                        castedScript.askForRepaint = true;
                        return null;
                    }

                    _previewAssetTexture = AssetPreview.GetAssetPreview(castedScript.tilePrefab);

                    if (_previewAssetTexture == null)
                    {
                        castedScript.askForRepaint = true;
                        return null;
                    }

                    return _previewAssetTexture;
                }

                set { _previewAssetTexture = value; }
            }
            [SerializeField] private Texture2D _previewAssetTexture;

            GameObject StoredLastPrefab
            {
                get
                {
                    if (_storedLastPrefab == null)
                        _storedLastPrefab = castedScript.tilePrefab;

                    return _storedLastPrefab;
                }
                set
                {
                    _storedLastPrefab = value;
                }
            }
            [SerializeField] GameObject _storedLastPrefab;

            // Event Handler for changing TilePrefab.
            private void OnTilePrefabChanged(SerializedPropertyChangeEvent evt)
            {
                // Returning false calls.
                if (castedScript.tilePrefab == StoredLastPrefab)
                {
                    SetTilePrefabPreviewInInspector();
                    return;
                }

                StoredLastPrefab = castedScript.tilePrefab;

                PreviewAssetTexture = null;

                SetTilePrefabPreviewInInspector();
            }

            private void SetTilePrefabPreviewInInspector()
            {
                if (assetPreviewImageVisualElement == null)
                    return;

                if (PreviewAssetTexture != null)
                    assetPreviewImageVisualElement.style.backgroundImage = PreviewAssetTexture;
                else
                    assetPreviewImageVisualElement.style.backgroundImage = NoPreviewAvailibleTexture;
            }

            private void CheckIfPreviewTextureNeedsRepaint()
            {
                if (castedScript.askForRepaint == false)
                    return;

                if (PreviewAssetTexture == null)
                    return;

                castedScript.askForRepaint = false;

                SetTilePrefabPreviewInInspector();
            }


            // Creating Custom Icons for ScriptableTile classes.
            public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
            {
                if (PreviewAssetTexture == null)
                    return base.RenderStaticPreview(assetPath, subAssets, width, height);

                // We clone the instantiablePrefab's Asset Preview.
                Texture2D tex = new Texture2D(width, height);
                EditorUtility.CopySerialized(PreviewAssetTexture, tex);

                return tex;
            }
        }
    }
}

