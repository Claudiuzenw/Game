using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Text = UnityEngine.UI.Text;
using Image = UnityEngine.UI.Image;

public class KingdomView : MonoBehaviour
{
    class Row
    {
        public BuildingDefinition Def;
        public Text Info;
        public Text ButtonLabel;
        public Button Button;
        public Button WorkerButton;
    }

    static readonly Color PanelColor = new Color(0.16f, 0.19f, 0.25f);
    static readonly Color GreenColor = new Color(0.25f, 0.62f, 0.38f);
    static readonly Color BlueColor = new Color(0.2f, 0.4f, 0.8f);
    static readonly Color GoldColor = new Color(1f, 0.85f, 0.35f);

    KingdomService _kingdom;
    PlayerSave _save;
    Font _font;
    readonly List<Row> _rows = new List<Row>();
    Text _goldText, _expText, _gemsText, _incomeText;
    GameObject _offlinePopup;
    float _refreshTimer;

    public void Init(KingdomService kingdom, PlayerSave save, OfflineResult offline)
    {
        _kingdom = kingdom;
        _save = save;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildScreen(offline);
        Refresh();
    }

    void Update()
    {
        if (_kingdom == null) return;
        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer >= 0.1f)
        {
            _refreshTimer = 0f;
            Refresh();
        }
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    void BuildScreen(OfflineResult offline)
    {
        var canvasGo = new GameObject("KingdomCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        Transform root = canvasGo.transform;
        var bg = NewUI("Background", root);
        Stretch(bg);
        AddImage(bg, new Color(0.10f, 0.12f, 0.16f));

        var top = NewUI("TopBar", root);
        top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1); top.pivot = new Vector2(0.5f, 1);
        top.sizeDelta = new Vector2(0, 100); top.anchoredPosition = Vector2.zero;
        AddImage(top, new Color(0.07f, 0.08f, 0.11f));
        var topLayout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(40, 40, 10, 10); topLayout.spacing = 40;
        topLayout.childAlignment = TextAnchor.MiddleLeft;
        _goldText = AddText(top, "Gold", 38, TextAnchor.MiddleLeft, GoldColor);
        _expText = AddText(top, "HeroExp", 38, TextAnchor.MiddleLeft, new Color(0.6f, 0.85f, 1f));
        _gemsText = AddText(top, "Gems", 38, TextAnchor.MiddleLeft, new Color(0.9f, 0.6f, 1f));
        _incomeText = AddText(top, "Income", 34, TextAnchor.MiddleRight, Color.white);

        var list = NewUI("BuildingList", root);
        list.anchorMin = new Vector2(0.5f, 0.5f); list.anchorMax = new Vector2(0.5f, 0.5f);
        list.pivot = new Vector2(0.5f, 0.5f); list.sizeDelta = new Vector2(1400, 780);
        list.anchoredPosition = new Vector2(0, -50);
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 18; listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = true; listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true; listLayout.childForceExpandHeight = false;

        int buildingIdx = 0;
        foreach (var def in _kingdom.Buildings)
        {
            if (def != null) BuildRow(list, buildingIdx++, def);
        }

        if (offline != null && offline.HasRewards) BuildOfflinePopup(root, offline);
    }

    void BuildRow(RectTransform parent, int index, BuildingDefinition def)
    {
        var row = NewUI("Row_" + def.Id, parent);
        AddImage(row, PanelColor);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 15, 15);
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var info = AddText(row, "Info", 34, TextAnchor.MiddleLeft, Color.white);
        info.rectTransform.sizeDelta = new Vector2(500, 100);

