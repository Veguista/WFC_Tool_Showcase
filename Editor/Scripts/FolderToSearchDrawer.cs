using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace WFC
{
    namespace EditorPack
    {
        [CustomPropertyDrawer(typeof(GridConfiguration.FolderToSearch))]
        public class FolderToSearchDrawer : PropertyDrawer
        {
            private const string styleSheetFileName = "WFC_StyleSheet";
            public StyleSheet MyWFCStyleSheet
            {
                get
                {
                    if (_myWFCStyleSheet != null)
                        return _myWFCStyleSheet;

                    _myWFCStyleSheet = WFC_EditorTools.ObtainStyleSheetAssetByName(styleSheetFileName);

                    return _myWFCStyleSheet;
                }
            }
            [Tooltip("Reference to the " + styleSheetFileName + ".uss asset that contains the . " +
                "\nIn case of being left empty, the script will try to find the asset in the inside the project's files.")]
            [SerializeField] private StyleSheet _myWFCStyleSheet = null;

            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                VisualElement rootVisualElement = new VisualElement();
                rootVisualElement.styleSheets.Add(MyWFCStyleSheet);

                VisualElement horizontalVisualElementBox = new VisualElement();
                horizontalVisualElementBox.style.flexDirection = FlexDirection.Row;
                horizontalVisualElementBox.style.justifyContent = Justify.SpaceBetween;
                horizontalVisualElementBox.style.alignContent = Align.Center;
                horizontalVisualElementBox.style.marginTop = 1;
                horizontalVisualElementBox.style.marginBottom = 1;
                horizontalVisualElementBox.style.flexWrap = Wrap.NoWrap;

                {
                    PropertyField pathPropField = new PropertyField(property.FindPropertyRelative("folderPath"));
                    pathPropField.label = "";
                    pathPropField.style.fontSize = 14;
                    pathPropField.style.unityFontStyleAndWeight = FontStyle.Normal;
                    pathPropField.style.alignSelf = Align.FlexStart;
                    pathPropField.style.marginRight = 10;
                    pathPropField.style.marginTop = 1.8f;
                    pathPropField.style.minWidth = 50;
                    pathPropField.style.flexGrow = 0.95f;
                    pathPropField.style.height = Length.Percent(100);
                    horizontalVisualElementBox.Add(pathPropField);

                    ToolbarToggle searchSubFoldersToggle = new ToolbarToggle();
                    SerializedProperty buttonProperty = property.FindPropertyRelative("searchSubFolders");
                    searchSubFoldersToggle.BindProperty(buttonProperty);
                    searchSubFoldersToggle.text = "Search Subfolders";
                    searchSubFoldersToggle.AddToClassList("dark-toolbartogl");
                    searchSubFoldersToggle.style.fontSize = 13;
                    searchSubFoldersToggle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    searchSubFoldersToggle.style.width = Length.Percent(45);
                    searchSubFoldersToggle.style.height = Length.Percent(100);
                    searchSubFoldersToggle.style.minWidth = 130;
                    searchSubFoldersToggle.style.maxWidth = 130;
                    horizontalVisualElementBox.Add(searchSubFoldersToggle);
                }

                rootVisualElement.Add(horizontalVisualElementBox);

                return rootVisualElement;
            }
        }
    }
}