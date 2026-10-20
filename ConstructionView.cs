using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Text = UnityEngine.UI.Text;
using Image = UnityEngine.UI.Image;

public class ConstructionView : MonoBehaviour
{
    class SlotUi
    {
        public Text Status;
        public UnityEngine.UI.Button Button;
        public Text ButtonLabel;
    }

    static readonly Color PanelColor = new Color(0.16f, 0.19f, 0.25f);
    static readonly Color GreenColor = new Color(0.25f, 0.62f, 0.38f);
    static readonly Color TabOn = new Color(0.25f, 0.45f, 0.75f);
    static readonly Color TabOff = new Color(0.22f, 0.25f, 0.32f);

    ConstructionService _svc;
    PlayerSave _save;
    GameDatabase _db;
    Font _font;

    GameObject _panel;
    int _poolIndex;
    readonly List<Image> _poolTabImages = new List<Image>();
    readonly List<SlotUi> _slots = new List<SlotUi>();
    readonly Queue<GrantResult> _reveals = new Queue<GrantResult>();

    Text _infoText, _claimLabel, _constructionTabLabel, _revealText;
    UnityEngine.UI.Button _claimButton;
    Image _kingdomTabImage, _constructionTabImage;
    GameObject _revealPopup;
    float _timer;

    public void Init(ConstructionService svc, PlayerSave save, GameDatabase db)
    {
        _svc = svc; _save = save; _db = db;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildScreen();
        ShowPanel(false);
        Refresh();
    }

    void Update()
    {
        if (_svc == null) return;
        _timer += Time.unscaledDeltaTime;
        if (_timer >= 0.2f) { _timer = 0f; Refresh(); }
    }

