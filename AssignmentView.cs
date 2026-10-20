using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Text = UnityEngine.UI.Text;
using Image = UnityEngine.UI.Image;

public class AssignmentView : MonoBehaviour
{
    class BuildingSlot { public BuildingDefinition Def; public UnityEngine.UI.Button Button; public Text Label; }
    class HeroSlot { public HeroInstance Instance; public UnityEngine.UI.Button Button; public Text Label; }

    static readonly Color PanelColor = new Color(0.16f, 0.19f, 0.25f);
    static readonly Color SelectedColor = new Color(0.3f, 0.5f, 0.9f);
    static readonly Color DefaultColor = new Color(0.22f, 0.25f, 0.32f);
    static readonly Color CloseButtonColor = new Color(0.8f, 0.2f, 0.2f);

    KingdomService _svc;
    PlayerSave _save;
    GameDatabase _db;
    Font _font;

    GameObject _panel;
    string _selectedBuildingId = "";

    readonly List<BuildingSlot> _bSlots = new List<BuildingSlot>();
    readonly List<HeroSlot> _hSlots = new List<HeroSlot>();

    public void Init(KingdomService svc, PlayerSave save, GameDatabase db)
    {
        _svc = svc; _save = save; _db = db;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildScreen();
        ShowPanel(false);
        Refresh();
    }

    public void SelectBuilding(string buildingId)
    {
        UnityEngine.Debug.Log($"[ASSIGNMENT] Selecting building: {buildingId}");
        _selectedBuildingId = buildingId;
        Refresh();
    }

    public void ShowPanel(bool show)
    {
        if (_panel == null) return;
        _panel.SetActive(show);
        if (show) Refresh();
    }

    void BuildScreen()
    {
        var canvasGo = new GameObject("AssignmentCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Transform root = canvasGo.transform;
        _panel = new GameObject("AssignPanel").AddComponent<RectTransform>().gameObject;
        _panel.transform.SetParent(root, false);
        Stretch(_panel.GetComponent<RectTransform>());
        AddImage(_panel.GetComponent<RectTransform>(), new Color(0.1f, 0.12f, 0.16f));

        // --- CLOSE BUTTON ---
        var closeBtnGo = NewUI("CloseButton", _panel.transform);
        closeBtnGo.anchorMin = new Vector2(1, 1);
        closeBtnGo.anchorMax = new Vector2(1, 1);
        closeBtnGo.pivot = new Vector2(1, 1);
        closeBtnGo.sizeDelta = new Vector2(60, 60);
        closeBtnGo.anchoredPosition = new Vector2(-20, -20);
        var closeImg = AddImage(closeBtnGo, CloseButtonColor);
        var closeButton = closeBtnGo.gameObject.AddComponent<UnityEngine.UI.Button>();
        closeButton.targetGraphic = closeImg;
        var closeLabel = AddText(closeBtnGo, "CloseLabel", 40, TextAnchor.MiddleCenter, Color.white);
        closeLabel.text = "X";
        Stretch(closeLabel.rectTransform);
        closeButton.onClick.AddListener(() => ShowPanel(false));
        // --------------------

        var header = NewUI("Header", _panel.transform);
        header.sizeDelta = new Vector2(1920, 100);
        header.anchoredPosition = new Vector2(0, 440);
        var title = AddText(header, "Title", 50, TextAnchor.MiddleCenter, Color.white);
        title.text = "Assign Heroines to Buildings";
        Stretch(title.rectTransform);

        var leftCol = NewUI("BuildingsCol", _panel.transform);
        leftCol.sizeDelta = new Vector2(600, 800);
        leftCol.anchoredPosition = new Vector2(-400, 0);
        var lLayout = leftCol.gameObject.AddComponent<VerticalLayoutGroup>();
        lLayout.spacing = 10; lLayout.childControlWidth = true; lLayout.childForceExpandWidth = true;
        AddText(leftCol, "LTitle", 30, TextAnchor.UpperCenter, Color.gray).text = "Select Building";

        var rightCol = NewUI("HeroesCol", _panel.transform);
        rightCol.sizeDelta = new Vector2(600, 800);
        rightCol.anchoredPosition = new Vector2(400, 0);
        var rLayout = rightCol.gameObject.AddComponent<VerticalLayoutGroup>();
        rLayout.spacing = 10; rLayout.childControlWidth = true; rLayout.childForceExpandWidth = true;
        AddText(rightCol, "RTitle", 30, TextAnchor.UpperCenter, Color.gray).text = "Select Heroine";

        foreach (var def in _svc.Buildings)
        {
            var btn = AddButton(leftCol, "BBtn_" + def.Id, DefaultColor, out Text lbl);
            lbl.text = def.DisplayName;
            var slot = new BuildingSlot { Def = def, Button = btn, Label = lbl };
            btn.onClick.AddListener(() => { _selectedBuildingId = def.Id; Refresh(); });
            _bSlots.Add(slot);
        }

        foreach (var hero in _save.Heroes)
        {
            var btn = AddButton(rightCol, "HBtn_" + hero.HeroId, DefaultColor, out Text lbl);
            lbl.text = _db.FindHero(hero.HeroId).DisplayName;
            var slot = new HeroSlot { Instance = hero, Button = btn, Label = lbl };
            btn.onClick.AddListener(() => {
                if (!string.IsNullOrEmpty(_selectedBuildingId))
                {
                    _svc.AssignHero(_selectedBuildingId, hero.HeroId);
                    Refresh();
                }
            });
            _hSlots.Add(slot);
        }
    }

    void Refresh()
    {
        foreach (var s in _bSlots)
        {
            s.Button.GetComponent<Image>().color = (s.Def.Id == _selectedBuildingId) ? SelectedColor : DefaultColor;
            var currentHero = _svc.GetHeroName(_save.GetBuilding(s.Def.Id)?.AssignedHeroId);
            s.Label.text = $"{s.Def.DisplayName} ({currentHero})";
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
        var rt = NewUI(name, parent); var t = rt.gameObject.AddComponent<Text>();
        t.font = _font; t.fontSize = size; t.alignment = anchor; t.color = color;
        return t;
    }
    UnityEngine.UI.Button AddButton(Transform parent, string name, Color color, out Text label)
    {
        var rt = NewUI(name, parent); var img = AddImage(rt, color);
        var btn = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); btn.targetGraphic = img;
        label = AddText(rt, "Label", 28, TextAnchor.MiddleCenter, Color.white);
        Stretch(label.rectTransform);
        return btn;
    }
}
