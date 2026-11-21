using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public class RewardDisplay : MonoBehaviour
{
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private Color rectangleColor = new Color(0.15f, 0.2f, 0.3f, 0.95f);
    [SerializeField] private Font textFont;
    private const int RewardCount = 3;
    private readonly Text[] rewardLabels = new Text[RewardCount];
    private readonly Equipment[] currentRewards = new Equipment[RewardCount];
    private readonly RawImage[] rewardIcons = new RawImage[RewardCount];
    private static readonly Color[] rarityColors =
    {
        new Color(0.75f, 0.8f, 0.84f),
        new Color(0.3f, 0.85f, 0.5f),
        new Color(0.3f, 0.65f, 1f),
        new Color(0.75f, 0.4f, 1f),
        new Color(1f, 0.72f, 0.25f)
    };
    private readonly RuneRewardCard[] cards = new RuneRewardCard[RewardCount];
    private readonly Text[] titles = new Text[RewardCount];
    private readonly Image[] accents = new Image[RewardCount];
    private CanvasGroup panelGroup;
    private Coroutine selection;
    private Controller rewardController;
    private GameObject panel;

    private void Awake()
    {
        if (rewardManager == null)
            rewardManager = GetComponent<RewardManager>();

        if (rewardManager == null)
        {
            Debug.LogError("RewardDisplay requires a RewardManager reference.", this);
            enabled = false;
            return;
        }

        panel = new GameObject("Reward Choices", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.04f, 0.88f);
        panelGroup = panel.GetComponent<CanvasGroup>();

        if (textFont == null)
            textFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateLabel(panel.transform, "Choose a Rune", new Vector2(0.1f, 0.84f),
            new Vector2(0.9f, 0.94f), 34, TextAnchor.MiddleCenter);
        CreateLabel(panel.transform, "Select one blessing", new Vector2(0.1f, 0.79f),
            new Vector2(0.9f, 0.84f), 18, TextAnchor.MiddleCenter);

        Texture2D runeAtlas = Resources.Load<Texture2D>("UI/RuneRarities");

        const float rectangleWidth = 0.24f;
        const float gap = 0.04f;
        for (int i = 0; i < RewardCount; i++)
        {
            GameObject rectangle = new GameObject($"Reward {i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            rectangle.transform.SetParent(panel.transform, false);
            RectTransform rect = (RectTransform)rectangle.transform;
            float left = 0.1f + i * (rectangleWidth + gap);
            rect.anchorMin = new Vector2(left, 0.14f);
            rect.anchorMax = new Vector2(left + rectangleWidth, 0.76f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rectangle.GetComponent<Image>();
            image.color = Color.Lerp(rectangleColor, new Color(0.035f, 0.045f, 0.065f, 1f), 0.8f);
            image.raycastTarget = true;

            Button button = rectangle.GetComponent<Button>();
            button.targetGraphic = image;
            cards[i] = rectangle.AddComponent<RuneRewardCard>();
            titles[i] = CreateLabel(rectangle.transform, "", new Vector2(0.06f, 0.43f),
                new Vector2(0.94f, 0.53f), 24, TextAnchor.MiddleCenter);
            Text footer = CreateLabel(rectangle.transform, "SELECT RUNE", new Vector2(0.06f, 0.02f),
                new Vector2(0.94f, 0.09f), 14, TextAnchor.MiddleCenter);
            footer.color = new Color(0.65f, 0.7f, 0.77f);
            GameObject accent = new GameObject("Rarity accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(rectangle.transform, false);
            RectTransform accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0f, 0.99f);
            accentRect.anchorMax = Vector2.one;
            accentRect.offsetMin = accentRect.offsetMax = Vector2.zero;
            accents[i] = accent.GetComponent<Image>();
            accents[i].raycastTarget = false;
            int rewardIndex = i;
            button.onClick.AddListener(() => SelectReward(rewardIndex));

            GameObject icon = new GameObject("Rune", typeof(RectTransform), typeof(RawImage));
            icon.transform.SetParent(rectangle.transform, false);
            RectTransform iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = new Vector2(0.15f, 0.55f);
            iconRect.anchorMax = new Vector2(0.85f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            AspectRatioFitter aspect = icon.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            aspect.aspectRatio = runeAtlas != null ? runeAtlas.width / (5f * runeAtlas.height) : 0.6f;
            RawImage runeImage = icon.GetComponent<RawImage>();
            runeImage.texture = runeAtlas;
            runeImage.raycastTarget = false;
            rewardIcons[i] = runeImage;

            GameObject label = new GameObject("Details", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(rectangle.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(0f, 0.1f);
            labelRect.anchorMax = new Vector2(1f, 0.42f);
            labelRect.offsetMin = new Vector2(16f, 16f);
            labelRect.offsetMax = new Vector2(-16f, -16f);

            Text text = label.GetComponent<Text>();
            text.font = textFont;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 20;
            text.raycastTarget = false;
            rewardLabels[i] = text;
        }

        panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (rewardManager != null) rewardManager.OnRewardsGenerated += ShowRewards;
    }

    private void OnDisable()
    {
        if (rewardManager != null)
            rewardManager.OnRewardsGenerated -= ShowRewards;
        HideRewards();
    }

    private void ShowRewards(Controller controller, IReadOnlyList<Equipment> rewards)
    {
        HideRewards();
        rewardController = controller;
        panelGroup.interactable = true;
        for (int i = 0; i < RewardCount; i++)
        {
            currentRewards[i] = rewards[i];
            rewardLabels[i].text = GetRewardText(rewards[i]);
            int rarity = (int)rewards[i].Rarity;
            rewardLabels[i].color = new Color(0.87f, 0.9f, 0.94f);
            titles[i].text = rewards[i].Name;
            titles[i].color = accents[i].color = rarityColors[rarity];
            cards[i].Reveal(i * 0.07f);
            rewardIcons[i].uvRect = new Rect(rarity / 5f, 0f, 1f / 5f, 1f);
        }
        panel.SetActive(true);
    }

    private void SelectReward(int index)
    {
        if (selection != null || rewardController == null || currentRewards[index] == null)
            return;

        panelGroup.interactable = false;
        cards[index].Choose();
        selection = StartCoroutine(ConfirmSelection(index));
    }

    private IEnumerator ConfirmSelection(int index)
    {
        yield return new WaitForSecondsRealtime(0.18f);
        selection = null;
        Controller controller = rewardController;
        Equipment equipment = currentRewards[index];
        HideRewards();
        rewardManager.SelectReward(controller, equipment);
    }

    private void HideRewards()
    {
        if (selection != null) StopCoroutine(selection);
        selection = null;
        rewardController = null;
        for (int i = 0; i < RewardCount; i++)
            currentRewards[i] = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private Text CreateLabel(Transform parent, string content, Vector2 minimum, Vector2 maximum,
        int size, TextAnchor alignment)
    {
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Text text = label.GetComponent<Text>();
        text.text = content;
        text.font = textFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
        return text;
    }

    private static string GetRewardText(Equipment equipment)
    {
        StringBuilder text = new StringBuilder(256);

        int headerLength = text.Length;
        Stats stats = equipment.Stats;
        AppendStat(text, "Max health", stats.MaxHealth);
        AppendStat(text, "Damage", stats.BaseDamage);
        AppendStat(text, "Attack speed", stats.AttackSpeed);
        AppendStat(text, "Defense", stats.Defense);
        AppendStat(text, "Critical chance", stats.CriticalChance, "%");
        AppendStat(text, "Multishot chance", stats.MultishotChance, "%");
        AppendStat(text, "Critical damage", stats.CriticalDamage, "x");
        AppendStat(text, "Max health", stats.MaxHealthPerc * 100f, "%");
        AppendStat(text, "Damage", stats.BaseDamagePerc * 100f, "%");
        AppendStat(text, "Attack speed", stats.AttackSpeedPerc * 100f, "%");
        AppendStat(text, "Defense", stats.DefensePerc * 100f, "%");
        if (text.Length == headerLength)
            text.Append("No bonus stats");

        return text.ToString();
    }

    private static void AppendStat(StringBuilder text, string label, float value, string unit = "")
    {
        if (value == 0f) return;
        text.Append(label).Append(' ')
            .Append(value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture))
            .Append(unit).AppendLine();
    }

    private void OnDestroy()
    {
        if (panel != null)
            Destroy(panel);
    }
}