    ConstructionPool SelectedPool()
    {
        if (_svc.Pools.Count == 0) return null;
        _poolIndex = Mathf.Clamp(_poolIndex, 0, _svc.Pools.Count - 1);
        return _svc.Pools[_poolIndex];
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

    void BuildScreen()
    {
        var canvasGo = new GameObject("ConstructionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Transform root = canvasGo.transform;
        var panel = NewUI("Panel", root);
        _panel = panel.gameObject;
        panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
        panel.offsetMin = new Vector2(0, 120); panel.offsetMax = new Vector2(0, -100);
        AddImage(panel, new Color(0.10f, 0.12f, 0.16f));
        var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(60, 60, 15, 15); panelLayout.spacing = 18;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true; panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true; panelLayout.childForceExpandHeight = false;

        var poolRow = NewUI("PoolRow", panel);
        poolRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
        var poolLayout = poolRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        poolLayout.spacing = 20; poolLayout.childControlWidth = true; poolLayout.childControlHeight = true;
        poolLayout.childForceExpandWidth = true; poolLayout.childForceExpandHeight = true;
        for (int i = 0; i < _svc.Pools.Count; i++)
        {
            int index = i;
            var tab = AddButton(poolRow, "Pool" + i, TabOff, out Text tabLabel);
            tabLabel.text = _svc.Pools[i].DisplayName.Replace("Construction", "Adventurer");
            tabLabel.fontSize = 36;
            _poolTabImages.Add(tab.GetComponent<Image>());
            tab.onClick.AddListener(() => { _poolIndex = index; Refresh(); });
        }

        var info = NewUI("InfoBox", panel);
        AddImage(info, PanelColor);
        info.gameObject.AddComponent<LayoutElement>().preferredHeight = 370;
        var infoLayout = info.gameObject.AddComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(30, 30, 15, 15); infoLayout.spacing = 10;
        infoLayout.childControlWidth = true; infoLayout.childControlHeight = true;
        _infoText = AddText(info, "InfoText", 28, TextAnchor.UpperLeft, Color.white);
        _claimButton = AddButton(info, "ClaimButton", GreenColor, out _claimLabel);
        _claimButton.onClick.AddListener(() => { EnqueueReveal(_svc.Claim(SelectedPool())); Refresh(); });

        var slotsRow = NewUI("SlotsRow", panel);
        slotsRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 260;
        var slotsLayout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        slotsLayout.spacing = 30; slotsLayout.childControlWidth = true; slotsLayout.childControlHeight = true;
        for (int i = 0; i < ConstructionService.SlotCount; i++) BuildSlot(slotsRow, i);

        var nav = NewUI("NavBar", root);
        nav.anchorMin = new Vector2(0.5f, 0); nav.anchorMax = new Vector2(0.5f, 0);
        nav.pivot = new Vector2(0.5f, 0); nav.sizeDelta = new Vector2(900, 110);
        nav.anchoredPosition = new Vector2(0, 5);
        AddImage(nav, new Color(0.07f, 0.08f, 0.11f));
        var navLayout = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
        navLayout.padding = new RectOffset(15, 15, 10, 10); navLayout.spacing = 20;
        navLayout.childControlWidth = true; navLayout.childControlHeight = true;
        navLayout.childForceExpandWidth = true; navLayout.childForceExpandHeight = true;

        var kingdomTab = AddButton(nav, "KingdomTab", TabOn, out Text kingdomLabel);
        kingdomLabel.text = "Kingdom";
        _kingdomTabImage = kingdomTab.GetComponent<Image>();
        kingdomTab.onClick.AddListener(() => {
            ShowPanel(false);
            var assign = gameObject.GetComponent<AssignmentView>();
            if (assign != null) assign.ShowPanel(false);
            var coll = gameObject.GetComponent<CollectionView>();
            if (coll != null) coll.ShowPanel(false);
        });

        var constructionTab = AddButton(nav, "ConstructionTab", TabOff, out _constructionTabLabel);
        _constructionTabLabel.text = "Construction";
        _constructionTabImage = constructionTab.GetComponent<Image>();
        constructionTab.onClick.AddListener(() => {
            ShowPanel(true);
            var assign = gameObject.GetComponent<AssignmentView>();
            if (assign != null) assign.ShowPanel(false);
            var coll = gameObject.GetComponent<CollectionView>();
            if (coll != null) coll.ShowPanel(false);
        });

        var collectTab = AddButton(nav, "CollectTab", TabOff, out Text collectLabel);
        collectLabel.text = "Collection";
        collectTab.onClick.AddListener(() => {
            ShowPanel(false);
            var assign = gameObject.GetComponent<AssignmentView>();
            if (assign != null) assign.ShowPanel(false);
            var coll = gameObject.GetComponent<CollectionView>();
            if (coll != null) coll.ShowPanel(true);
        });

        BuildRevealPopup(root);
    }

    void BuildSlot(RectTransform parent, int index)
    {
        var card = NewUI("Slot" + index, parent);
        AddImage(card, PanelColor);
        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(25, 25, 15, 15); layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        var status = AddText(card, "Status", 28, TextAnchor.UpperLeft, Color.white);
        var button = AddButton(card, "Action", GreenColor, out Text label);
        button.onClick.AddListener(() => OnSlotClicked(index));
        _slots.Add(new SlotUi { Status = status, Button = button, ButtonLabel = label });
    }

    void BuildRevealPopup(Transform root)
    {
        var dim = NewUI("RevealPopup", root);
        _revealPopup = dim.gameObject;
        Stretch(dim);
        AddImage(dim, new Color(0, 0, 0, 0.8f));
        var box = NewUI("Box", dim);
        box.anchorMin = new Vector2(0.5f, 0.5f); box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 0.5f); box.sizeDelta = new Vector2(900, 640);
        AddImage(box, PanelColor);
        _revealText = AddText(box, "Message", 40, TextAnchor.MiddleCenter, Color.white);
        var ok = AddButton(box, "OkButton", GreenColor, out Text okLabel);
        okLabel.text = "OK";
        ok.onClick.AddListener(ShowNextReveal);
        _revealPopup.SetActive(false);
    }

    void ShowPanel(bool show)
    {
        _panel.SetActive(show);
        _kingdomTabImage.color = show ? TabOff : TabOn;
        _constructionTabImage.color = show ? TabOn : TabOff;
        Refresh();
    }

    void OnSlotClicked(int index)
    {
        var slot = _save.Slots[index];
        if (slot.Active)
        {
            if (_svc.IsReady(slot)) EnqueueReveal(_svc.Collect(index));
        }
        else
        {
            var pool = SelectedPool();
            if (pool != null && _svc.TryStart(pool, index)) { }
        }
        Refresh();
    }

    void EnqueueReveal(GrantResult result)
    {
        if (result == null) return;
        _reveals.Enqueue(result);
        if (!_revealPopup.activeSelf) ShowNextReveal();
    }

    void ShowNextReveal()
    {
        if (_reveals.Count == 0) { _revealPopup.SetActive(false); return; }
        GrantResult res = _reveals.Dequeue();
        _revealText.text = Describe(res);
        _revealPopup.SetActive(true);
    }

    void Refresh()
    {
        var pool = SelectedPool();
        for (int i = 0; i < _poolTabImages.Count; i++) _poolTabImages[i].color = (i == _poolIndex) ? TabOn : TabOff;
        if (!_panel.activeSelf) return;
        if (pool == null) return;

        _infoText.text = BuildInfo(pool);
        RefreshClaim(pool);

        for (int i = 0; i < _slots.Count; i++)
        {
            var ui = _slots[i];
            var slot = _save.Slots[i];
            if (slot.Active)
            {
                if (_svc.IsReady(slot))
                {
                    ui.Status.text = "<color=yellow><b>ADVENTURER FOUND!</b></color>";
                    ui.ButtonLabel.text = "Collect Now!";
                }
                else
                {
                    ui.Status.text = $"Searching Adventurer...\n{NumberFormat.Duration(_svc.SecondsLeft(slot))}";
                    ui.ButtonLabel.text = "Wait...";
                }
            }
            else
            {
                ui.Status.text = "Empty";
                ui.ButtonLabel.text = "Start";
            }
        }
    }

    void RefreshClaim(ConstructionPool pool)
    {
        bool hasClaim = pool.PityToClaim > 0 && pool.Featured != null;
        _claimButton.gameObject.SetActive(hasClaim);
        if (!hasClaim) return;

        if (_svc.OwnsHero(pool.Featured))
        {
            _claimLabel.text = pool.Featured.DisplayName + " owned.";
            _claimButton.interactable = false;
        }
        else if (_svc.CanClaim(pool))
        {
            _claimLabel.text = "Claim free copy: " + pool.Featured.DisplayName;
            _claimButton.interactable = true;
        }
        else
        {
            _claimLabel.text = "Free copy at " + pool.PityToClaim + " points";
            _claimButton.interactable = false;
        }
    }

    string BuildInfo(ConstructionPool pool)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<size=40><b>{pool.DisplayName}</b></size>");
        sb.AppendLine($"Cost: {NumberFormat.Short(pool.CostGold)} Gold + {NumberFormat.Short(pool.CostGems)} Gems");
        sb.AppendLine($"Rates: SSR {NumberFormat.Short(pool.Percent(Rarity.SSR))}% | SR {NumberFormat.Short(pool.Percent(Rarity.SR))}% | R {NumberFormat.Short(pool.Percent(Rarity.R))}%");
        if (pool.Featured != null) sb.AppendLine($"Featured: {pool.Featured.DisplayName}");
        return sb.ToString();
    }

