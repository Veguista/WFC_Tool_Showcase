using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace WFC
{
    namespace EditorPack
    {
        [CustomPropertyDrawer(typeof(TileInstanceConfiguration))]
        public class TileInstanceConfigurationDrawer : PropertyDrawer
        {
            private const string uxmlFileName = "TileInstanceConfigurationUXMLDrawer";
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
            [Tooltip("Reference to the " + uxmlFileName + ".uxml asset that manages the custom drawer layout. " +
                "\nIf it is left empty, the script will try to find the asset inside the project's files.")]
            [SerializeField] private VisualTreeAsset _myStoredUXML = null;

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
                "\nIf it is left empty, the script will try to find the asset inside the project's files.")]
            [SerializeField] private Texture2D _noPreviewAvailibleTexture = null;

            Toggle enabledToggled;

            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                VisualElement rootVisualElement = new VisualElement();
                rootVisualElement.Add(MyUXML.Instantiate());

                enabledToggled = rootVisualElement.Q<Toggle>();
                SerializedProperty enabledTileTypeConfigProperty = property.FindPropertyRelative("isEnabled");
                enabledToggled.BindProperty(enabledTileTypeConfigProperty);
                enabledToggled.RegisterValueChangedCallback(ToggleEnabled);

                ScriptableTile castedScriptableTile =
                    property.FindPropertyRelative("scriptableTile").boxedValue as ScriptableTile;
                Texture2D previewImage = WFC_EditorTools.GetGameObjectPreview((castedScriptableTile).tilePrefab);
                if (previewImage == null)
                    previewImage = WFC_EditorTools.ObtainNoPreviewAvailibleTexture();
                rootVisualElement.Q<VisualElement>("image").style.backgroundImage = previewImage;

                Button SelectedTileButtonVisualElement = rootVisualElement.Q<Button>("button");
                SelectedTileButtonVisualElement.text = castedScriptableTile.name;
                SelectedTileButtonVisualElement.clicked += () => SelectedTileButtonClicked(SelectedTileButtonVisualElement);

                UnsignedIntegerField uIntWeightField = rootVisualElement.Q<UnsignedIntegerField>("weight");
                uIntWeightField.BindProperty(property.FindPropertyRelative("weight"));
                uIntWeightField.RegisterValueChangedCallback(TileWeightChanged);

                lastPressedSelectedTileTypeButton = null;

                return rootVisualElement;
            }

            // Changing the color of the image when enabling/disabling the TileTypeConfig.
            private Color disabledColorTint = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            private Color enabledBorderColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            private Color disabledBorderColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

            public void ToggleEnabled(ChangeEvent<bool> evt)
            {
                // While loading our Window, we ignore any RegisterCallback calls.
                if (GridConfigurationWindowEditor.loadingWindow)
                    return;

                VisualElement targetVisualElement = ((VisualElement)evt.currentTarget).parent.parent;
                bool toggleValue = (bool)evt.newValue;

                if (toggleValue == true)
                    targetVisualElement.style.unityBackgroundImageTintColor = Color.white;

                else // toggleValue == false
                    targetVisualElement.style.unityBackgroundImageTintColor = new StyleColor(disabledColorTint);

                PaintTileTypeBorder(targetVisualElement.parent.parent);

                // Seeing if this TilePrefab is the one active.
                Button tileTypeButton = targetVisualElement.parent.parent.Q<Button>("button");
                if (tileTypeButton == lastPressedSelectedTileTypeButton)
                {
                    PaintTileTypeButton(tileTypeButton);
                    PaintTileSelectedInspector(tileTypeButton);
                }

                // Propagating the change to the section where Connections are handled.
                GridConfigurationWindowEditor.RefreshTileInspectorInformation();
                GridConfigurationWindowEditor.RefreshTileWeightValues();
            }


            // Handling which TileInstanceConfiguration is selected.
            private Color normalButtonColor = new Color(0.345098f, 0.345098f, 0.345098f, 1);
            private Color activeButtonColor = new Color(0.15f, 0.62f, 0.75f, 1);
            private Color activeButDisabledButtonColor = new Color(0.6f, 0.3f, 0.3f, 1);

            internal static Button lastPressedSelectedTileTypeButton = null;

            public void SelectedTileButtonClicked(Button buttonClicked)
            {
                // While loading our Window, we ignore any RegisterCallback calls.
                if (GridConfigurationWindowEditor.loadingWindow)
                    return;

                if (lastPressedSelectedTileTypeButton == buttonClicked)
                    return;

                Button oldPressedButton = lastPressedSelectedTileTypeButton;

                lastPressedSelectedTileTypeButton = buttonClicked;
                PaintTileTypeButton(buttonClicked);
                PaintTileTypeBorder(buttonClicked.parent);
                PaintTileSelectedInspector(buttonClicked);

                Texture2D previewTexture =
                    buttonClicked.parent.Q<VisualElement>("image").style.backgroundImage.value.texture;
                RedrawSelectedTilePreview(previewTexture);
                RenameSelectedTilePreview(buttonClicked.text);

                if (oldPressedButton != null)
                {
                    PaintTileTypeButton(oldPressedButton);
                    PaintTileTypeBorder(oldPressedButton.parent);
                }

                // Propagating the change to the section where Connections are handled.
                GridConfigurationWindowEditor.RefreshTileInspectorInformation();
            }
            
            public void TileWeightChanged(ChangeEvent<uint> evt)
            {
                // While loading our Window, we ignore any RegisterCallback calls.
                if (GridConfigurationWindowEditor.loadingWindow)
                    return;

                // We clamp our value, a tile's weight can never be == 0.
                if (evt.newValue <= 0)
                {
                    ((UnsignedIntegerField)evt.target).value = 1;
                    ((UnsignedIntegerField)evt.target).binding.Update();
                    return;
                }  
                else if(evt.newValue > ushort.MaxValue)
                {
                    ((UnsignedIntegerField)evt.target).value = ushort.MaxValue;
                    ((UnsignedIntegerField)evt.target).binding.Update();
                    return;
                }


                GridConfigurationWindowEditor.RefreshTileWeightValues();
            }

            private void PaintTileTypeButton(Button button)
            {
                if (button == null)
                {
                    Debug.LogError("Trying to paint a null Button.");
                    return;
                }

                // If our button is not the last button to be pressed.
                if (button != lastPressedSelectedTileTypeButton)
                {
                    button.style.backgroundColor = normalButtonColor;
                    return;
                }

                // If our TileInstanceConfiguration is disabled.
                if (button.parent.Q<Toggle>().value == false)
                {
                    button.style.backgroundColor = activeButDisabledButtonColor;
                    return;
                }

                button.style.backgroundColor = activeButtonColor;
            }
            private void PaintTileTypeBorder(VisualElement visualElement)
            {
                if (visualElement == null)
                {
                    Debug.LogError("Trying to paint a null VisualElement.");
                    return;
                }

                Button tileTypeButton = visualElement.Q<Button>("button");
                Toggle tileTypeToggle = visualElement.Q<Toggle>();

                // If our scriptableTile is not the last button to be pressed.
                if (tileTypeButton != lastPressedSelectedTileTypeButton)
                {
                    if (tileTypeToggle.value == true)
                        SetBorderColor(visualElement, enabledBorderColor);
                    else
                        SetBorderColor(visualElement, disabledBorderColor);

                    return;
                }

                // If our TileInstanceConfiguration active (last button to be pressed) but disabled.
                if (tileTypeToggle.value == false)
                {
                    SetBorderColor(visualElement, activeButDisabledButtonColor);
                    return;
                }

                SetBorderColor(visualElement, activeButtonColor);


                // Private functions:
                void SetBorderColor(VisualElement target, Color newColor)
                {
                    target.style.borderRightColor = newColor;
                    target.style.borderBottomColor = newColor;
                    target.style.borderLeftColor = newColor;
                    target.style.borderTopColor = newColor;
                }
            }
            private void PaintTileSelectedInspector(Button button)
            {
                if (button == null)
                {
                    Debug.LogError("Trying to paint the inspector for a null Button.");
                    return;
                }

                // If our TileInstanceConfiguration is disabled.
                if (button.parent.Q<Toggle>().value == false)
                {
                    GridConfigurationWindowEditor.EnableSelectedTileEditor(false);
                    return;
                }

                GridConfigurationWindowEditor.EnableSelectedTileEditor(true);
            }
            private void RedrawSelectedTilePreview(Texture2D previewTexture)
            {
                if (previewTexture == null)
                {
                    Debug.LogError("Trying to redraw the selected tile's preview with a NULL preview texture.");
                    return;
                }

                GridConfigurationWindowEditor.selectedTilePreviewImage.style.backgroundImage = previewTexture;
            }
            private void RenameSelectedTilePreview(string newName)
            {
                if (newName == "")
                {
                    Debug.LogError("Trying to rename the selected tile's preview with an empty name.");
                    return;
                }

                ((Label)GridConfigurationWindowEditor.selectedTilePreviewName).text = newName;
            }
        }

    }
}
