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

        panel = new GameObject("Reward Choices", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        if (textFont == null)
            textFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        const float rectangleWidth = 0.24f;
        const float gap = 0.04f;
        for (int i = 0; i < RewardCount; i++)
        {
            GameObject rectangle = new GameObject($"Reward {i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            rectangle.transform.SetParent(panel.transform, false);
            RectTransform rect = (RectTransform)rectangle.transform;
            float left = 0.1f + i * (rectangleWidth + gap);
            rect.anchorMin = new Vector2(left, 0.3f);
            rect.anchorMax = new Vector2(left + rectangleWidth, 0.7f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rectangle.GetComponent<Image>();
            image.color = rectangleColor;
            image.raycastTarget = true;

            Button button = rectangle.GetComponent<Button>();
            button.targetGraphic = image;
            int rewardIndex = i;
            button.onClick.AddListener(() => SelectReward(rewardIndex));

            GameObject label = new GameObject("Details", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(rectangle.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
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
            text.resizeTextMaxSize = 28;
            text.raycastTarget = false;
            rewardLabels[i] = text;
        }

        panel.SetActive(false);
    }

    private void OnEnable()
    {
        rewardManager.OnRewardsGenerated += ShowRewards;
    }

    private void OnDisable()
    {
        if (rewardManager != null)
            rewardManager.OnRewardsGenerated -= ShowRewards;
        HideRewards();
    }

    private void ShowRewards(Controller controller, IReadOnlyList<Equipment> rewards)
    {
        rewardController = controller;
        for (int i = 0; i < RewardCount; i++)
        {
            currentRewards[i] = rewards[i];
            rewardLabels[i].text = GetRewardText(rewards[i]);
        }
        panel.SetActive(true);
    }

    private void SelectReward(int index)
    {
        if (rewardController == null || currentRewards[index] == null)
            return;

        Controller controller = rewardController;
        Equipment equipment = currentRewards[index];
        HideRewards();
        rewardManager.SelectReward(controller, equipment);
    }

    private void HideRewards()
    {
        rewardController = null;
        for (int i = 0; i < RewardCount; i++)
            currentRewards[i] = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private static string GetRewardText(Equipment equipment)
    {
        StringBuilder text = new StringBuilder(256);
        text.AppendLine(equipment.Name);
        text.Append(equipment.Rarity).Append(" / ").AppendLine(equipment.Type.ToString());
        text.AppendLine();

        int headerLength = text.Length;
        Stats stats = equipment.Stats;
        AppendStat(text, "Max health", stats.MaxHealth);
        AppendStat(text, "Damage", stats.BaseDamage);
        AppendStat(text, "Attack speed", stats.AttackSpeed);
        AppendStat(text, "Defense", stats.Defense);
        AppendStat(text, "Critical chance", stats.CriticalChance, "%");
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
