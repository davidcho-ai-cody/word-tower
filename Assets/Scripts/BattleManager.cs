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
    private TMP_InputField wordInput;
    private Button attackButton;

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

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);

        if (wordInput != null)
            wordInput.onSubmit.AddListener(_ => OnAttackButtonClicked());
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
            chainHintText.text = $"❌ '{requiredChar}'로 시작해야 합니다!";
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

        chainHintText.text = $"⚔ 공격 성공! 슬라임에게 {playerAttack} 데미지!";
        UpdateUI();

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

        chainHintText.text =
            $"💥 슬라임의 공격! {slimeAttack} 데미지\n'{currentWord[currentWord.Length - 1]}'로 시작하는 단어를 입력하세요!";

        UpdateUI();

        if (playerHp <= 0)
        {
            LoseBattle();
            yield break;
        }

        wordInput.interactable = true;
        attackButton.interactable = true;

        wordInput.ActivateInputField();
    }

    string GetSlimeWord(char startChar)
    {
        // 임시 테스트용 단어
        // 나중에 words.db / JSON으로 교체 예정

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
            $"🎉 슬라임 처치!\nEXP +{slimeExpReward}   GOLD +{slimeGoldReward}";

        wordInput.interactable = false;
        attackButton.interactable = false;

        UpdateUI();
    }

    void LoseBattle()
    {
        battleEnded = true;

        chainHintText.text = "💀 패배했습니다.";

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
}