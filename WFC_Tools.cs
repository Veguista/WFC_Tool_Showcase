using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace WFC
{
    public static class WFC_Tools
    {
        /// <summary>
        /// Searches the indicated Folder path for Scriptable Objects of a concrete type. 
        /// Allows for children folders to also be searched.
        /// </summary>
        /// <typeparam name = "T" > Class name of Scriptable Object.</typeparam>
        /// <param name="folder">Folder path where the Scriptable Objects are (EX: "Assets/ScriptableObjects").</param>
        /// <param name="searchChildrenFolders"></param>
        /// <returns>All Scriptable Objects of Type T inside the indicated folder/s.</returns>
        public static List<T> FindAllScriptableObjectsOfType<T>
            (string folder = "Assets", bool searchChildrenFolders = false)
            where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning("Folder Path <" + folder + "> is not valid." +
                    "\nThus, no instances of " + nameof(T) + " can be found there.");
                return new List<T>();
            }

            List<T> resultOfFirstSearch = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();   // AssetDatabase.FindAssets returns objects in children folders by default.

            if (resultOfFirstSearch.Count == 0)
                return new List<T>();

            if (searchChildrenFolders)
                return resultOfFirstSearch;


            string[] subFolders = AssetDatabase.GetSubFolders(folder);

            for (int i = 0; i < subFolders.Length; i++)
            {
                List<T> childSearch = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { subFolders[i] })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                    .ToList();

                if (childSearch.Count == 0)
                    continue;

                foreach (T childFound in childSearch)
                    resultOfFirstSearch.Remove(childFound);
            }

            return resultOfFirstSearch;
        }

        /// <summary>
        /// Returns a path without the folder path outside of the Unity folder.
        /// (Ex. An input of "[path to project folder]/Assets/Scripts" will return "Assets/Scripts").
        /// </summary>
        /// <param name="fullPath">Full path to a File or a Directory.</param>
        /// <returns></returns>
        public static string GetUnityPathFromFullPath(string fullPath)
        {
            const char unityApplicationDataPathSeparationChar = '/';

            // Finding which of those assets are located in Children folders.
            List<string> dataPathParts = new();
            dataPathParts.AddRange(Application.dataPath.Split(unityApplicationDataPathSeparationChar));

            if (dataPathParts.Count <= 1)
            {
                Debug.LogError("There was an error dividing the Application.dataPath string into parts.");
                return null;
            }

            // We reconstruct the application.dataPath
            string sectionToRemoveFromFullPath = dataPathParts[0] + Path.DirectorySeparatorChar;

            // We add all file names but the final one (The Assets / Contents folder)
            for (int i = 1; i < dataPathParts.Count - 1; i++)
                sectionToRemoveFromFullPath = Path.Combine(sectionToRemoveFromFullPath, dataPathParts[i]);

            // But we leave the last unitySeparationChar without a Directory at it's end.
            sectionToRemoveFromFullPath += Path.DirectorySeparatorChar;

            // For issues of compatibility with how Unity manages its own files, we need to replace any non "/" chars
            // with "/" chars. Unity does not use "\" char for example when managing its folders I believe.
            if (Path.DirectorySeparatorChar != '/')
                sectionToRemoveFromFullPath = sectionToRemoveFromFullPath.Replace(Path.DirectorySeparatorChar, '/');

            // Just in case, we prevent user error by swapping any unnecessary or wrong Chars used in input.
            string correctedFullPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');

            // And finally, we create the new path without the initial part of the Path.
            return correctedFullPath.Replace(sectionToRemoveFromFullPath, "");
        }
    }
}