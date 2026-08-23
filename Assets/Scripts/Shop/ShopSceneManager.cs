using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopSceneManager : MonoBehaviour
{
    private const string BattleSceneName = "BattleScene";
    private const string WoodSwordId = "wood_sword_01";
    private const string IronSwordId = "iron_sword_01";
    private const float CardWidth = 940f;
    private const float CardHeight = 317f;
    private const float CardGap = 34f;
    private const float FirstCardTopPadding = 24f;
    private static readonly Vector2 IconPos = new Vector2(-350f, 0f);
    private static readonly Vector2 NameTextPos = new Vector2(-65f, 89f);
    private static readonly Vector2 DescriptionTextPos =
        new Vector2(-40f, 46f);
    private static readonly Vector2 AttackTextPos = new Vector2(-117f, -30f);
    private static readonly Vector2 PriceTextPos = new Vector2(116f, -25f);
    private static readonly Vector2 StateButtonPos = new Vector2(330f, -18f);
    private static readonly Vector2 IconSize = new Vector2(190f, 190f);
    private static readonly Vector2 NameTextSize = new Vector2(500f, 54f);
    private static readonly Vector2 DescriptionTextSize =
        new Vector2(560f, 46f);
    private static readonly Vector2 AttackTextSize = new Vector2(205f, 44f);
    private static readonly Vector2 PriceTextSize = new Vector2(205f, 44f);
    private static readonly Vector2 StateButtonSize = new Vector2(185f, 66f);

    [SerializeField] private Sprite serializedTabActiveSprite;
    [SerializeField] private Sprite serializedTabInactiveSprite;
    [SerializeField] private Sprite serializedItemCardSprite;
    [SerializeField] private Sprite serializedItemCardEquippedSprite;
    [SerializeField] private Sprite serializedBuyButtonSprite;

    private readonly Color tabTextColor =
        new Color(0.98f, 0.92f, 0.78f, 1f);
    private readonly Color goldColor =
        new Color(1f, 0.78f, 0.32f, 1f);
    private readonly Color ivoryColor =
        new Color(0.98f, 0.94f, 0.84f, 1f);
    private readonly Color softTextColor =
        new Color(0.94f, 0.90f, 0.82f, 1f);
    private readonly Color greenTextColor =
        new Color(0.65f, 1f, 0.54f, 1f);
    private readonly Color buttonTextColor =
        new Color(0.98f, 0.94f, 0.84f, 1f);

    private SaveService saveService;
    private SaveData saveData;
    private PlayerProgressService playerProgress;
    private ItemService itemService;

    private TMP_FontAsset koreanFont;
    private TMP_Text goldText;
    private TMP_Text messageText;
    private RectTransform itemListContent;
    private Button weaponTabButton;
    private Button armorTabButton;
    private Button accessoryTabButton;
    private Button backButton;
    private Sprite tabActiveSprite;
    private Sprite tabInactiveSprite;
    private Sprite itemCardSprite;
    private Sprite itemCardEquippedSprite;
    private Sprite buyButtonSprite;
    private ItemType currentTab = ItemType.Weapon;
    private bool isLeaving;

    private IEnumerator Start()
    {
        FindUI();

        saveService = new SaveService();
        playerProgress = new PlayerProgressService();
        itemService = new ItemService();

        LoadProgress();
        yield return StartCoroutine(itemService.Initialize());
        ValidateStartingItems();
        RefreshShopUI("");
    }

    private void Update()
    {
        if (isLeaving ||
            Keyboard.current?.escapeKey.wasPressedThisFrame != true)
        {
            return;
        }

        ReturnToBattle();
    }

    private void FindUI()
    {
        koreanFont = GameObject.Find("GoldText")
            ?.GetComponent<TMP_Text>()?.font;
        goldText = GameObject.Find("GoldText")?.GetComponent<TMP_Text>();
        messageText = GameObject.Find("MessageText")?.GetComponent<TMP_Text>();
        itemListContent = GameObject.Find("ShopItemListContent")
            ?.GetComponent<RectTransform>();
        weaponTabButton = GameObject.Find("WeaponTab")?.GetComponent<Button>();
        armorTabButton = GameObject.Find("ArmorTab")?.GetComponent<Button>();
        accessoryTabButton = GameObject.Find("AccessoryTab")
            ?.GetComponent<Button>();
        backButton = GameObject.Find("BackButton")?.GetComponent<Button>();

        tabActiveSprite = serializedTabActiveSprite != null
            ? serializedTabActiveSprite
            : weaponTabButton?.transform.Find("Background")
                ?.GetComponent<Image>()?.sprite;
        tabInactiveSprite = serializedTabInactiveSprite != null
            ? serializedTabInactiveSprite
            : armorTabButton?.transform.Find("Background")
                ?.GetComponent<Image>()?.sprite;
        itemCardSprite = serializedItemCardSprite;
        itemCardEquippedSprite = serializedItemCardEquippedSprite;
        buyButtonSprite = serializedBuyButtonSprite;

        if (weaponTabButton != null)
            weaponTabButton.onClick.AddListener(
                () => SelectTab(ItemType.Weapon)
            );

        if (armorTabButton != null)
            armorTabButton.onClick.AddListener(
                () => SelectTab(ItemType.Armor)
            );

        if (accessoryTabButton != null)
            accessoryTabButton.onClick.AddListener(
                () => SelectTab(ItemType.Unknown)
            );

        if (backButton != null)
            backButton.onClick.AddListener(ReturnToBattle);
    }

    private void LoadProgress()
    {
        if (saveService.TryLoad(out SaveData loadedSaveData))
        {
            saveData = loadedSaveData;
        }
        else
        {
            saveData = new SaveData();
        }

        playerProgress.SetData(saveData.playerProgress);
        saveData.playerProgress = playerProgress.Data;
    }

    private void SaveProgress()
    {
        if (saveData == null)
            saveData = new SaveData();

        saveData.playerProgress = playerProgress.Data;
        saveService.Save(saveData);
    }

    private void ValidateStartingItems()
    {
        if (itemService == null)
            return;

        bool correctedWeapon = ValidateEquippedItem(
            playerProgress.EquippedWeaponId,
            PlayerProgressData.DefaultWeaponId,
            ItemType.Weapon
        );
        bool correctedArmor = ValidateEquippedItem(
            playerProgress.EquippedArmorId,
            PlayerProgressData.DefaultArmorId,
            ItemType.Armor
        );

        if (correctedWeapon || correctedArmor)
            SaveProgress();
    }

    private bool ValidateEquippedItem(
        string equippedItemId,
        string fallbackItemId,
        ItemType expectedType
    )
    {
        ItemData equippedItem = itemService.GetItem(equippedItemId);

        if (equippedItem != null &&
            equippedItem.GetItemType() == expectedType &&
            playerProgress.OwnsItem(equippedItemId))
        {
            return false;
        }

        ItemData fallbackItem = itemService.GetItem(fallbackItemId);

        if (fallbackItem == null ||
            fallbackItem.GetItemType() != expectedType)
        {
            Debug.LogError(
                $"기본 장비 데이터를 찾을 수 없습니다: {fallbackItemId}"
            );
            return false;
        }

        playerProgress.EnsureOwnedItem(fallbackItemId);
        playerProgress.TryEquipItem(fallbackItem);
        return true;
    }

    private void SelectTab(ItemType itemType)
    {
        currentTab = itemType;
        RefreshShopUI("");
    }

    private void RefreshShopUI(string message)
    {
        RefreshTabs();

        if (goldText != null)
            goldText.text = $"GOLD {playerProgress.Gold:N0}";

        if (messageText != null)
            messageText.text = message;

        ClearItemList();

        if (itemListContent == null || itemService == null)
            return;

        if (currentTab != ItemType.Weapon)
        {
            CreateText(
                itemListContent,
                "EmptyText",
                "준비 중",
                48,
                FontStyles.Bold,
                new Vector2(0.5f, 0.72f),
                new Vector2(760f, 100f),
                ivoryColor,
                TextAlignmentOptions.Center
            );
            itemListContent.sizeDelta = new Vector2(
                itemListContent.sizeDelta.x,
                780f
            );
            return;
        }

        List<ItemData> items = itemService.GetItemsByType(ItemType.Weapon);
        items = items.FindAll(
            item => item.id == WoodSwordId || item.id == IronSwordId
        );

        itemListContent.sizeDelta = new Vector2(
            itemListContent.sizeDelta.x,
            Mathf.Max(
                780f,
                FirstCardTopPadding +
                    (items.Count * CardHeight) +
                    (Mathf.Max(0, items.Count - 1) * CardGap) +
                    40f
            )
        );

        for (int i = 0; i < items.Count; i++)
            CreateItemCard(items[i], i);
    }

    private void RefreshTabs()
    {
        SetTabState(weaponTabButton, currentTab == ItemType.Weapon);
        SetTabState(armorTabButton, currentTab == ItemType.Armor);
        SetTabState(accessoryTabButton, currentTab == ItemType.Unknown);
    }

    private void SetTabState(Button button, bool isActive)
    {
        if (button == null)
            return;

        Image image = button.transform.Find("Background")
            ?.GetComponent<Image>();
        if (image != null)
        {
            Sprite sprite = isActive ? tabActiveSprite : tabInactiveSprite;
            if (sprite != null)
                image.sprite = sprite;

            image.color = Color.white;
            image.preserveAspect = true;
        }

        TMP_Text label = button.transform.Find("Label")
            ?.GetComponent<TMP_Text>();
        if (label != null)
        {
            label.color = tabTextColor;
            label.fontSize = 34;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
            label.alpha = 1f;
            label.rectTransform.SetAsLastSibling();
        }
    }

    private void ClearItemList()
    {
        if (itemListContent == null)
            return;

        for (int i = itemListContent.childCount - 1; i >= 0; i--)
            Destroy(itemListContent.GetChild(i).gameObject);
    }

    private void CreateItemCard(ItemData item, int index)
    {
        bool isOwned = playerProgress.OwnsItem(item.id);
        bool isEquipped = item.id == playerProgress.EquippedWeaponId;
        Sprite cardSprite = isEquipped
            ? itemCardEquippedSprite
            : itemCardSprite;

        GameObject card = CreatePanel(
            itemListContent,
            item.id == WoodSwordId ? "WoodenSwordCard" : "IronSwordCard",
            Color.white,
            new Vector2(0.5f, 1f),
            new Vector2(CardWidth, CardHeight)
        );

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = new Vector2(
            0f,
            -FirstCardTopPadding - (index * (CardHeight + CardGap))
        );

        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = cardSprite;
        cardImage.color = Color.white;
        cardImage.preserveAspect = false;
        card.SetActive(true);

        Sprite itemSprite = LoadRuntimeSprite(item.spritePath);
        if (itemSprite != null)
        {
            Image icon = CreateCardImage(
                card.transform,
                "Icon",
                itemSprite,
                IconPos,
                IconSize,
                true
            );
            icon.color = Color.white;
        }

        CreateCardText(
            card.transform,
            "NameText",
            item.name,
            44,
            FontStyles.Bold,
            NameTextPos,
            NameTextSize,
            goldColor,
            TextAlignmentOptions.Left
        );

        CreateCardText(
            card.transform,
            "StatText",
            $"ATK +{item.attackBonus}",
            30,
            FontStyles.Bold,
            AttackTextPos,
            AttackTextSize,
            greenTextColor,
            TextAlignmentOptions.Center
        );

        CreateCardText(
            card.transform,
            "DescriptionText",
            item.description,
            25,
            FontStyles.Normal,
            DescriptionTextPos,
            DescriptionTextSize,
            softTextColor,
            TextAlignmentOptions.Left
        );

        string price = $"{item.price:N0} GOLD";
        CreateCardText(
            card.transform,
            "PriceText",
            price,
            30,
            FontStyles.Bold,
            PriceTextPos,
            PriceTextSize,
            goldColor,
            TextAlignmentOptions.Center
        );

        string buttonLabel = isEquipped
            ? "장착중"
            : isOwned
                ? "장착"
                : "구매";
        Button stateButton = CreateStateButton(
            card.transform,
            "StateButton",
            buttonLabel,
            StateButtonPos,
            StateButtonSize
        );

        if (isEquipped)
        {
            stateButton.interactable = false;
        }
        else if (isOwned)
        {
            stateButton.onClick.AddListener(() => TryEquipItem(item.id));
        }
        else
        {
            stateButton.onClick.AddListener(() => TryBuyItem(item.id));
        }
    }

    private void TryBuyItem(string itemId)
    {
        ItemData item = itemService.GetItem(itemId);

        if (item == null)
        {
            RefreshShopUI("아이템을 찾을 수 없습니다.");
            return;
        }

        if (playerProgress.OwnsItem(item.id))
        {
            RefreshShopUI("이미 보유한 아이템입니다.");
            return;
        }

        if (!playerProgress.TrySpendGold(item.price))
        {
            RefreshShopUI("Gold가 부족합니다.");
            return;
        }

        playerProgress.AddOwnedItem(item.id);
        SaveProgress();
        RefreshShopUI($"{item.name} 구매 완료!");
    }

    private void TryEquipItem(string itemId)
    {
        ItemData item = itemService.GetItem(itemId);

        if (item == null)
        {
            RefreshShopUI("아이템을 찾을 수 없습니다.");
            return;
        }

        if (!playerProgress.OwnsItem(item.id))
        {
            RefreshShopUI("보유하지 않은 아이템은 장착할 수 없습니다.");
            return;
        }

        if (!playerProgress.TryEquipItem(item))
        {
            RefreshShopUI("장착할 수 없는 아이템입니다.");
            return;
        }

        SaveProgress();
        RefreshShopUI($"{item.name} 장착 완료!");
    }

    private void ReturnToBattle()
    {
        if (isLeaving)
            return;

        isLeaving = true;
        SaveProgress();
        SceneManager.LoadScene(BattleSceneName);
    }

    private Sprite LoadRuntimeSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

#if UNITY_EDITOR
        Sprite editorSprite =
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (editorSprite != null)
            return editorSprite;
#endif

        string resourcesPath = ToResourcesSpritePath(assetPath);

        if (string.IsNullOrEmpty(resourcesPath))
            return null;

        return Resources.Load<Sprite>(resourcesPath);
    }

    private string ToResourcesSpritePath(string assetPath)
    {
        string normalizedPath = assetPath.Replace("\\", "/");

        if (normalizedPath.StartsWith("Assets/Resources/"))
        {
            normalizedPath = normalizedPath.Substring(
                "Assets/Resources/".Length
            );
        }
        else if (normalizedPath.StartsWith("Assets/Art/"))
        {
            normalizedPath = "Art/" + normalizedPath.Substring(
                "Assets/Art/".Length
            );
        }
        else
        {
            return null;
        }

        string extension = Path.GetExtension(normalizedPath);

        if (!string.IsNullOrEmpty(extension))
            normalizedPath = normalizedPath.Substring(
                0,
                normalizedPath.Length - extension.Length
            );

        return normalizedPath;
    }

    private GameObject CreatePanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject obj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private Image CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchor,
        Vector2 size,
        bool preserveAspect
    )
    {
        GameObject obj = CreatePanel(parent, name, Color.white, anchor, size);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private Image CreateCardImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size,
        bool preserveAspect
    )
    {
        Image image = CreateImage(
            parent,
            name,
            sprite,
            new Vector2(0.5f, 0.5f),
            size,
            preserveAspect
        );
        image.rectTransform.anchoredPosition = anchoredPosition;
        return image;
    }

    private TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        FontStyles style,
        Vector2 anchor,
        Vector2 size,
        Color color,
        TextAlignmentOptions alignment
    )
    {
        GameObject obj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.font = koreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.alpha = 1f;
        return text;
    }

    private TMP_Text CreateCardText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        FontStyles style,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color,
        TextAlignmentOptions alignment
    )
    {
        TMP_Text text = CreateText(
            parent,
            name,
            value,
            fontSize,
            style,
            new Vector2(0.5f, 0.5f),
            size,
            color,
            alignment
        );
        text.rectTransform.anchoredPosition = anchoredPosition;
        return text;
    }

    private Button CreateStateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Button button = obj.AddComponent<Button>();

        Image background = CreateImage(
            obj.transform,
            "Background",
            buyButtonSprite,
            new Vector2(0.5f, 0.5f),
            size,
            true
        );
        StretchToParent(background.rectTransform);
        background.raycastTarget = true;
        button.targetGraphic = background;

        TMP_Text labelText = CreateText(
            obj.transform,
            "Label",
            label,
            30,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            size,
            buttonTextColor,
            TextAlignmentOptions.Center
        );
        StretchToParent(labelText.rectTransform);
        labelText.rectTransform.anchoredPosition = new Vector2(0f, 2f);
        labelText.raycastTarget = false;
        labelText.alpha = 1f;
        labelText.enableAutoSizing = false;
        labelText.rectTransform.SetAsLastSibling();

        return button;
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
}
