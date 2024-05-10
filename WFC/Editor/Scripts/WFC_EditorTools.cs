using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WFC
{
    namespace EditorPack
    {
        public class WFC_EditorTools : Editor
        {
            #region Tile Type Textures in the Custom Inspector

            [Space(10)]
            [Tooltip("Reference to the path that stores the Tile Type Textures for the WFC Tile Type custom inspector." +
                "\nThis field should be left empty as default.")]
            [SerializeField]
            Texture2D _square2dTileTypeTexture, _square3dTileTypeTexture,
                        _hexagon2dTileTypeTexture, _hexagon3dTileTypeTexture;

            private const string square2dTileTypeTextureName = "T_WFC_Square2D";    // TilePrefab ID: 0
            private const string square3dTileTypeTextureName = "T_WFC_Square3D";    // TilePrefab ID: 1
            private const string hexagon2dTileTypeTextureName = "T_WFC_Hexagon2D";  // TilePrefab ID: 2
            private const string hexagon3dTileTypeTextureName = "T_WFC_Hexagon3D";  // TilePrefab ID: 3

            protected Texture2D Square2dTileTypeTexture
            {
                get
                {
                    if (_square2dTileTypeTexture != null)
                        return _square2dTileTypeTexture;

                    // Automatically filling the reference:
                    string[] guids = AssetDatabase.FindAssets(square2dTileTypeTextureName);

                    if (guids.Length == 0)
                    {
                        Debug.LogError("Can't locate file " + square2dTileTypeTextureName
                            + ".png inside the project's folders.");
                        return null;
                    }

                    if (guids.Length > 2)
                    {
                        Debug.LogError("The project contains more than 1 " + square2dTileTypeTextureName + ".png file." +
                            "\nChange the name of the duplicated file or assign the Texture field in the ScriptableTileTypeEditor script." +
                            "\nNot doing so might result in unwanted behavior from the custom editor for Tile Types.");
                    }

                    _square2dTileTypeTexture =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));

                    return _square2dTileTypeTexture;
                }
            }    // TilePrefab ID: 0
            protected Texture2D Square3dTileTypeTexture
            {
                get
                {
                    if (_square3dTileTypeTexture != null)
                        return _square3dTileTypeTexture;

                    // Automatically filling the reference:
                    string[] guids = AssetDatabase.FindAssets(square3dTileTypeTextureName);

                    if (guids.Length == 0)
                    {
                        Debug.LogError("Can't locate file " + square3dTileTypeTextureName
                            + ".png inside the project's folders.");
                        return null;
                    }

                    if (guids.Length > 2)
                    {
                        Debug.LogError("The project contains more than 1 " + square3dTileTypeTextureName + ".png file." +
                            "\nChange the name of the duplicated file or assign the Texture field in the ScriptableTileTypeEditor script." +
                            "\nNot doing so might result in unwanted behavior from the custom editor for Tile Types.");
                    }

                    _square3dTileTypeTexture =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));

                    return _square3dTileTypeTexture;
                }
            }    // TilePrefab ID: 1
            protected Texture2D Hexagon2dTileTypeTexture
            {
                get
                {
                    if (_hexagon2dTileTypeTexture != null)
                        return _hexagon2dTileTypeTexture;

                    // Automatically filling the reference:
                    string[] guids = AssetDatabase.FindAssets(hexagon2dTileTypeTextureName);

                    if (guids.Length == 0)
                    {
                        Debug.LogError("Can't locate file " + hexagon2dTileTypeTextureName
                            + ".png inside the project's folders.");
                        return null;
                    }

                    if (guids.Length > 2)
                    {
                        Debug.LogError("The project contains more than 1 " + hexagon2dTileTypeTextureName + ".png file." +
                            "\nChange the name of the duplicated file or assign the Texture field in the ScriptableTileTypeEditor script." +
                            "\nNot doing so might result in unwanted behavior from the custom editor for Tile Types.");
                    }

                    _hexagon2dTileTypeTexture =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));

                    return _hexagon2dTileTypeTexture;
                }
            }   // TilePrefab ID: 2
            protected Texture2D Hexagon3dTileTypeTexture
            {
                get
                {
                    if (_hexagon3dTileTypeTexture != null)
                        return _hexagon3dTileTypeTexture;

                    // Automatically filling the reference:
                    string[] guids = AssetDatabase.FindAssets(hexagon3dTileTypeTextureName);

                    if (guids.Length == 0)
                    {
                        Debug.LogError("Can't locate file " + hexagon3dTileTypeTextureName
                            + ".png inside the project's folders.");
                        return null;
                    }

                    if (guids.Length > 2)
                    {
                        Debug.LogError("The project contains more than 1 " + hexagon3dTileTypeTextureName + ".png file." +
                            "\nChange the name of the duplicated file or assign the Texture field in the ScriptableTileTypeEditor script." +
                            "\nNot doing so might result in unwanted behavior from the custom editor for Tile Types.");
                    }

                    _hexagon3dTileTypeTexture =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));

                    return _hexagon3dTileTypeTexture;
                }
            }   // TilePrefab ID: 3

            protected Texture2D ObtainTileTypeTextureForID(int tileTypeID)
            {
                switch (tileTypeID)
                {
                    // Square 2D
                    case 0:
                        return Square2dTileTypeTexture;

                    // Square 3D
                    case 1:
                        return Square3dTileTypeTexture;

                    // Hexagon 2D
                    case 2:
                        return Hexagon2dTileTypeTexture;

                    // Hexagon 3D
                    case 3:
                        return Hexagon3dTileTypeTexture;

                    default:
                        Debug.LogWarning("Unrecognised Tile Type id. Cannot display custom Texture for this tile type.");
                        return null;
                }
            }
            #endregion

            #region No Preview Availible Texture in the Custom Inspector

            public static Texture2D ObtainNoPreviewAvailibleTexture()
            {
                // Automatically filling the NoPreviewAvailible reference:
                string[] guids = AssetDatabase.FindAssets("T_WFC_NoPreviewAvailible");

                if (guids.Length == 0)
                {
                    Debug.LogError("Cannot locate file T_WFC_NoPreviewAvailible.png inside the project's folders.");
                    return null;
                }

                if (guids.Length > 2)
                {
                    Debug.LogError("The project cannot contain more than 1 T_WFC_NoPreviewAvailible file." +
                        "\nOtherwise, the Tile Types custom inspector might present unwanted behavior.");
                }

                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            #endregion

            #region Obtaining Prefab Previews

            // Only works for prefabs.
            public static Texture2D GetGameObjectPreview(GameObject gameObject)
            {
                if (gameObject == null)
                    return null;

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
                string path = AssetDatabase.GetAssetPath(prefab);
                var editor = CreateEditor(prefab);
                Texture2D tex = editor.RenderStaticPreview(path, null, 200, 200);
                DestroyImmediate(editor);
                return tex;
            }

            #endregion

            #region Find Files by Name

            public static VisualTreeAsset ObtainVisualTreeAssetByName(string uxmlFileName)
            {
                // Automatically filling the UXML reference:
                string[] guids = AssetDatabase.FindAssets(uxmlFileName);

                if (guids.Length == 0)
                {
                    Debug.LogError("Cannot locate file " + uxmlFileName + ".uxml inside the project's folders.");
                    return null;
                }

                if (guids.Length > 2)
                {
                    Debug.LogWarning("The project cannot contain more than 1 " + uxmlFileName + " file." +
                        "\nOtherwise, the unwanted behavior might be generated.");
                }

                return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            public static StyleSheet ObtainStyleSheetAssetByName(string ussFileName)
            {

                // Automatically filling the USS reference:
                string[] guids = AssetDatabase.FindAssets(ussFileName);

                if (guids.Length == 0)
                {
                    Debug.LogError("Can't locate file " + ussFileName + ".uss inside the project's folders.");
                    return null;
                }

                if (guids.Length > 2)
                {
                    Debug.LogError("The project cannot contain more than 1 " + ussFileName + " file." +
                        "\nOtherwise, the Editor Styles of the WFC Custom inspector might be affected.");
                }

                return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            #endregion

            #region Handling VisualElements

            /// <summary>
            /// Sets a VisualElement's DisplayStyle to "None" (invisible and not occupying space).
            /// </summary>
            /// <param name="visualElement"></param>
            public static void HideVisualElement(VisualElement visualElement)
            {
                visualElement.style.display = DisplayStyle.None;
            }

            /// <summary>
            /// Sets a VisualElement's DisplayStyle to "Flex" (visible and occupying a space).
            /// </summary>
            /// <param name="visualElement"></param>
            public static void RevealVisualElement(VisualElement visualElement)
            {
                visualElement.style.display = DisplayStyle.Flex;
            }

            #endregion
        }
    }
}


