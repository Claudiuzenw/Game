using UnityEngine;

// A cosmetic skin for a hero. Skins are the main paid item and never change
// stats. There is no shop yet: this is the content definition plus the save
// fields (PlayerSave.OwnedSkinIds, HeroInstance.SkinId) so a store can be added later.
[CreateAssetMenu(menuName = "Game/Skin", fileName = "skin_new")]
public class SkinDefinition : ScriptableObject
{
    [Tooltip("Stable ID used in save files, e.g. skin_aria_summer")]
    public string Id;
    public string DisplayName;
    public HeroDefinition Hero;

    [Header("Art")]
    public Sprite Portrait;
    public Sprite Icon;

    [Header("Store (hooked up later, e.g. Steam purchase)")]
    public string StoreProductId;
    public string PriceLabel = "$4.99";

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(Id)) Id = name;
    }
#endif
}
