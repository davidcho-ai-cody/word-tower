using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Player")]
    public int playerMaxHp = 100;
    public int playerHp = 100;
    public int playerAttack = 20;

    [Header("Slime")]
    public int slimeMaxHp = 100;
    public int slimeHp = 100;
    public int slimeAttack = 10;

    [Header("Reward")]
    public int exp = 0;
    public int gold = 0;
    public int slimeExpReward = 20;
    public int slimeGoldReward = 10;

    private TMP_Text playerHpText;
    private TMP_Text slimeHpText;
    private TMP_Text enemyWordText;
    private TMP_Text chainHintText;
    private TMP_Text expText;
    private TMP_Text goldText;
    private TMP_FontAsset koreanFont;

    private TMP_InputField wordInput;
    private Button attackButton;

    private Image playerHpFill;
    private Image slimeHpFill;
    private float playerHpFullWidth;
    private float slimeHpFullWidth;

    private RectTransform playerVisual;
    private RectTransform slimeVisual;

    private string currentWord = "사과";
    private bool battleEnded = false;

    void Start()
    {
        FindUI();
        SetupBattle();
    }

    void FindUI()
    {
        playerHpText = GameObject.Find("PlayerHP/HPText")?.GetComponent<TMP_Text>();
        slimeHpText = GameObject.Find("MonsterHP/HPText")?.GetComponent<TMP_Text>();

        enemyWordText = GameObject.Find("EnemyWord")?.GetComponent<TMP_Text>();
        chainHintText = GameObject.Find("ChainHint")?.GetComponent<TMP_Text>();

        expText = GameObject.Find("ExpText")?.GetComponent<TMP_Text>();
        goldText = GameObject.Find("GoldText")?.GetComponent<TMP_Text>();

        wordInput = GameObject.Find("WordInput")?.GetComponent<TMP_InputField>();
        attackButton = GameObject.Find("AttackButton")?.GetComponent<Button>();

        playerHpFill = GameObject.Find("PlayerHP/Fill")?.GetComponent<Image>();
        slimeHpFill = GameObject.Find("MonsterHP/Fill")?.GetComponent<Image>();

        playerVisual = GameObject.Find("PlayerPlaceholder")?.GetComponent<RectTransform>();
        slimeVisual = GameObject.Find("SlimePlaceholder")?.GetComponent<RectTransform>();

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);

        if (wordInput != null)
            wordInput.onSubmit.AddListener(_ => OnAttackButtonClicked());

        if (playerHpFill != null)
            playerHpFullWidth = playerHpFill.rectTransform.sizeDelta.x;

        if (slimeHpFill != null)
            slimeHpFullWidth = slimeHpFill.rectTransform.sizeDelta.x;

        koreanFont = Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-VF SDF");

        if (chainHintText != null)
            koreanFont = chainHintText.font;
    }

    void SetupBattle()
    {
        playerHp = playerMaxHp;
        slimeHp = slimeMaxHp;

        currentWord = "사과";
        battleEnded = false;

        UpdateUI();

        if (wordInput != null)
        {
            wordInput.text = "";
            wordInput.ActivateInputField();
        }
    }

    public void OnAttackButtonClicked()
    {
        if (battleEnded || wordInput == null)
            return;

        string inputWord = wordInput.text.Trim();

        if (string.IsNullOrEmpty(inputWord))
            return;

        char requiredChar = currentWord[currentWord.Length - 1];

        if (inputWord[0] != requiredChar)
        {
            chainHintText.text = $"'{requiredChar}'로 시작해야 합니다!";
            wordInput.text = "";
            wordInput.ActivateInputField();
            return;
        }

        PlayerAttack(inputWord);
    }

    void PlayerAttack(string inputWord)
    {
        slimeHp -= playerAttack;
        slimeHp = Mathf.Max(slimeHp, 0);

        currentWord = inputWord;

        chainHintText.text = $"공격 성공! 슬라임에게 {playerAttack} 데미지!";

        UpdateUI();

        // 슬라임 머리 위에 데미지 숫자 표시
        ShowDamageText(slimeVisual, playerAttack);

        // 슬라임 피격 흔들림 효과
        if (slimeVisual != null)
            StartCoroutine(HitEffect(slimeVisual));

        wordInput.text = "";
        wordInput.interactable = false;
        attackButton.interactable = false;

        if (slimeHp <= 0)
        {
            WinBattle();
            return;
        }

        StartCoroutine(SlimeTurn());
    }

    IEnumerator SlimeTurn()
    {
        yield return new WaitForSeconds(1f);

        string slimeWord = GetSlimeWord(currentWord[currentWord.Length - 1]);

        enemyWordText.text = slimeWord;

        playerHp -= slimeAttack;
        playerHp = Mathf.Max(playerHp, 0);

        currentWord = slimeWord;

        UpdateUI();

        chainHintText.text =
            $"슬라임의 공격! {slimeAttack} 데미지\n'{currentWord[currentWord.Length - 1]}'로 시작하는 단어를 입력하세요!";

        // 플레이어 머리 위에 데미지 숫자 표시
        ShowDamageText(playerVisual, slimeAttack);

        // 플레이어 피격 흔들림 효과
        if (playerVisual != null)
            StartCoroutine(HitEffect(playerVisual));

        if (playerHp <= 0)
        {
            LoseBattle();
            yield break;
        }

        wordInput.interactable = true;
        attackButton.interactable = true;

        wordInput.ActivateInputField();
    }

    IEnumerator HitEffect(RectTransform target)
    {
        Vector2 original = target.anchoredPosition;

        for (int i = 0; i < 6; i++)
        {
            float offset = (i % 2 == 0) ? 12f : -12f;
            target.anchoredPosition = original + new Vector2(offset, 0);
            yield return new WaitForSeconds(0.04f);
        }

        target.anchoredPosition = original;
    }

    string GetSlimeWord(char startChar)
    {
        switch (startChar)
        {
            case '자': return "자동차";
            case '차': return "차표";
            case '표': return "표범";
            case '범': return "범인";
            case '인': return "인형";
            case '형': return "형광등";
            case '등': return "등산";
            case '산': return "산책";
            case '책': return "책상";
            case '상': return "상자";
            case '과': return "과자";
            default: return "사과";
        }
    }

    void WinBattle()
    {
        battleEnded = true;

        exp += slimeExpReward;
        gold += slimeGoldReward;

        chainHintText.text =
            $"슬라임 처치!\nEXP +{slimeExpReward}   GOLD +{slimeGoldReward}";

        wordInput.interactable = false;
        attackButton.interactable = false;

        UpdateUI();
    }

    void LoseBattle()
    {
        battleEnded = true;

        chainHintText.text = "패배했습니다.";

        wordInput.interactable = false;
        attackButton.interactable = false;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerHpText != null)
            playerHpText.text = $"HP {playerHp} / {playerMaxHp}";

        if (slimeHpText != null)
            slimeHpText.text = $"HP {slimeHp} / {slimeMaxHp}";

        if (playerHpFill != null)
        {
            float ratio = (float)playerHp / playerMaxHp;

            Vector2 size = playerHpFill.rectTransform.sizeDelta;
            size.x = playerHpFullWidth * ratio;
            playerHpFill.rectTransform.sizeDelta = size;
        }

        if (slimeHpFill != null)
        {
            float ratio = (float)slimeHp / slimeMaxHp;

            Vector2 size = slimeHpFill.rectTransform.sizeDelta;
            size.x = slimeHpFullWidth * ratio;
            slimeHpFill.rectTransform.sizeDelta = size;
        }

        if (enemyWordText != null)
            enemyWordText.text = currentWord;

        if (chainHintText != null && !battleEnded)
        {
            char requiredChar = currentWord[currentWord.Length - 1];
            chainHintText.text = $"『 {requiredChar} 』로 시작하는 단어를 입력하세요!";
        }

        if (expText != null)
            expText.text = $"EXP {exp} / 100";

        if (goldText != null)
            goldText.text = $"GOLD {gold}";
    }

    void ShowDamageText(RectTransform target, int damage)
    {
        if (target == null)
            return;

        GameObject obj = new GameObject(
            "DamageText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(target.parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = target.anchorMin;
        rect.anchorMax = target.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = target.anchoredPosition + new Vector2(0, 180);
        rect.sizeDelta = new Vector2(250, 100);

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();

        text.text = $"-{damage}";
        text.font = koreanFont;
        text.fontSize = 54;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        StartCoroutine(DamageTextAnimation(rect, text));
    }

    IEnumerator DamageTextAnimation(
        RectTransform rect,
        TextMeshProUGUI text
    )
    {
        float duration = 0.7f;
        float elapsed = 0f;

        Vector2 startPosition = rect.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, 100);

        Color startColor = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            rect.anchoredPosition =
                Vector2.Lerp(startPosition, endPosition, t);

            text.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                1f - t
            );

            yield return null;
        }

        Destroy(rect.gameObject);
    }
}