    string Describe(GrantResult result)
    {
        if (result == null || result.Hero == null) return "No adventurer found!";
        var hero = result.Hero;
        var sb = new StringBuilder();
        string colorHex = "#FFFFFF";
        if (hero.Rarity == Rarity.SSR) colorHex = "#FFD94D";
        else if (hero.Rarity == Rarity.SR) colorHex = "#C58CFF";
        else colorHex = "#9FB4C9";

        sb.AppendLine($"<size=40>New Adventurer Found!</size>");
        sb.AppendLine();
        sb.AppendLine($"<size=60><color={colorHex}><b>{hero.DisplayName}</b></color></size>");
        sb.AppendLine($"<size=30>{hero.Rarity} {hero.Class}</size>");
        sb.AppendLine();
        if (result.Kind == GrantKind.NewHero) sb.AppendLine("<color=green>FIRST COPY!</color>");
        else if (result.Kind == GrantKind.LimitBreak) sb.AppendLine($"<color=cyan>Limit Break! Stars: {result.Stars}/4</color>");
        else sb.AppendLine($"<color=orange>Duplicate! +{NumberFormat.Short(result.ExpGained)} Hero EXP</color>");
        return sb.ToString();
    }

    RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
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
        label = AddText(rt, "Label", 32, TextAnchor.MiddleCenter, Color.white);
        Stretch(label.rectTransform);
        return btn;
    }
}
