using System.IO;
using UnityEditor;
using UnityEngine;

// Debug helpers under the Tools menu. Editor only, never ships in a build.
public static class SaveTools
{
    [MenuItem("Tools/Save/Open Save Folder")]
    static void OpenFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }

    [MenuItem("Tools/Save/Delete Save (start fresh)")]
    static void DeleteSave()
    {
        if (!EditorUtility.DisplayDialog("Delete save", "Delete the save file and its backup?", "Delete", "Cancel"))
            return;

        foreach (var name in new[] { "save.json", "save.backup.json", "save.tmp" })
        {
            string path = Path.Combine(Application.persistentDataPath, name);
            if (File.Exists(path)) File.Delete(path);
        }
        Debug.Log("Save deleted.");
    }
}
