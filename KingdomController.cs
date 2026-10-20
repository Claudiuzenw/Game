using UnityEngine;
using System;

public class KingdomController : MonoBehaviour
{
    KingdomService _kingdom;
    ConstructionService _construction;

    void Start()
    {
        var boot = GameBootstrap.Instance;
        if (boot == null) return;

        var db = Resources.Load<GameDatabase>("GameDatabase");
        if (db == null) return;

        UnityEngine.Application.runInBackground = true;

        _kingdom = new KingdomService(db, boot.Data);
        _kingdom.EnsureStarterBuildings();

        var offline = OfflineCalculator.Compute(_kingdom, boot.Saves.OfflineSeconds);
        if (offline.HasRewards) offline.ApplyTo(boot.Data.Wallet);

        // 1. Kingdom View
        var kingdomView = gameObject.AddComponent<KingdomView>();
        kingdomView.Init(_kingdom, boot.Data, offline);

        // 2. Construction View
        _construction = new ConstructionService(db, boot.Data);
        _construction.Changed += () => boot.Saves.Save();
        var constructionView = gameObject.AddComponent<ConstructionView>();
        constructionView.Init(_construction, boot.Data, db);

        // 3. Assignment View
        var assignmentView = gameObject.AddComponent<AssignmentView>();
        assignmentView.Init(_kingdom, boot.Data, db);
    }

    void Update()
    {
        if (_kingdom == null) return;
        _kingdom.Tick(Mathf.Min(Time.unscaledDeltaTime, 1f));
    }
}