        var upgradeBtn = AddButton(row, "UpgradeButton", GreenColor, out Text upgradeLabel);
        upgradeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 100);
        upgradeBtn.onClick.AddListener(() => { _kingdom.TryUpgrade(def); Refresh(); });

        // MODIFIED: Now opens the Assignment Tab and selects the building
        var workerBtn = AddButton(row, "WorkerButton", BlueColor, out Text workerLabel);
        workerBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 100);
        workerBtn.onClick.AddListener(() => {
            var assignView = gameObject.GetComponent<AssignmentView>();
            if (assignView != null)
            {
                assignView.SelectBuilding(def.Id);
                assignView.ShowPanel(true);
            }
        });

        _rows.Add(new Row { Def = def, Info = info, ButtonLabel = upgradeLabel, Button = upgradeBtn, WorkerButton = workerBtn });
    }

    void BuildOfflinePopup(Transform root, OfflineResult offline)
    {
        var dim = NewUI("OfflinePopup", root);
        _offlinePopup = dim.gameObject;
        Stretch(dim);
        AddImage(dim, new Color(0, 0, 0, 0.75f));

        var box = NewUI("Box", dim);
        box.anchorMin = new Vector2(0.5f, 0.5f); box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 0.5f); box.sizeDelta = new Vector2(900, 560);
        AddImage(box, PanelColor);

        var sb = new StringBuilder();
        sb.AppendLine("<b>Welcome back!</b>");
        sb.Append("Away for " + NumberFormat.Duration(offline.AwaySeconds));
        sb.AppendLine();
        if (offline.Gold >= 1) sb.AppendLine("+" + NumberFormat.Short(Math.Floor(offline.Gold)) + " Gold");
        if (offline.HeroExp >= 1) sb.AppendLine("+" + NumberFormat.Short(Math.Floor(offline.HeroExp)) + " Hero EXP");
        if (offline.Gems >= 1) sb.AppendLine("+" + NumberFormat.Short(Math.Floor(offline.Gems)) + " Gems");

        var text = AddText(box, "Message", 40, TextAnchor.MiddleCenter, Color.white);
        text.rectTransform.anchorMin = new Vector2(0, 0.28f); text.rectTransform.anchorMax = Vector2.one;
        text.text = sb.ToString();

        var collect = AddButton(box, "CollectButton", GreenColor, out Text collectLabel);
        collectLabel.text = "Collect";
        var cr = (RectTransform)collect.transform;
        cr.anchorMin = new Vector2(0.5f, 0); cr.anchorMax = new Vector2(0.5f, 0);
        cr.pivot = new Vector2(0.5f, 0); cr.sizeDelta = new Vector2(360, 90);
        cr.anchoredPosition = new Vector2(0, 40);
        collect.onClick.AddListener(() => _offlinePopup.SetActive(false));
    }

    void Refresh()
    {
        var wallet = _save.Wallet;
        _goldText.text = "Gold  " + NumberFormat.Short(Math.Floor(wallet.Gold));
        _expText.text = "Hero EXP  " + NumberFormat.Short(Math.Floor(wallet.HeroExp));
        _gemsText.text = "Gems  " + NumberFormat.Short(Math.Floor(wallet.Gems));
        _incomeText.text = NumberFormat.Short(_kingdom.IncomePerSecond(ResourceType.Gold)) + " gold/s";

        foreach (var row in _rows)
        {
            var def = row.Def;
            int level = _kingdom.LevelOf(def);
            var state = _save.GetBuilding(def.Id);
            string workerName = "None";
            if (state != null && !string.IsNullOrEmpty(state.AssignedHeroId))
            {
                workerName = _kingdom.GetHeroName(state.AssignedHeroId);
            }

            if (!_kingdom.IsUnlocked(def))
            {
                row.Info.text = $"<b>{def.DisplayName}</b>\n<size=28>Locked</size>";
                row.ButtonLabel.text = "Locked";
                row.Button.interactable = false;
                row.WorkerButton.interactable = false;
            }
            else
            {
                double outSec = _kingdom.OutputPerSecond(def);
                row.Info.text = $"<b>{def.DisplayName} Lv {level}</b>\nWorker: {workerName}\nOut: {NumberFormat.Short(outSec)} gold/s";
                row.ButtonLabel.text = "Upgrade\n" + NumberFormat.Short(_kingdom.UpgradeCost(def)) + " Gold";
                row.Button.interactable = _kingdom.CanUpgrade(def);
                row.WorkerButton.interactable = true;
                row.WorkerButton.GetComponentInChildren<Text>().text = "Assign Hero";
            }
        }
    }

    RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
    static Image AddImage(RectTransform rt, Color color) { var image = rt.gameObject.AddComponent<Image>(); image.color = color; return image; }

    Text AddText(Transform parent, string name, int size, TextAnchor anchor, Color color)
    {
        var rt = NewUI(name, parent);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = _font; text.fontSize = size; text.alignment = anchor; text.color = color;
        text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    Button AddButton(Transform parent, string name, Color color, out Text label)
    {
        var rt = NewUI(name, parent);
        var image = AddImage(rt, color);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f); colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        button.colors = colors;
        label = AddText(rt, "Label", 32, TextAnchor.MiddleCenter, Color.white);
        Stretch(label.rectTransform);
        return button;
    }
}
