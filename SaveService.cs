using System;
using System.IO;
using UnityEngine;

// Plain C# class (no MonoBehaviour) so it can be tested and reused.
// Saves are written to a temp file first, then swapped in, and the previous
// good save is kept as a backup. A crash mid-write cannot corrupt progress.
public class SaveService
{
    public const int CurrentVersion = 1;

    const string FileName = "save.json";
    const string BackupName = "save.backup.json";
    const string TempName = "save.tmp";

    readonly string _path;
    readonly string _backupPath;
    readonly string _tempPath;

    public string SaveDirectory { get; }
    public PlayerSave Data { get; private set; }

    // True if a save file existed when Load() ran.
    public bool LoadedFromExistingSave { get; private set; }

    // Seconds between the last save and this Load(). Feed this to the offline reward step.
    public long OfflineSeconds { get; private set; }

    public SaveService(string directory = null)
    {
        SaveDirectory = directory ?? Application.persistentDataPath;
        _path = Path.Combine(SaveDirectory, FileName);
        _backupPath = Path.Combine(SaveDirectory, BackupName);
        _tempPath = Path.Combine(SaveDirectory, TempName);
    }

    public PlayerSave Load()
    {
        var data = TryRead(_path);
        if (data == null) data = TryRead(_backupPath);

        LoadedFromExistingSave = data != null;
        if (data == null) data = PlayerSave.CreateNew();

        Migrate(data);
        Data = data;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        OfflineSeconds = LoadedFromExistingSave ? Math.Max(0, now - data.LastSavedUnix) : 0;
        return Data;
    }

    public void Save()
    {
        if (Data == null) return;

        Data.Version = CurrentVersion;
        Data.LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string json = JsonUtility.ToJson(Data, true);

        try
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(_tempPath, json);

            if (File.Exists(_path)) File.Replace(_tempPath, _path, _backupPath);
            else File.Move(_tempPath, _path);
        }
        catch (Exception e)
        {
            Debug.LogError($"Save failed: {e}");
        }
    }

    PlayerSave TryRead(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<PlayerSave>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not read {path}: {e.Message}");
            return null;
        }
    }

    // Add one step per version bump, oldest first. Example for a future version 2:
    //   if (data.Version < 2) { /* change old fields */ data.Version = 2; }
    void Migrate(PlayerSave data)
    {
        data.Version = CurrentVersion;
    }
}
