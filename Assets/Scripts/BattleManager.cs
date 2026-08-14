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

    // =========================
    // 공격 / 타격 연출
    // =========================
    private RectTransform weaponVisual;

    // 슬라임이 맞는 순간 표시할 타격 이펙트
    private RectTransform impactEffect;

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

        // 용사의 무기 레이어
        weaponVisual = GameObject.Find("Weapon")?.GetComponent<RectTransform>();

        // ImpactEffect는 시작 시 비활성화 상태이므로
        // GameObject.Find() 대신 BattleCanvas 하위에서 직접 검색한다.
        Transform battleCanvas = GameObject.Find("BattleCanvas")?.transform;

        if (battleCanvas != null)
        {
            Transform impactTransform =
                battleCanvas.Find("ImpactEffect");

            if (impactTransform != null)
            {
                impactEffect =
                    impactTransform.GetComponent<RectTransform>();
            }
        }

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

    // ========================================
    // 플레이어 공격 시작
    // ========================================
    void PlayerAttack(string inputWord)
    {
        currentWord = inputWord;

        // 공격 중에는 입력 방지
        wordInput.text = "";
        wordInput.interactable = false;
        attackButton.interactable = false;

        // 돌진 + 검 공격 연출 시작
        StartCoroutine(PlayerAttackSequence());
    }

    IEnumerator SlimeTurn()
    {
        yield return new WaitForSeconds(1f);

        string slimeWord = GetSlimeWord(currentWord[currentWord.Length - 1]);

        enemyWordText.text = slimeWord;

        currentWord = slimeWord;

        // 슬라임 공격 연출 시작
        yield return StartCoroutine(SlimeAttackSequence());

        if (playerHp <= 0)
        {
            LoseBattle();
            yield break;
        }

        wordInput.interactable = true;
        attackButton.interactable = true;

        wordInput.ActivateInputField();
    }

    // ========================================
    // 슬라임 공격 연출
    // 1. 플레이어 방향으로 돌진
    // 2. 데미지 적용
    // 3. 플레이어 피격
    // 4. 슬라임 원위치 복귀
    // ========================================
    IEnumerator SlimeAttackSequence()
    {
        if (slimeVisual == null)
            yield break;

        Vector2 originalSlimePos = slimeVisual.anchoredPosition;

        // -------------------------
        // 1. 용사 쪽으로 돌진
        // 슬라임은 오른쪽에 있으므로 왼쪽(-X)으로 이동
        // -------------------------
        Vector2 attackPosition =
            originalSlimePos + new Vector2(-85f, 0f);

        float rushDuration = 0.13f;
        float elapsed = 0f;

        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / rushDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    originalSlimePos,
                    attackPosition,
                    t
                );

            yield return null;
        }

        slimeVisual.anchoredPosition = attackPosition;

        // -------------------------
        // 2. 실제 데미지 적용
        // -------------------------
        playerHp -= slimeAttack;
        playerHp = Mathf.Max(playerHp, 0);

        UpdateUI();

        chainHintText.text =
            $"슬라임의 공격! {slimeAttack} 데미지\n" +
            $"'{currentWord[currentWord.Length - 1]}'로 시작하는 단어를 입력하세요!";

        // 플레이어 머리 위 데미지 숫자
        ShowDamageText(playerVisual, slimeAttack);

        // 플레이어는 왼쪽으로 밀리며 피격
        if (playerVisual != null)
            StartCoroutine(HitEffect(playerVisual));

        yield return new WaitForSeconds(0.1f);

        // -------------------------
        // 3. 슬라임 원위치 복귀
        // -------------------------
        float returnDuration = 0.15f;
        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    attackPosition,
                    originalSlimePos,
                    t
                );

            yield return null;
        }

        slimeVisual.anchoredPosition = originalSlimePos;
    }

    // ========================================
    // 피격 연출
    // 1. 맞는 순간 뒤로 밀림
    // 2. 짧게 흔들림
    // 3. 원래 위치로 복귀
    // ========================================
    IEnumerator HitEffect(RectTransform target)
    {
        if (target == null)
            yield break;

        // 피격 전 원래 위치 저장
        Vector2 originalPosition = target.anchoredPosition;

        // ========================================
        // 1. 뒤로 밀려나는 연출
        // ========================================

        // ========================================
        // 피격 방향 결정
        // 슬라임은 오른쪽으로, 용사는 왼쪽으로 밀림
        // ========================================

        float knockbackDirection = 1f;

        // 플레이어가 맞았으면 왼쪽 방향
        if (target == playerVisual)
        {
            knockbackDirection = -1f;
        }

        // 슬라임은 기본값 +1 → 오른쪽 방향

        Vector2 knockbackPosition =
            originalPosition + new Vector2(35f * knockbackDirection, 0f);

        float knockbackDuration = 0.08f;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / knockbackDuration;

            target.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    knockbackPosition,
                    t
                );

            yield return null;
        }

        target.anchoredPosition = knockbackPosition;


        // ========================================
        // 2. 피격 흔들림
        // ========================================

        for (int i = 0; i < 4; i++)
        {
            float offset =
                (i % 2 == 0)
                    ? 10f * knockbackDirection
                    : -10f * knockbackDirection;

            target.anchoredPosition =
                knockbackPosition + new Vector2(offset, 0f);

            yield return new WaitForSeconds(0.035f);
        }


        // ========================================
        // 3. 원래 위치로 복귀
        // ========================================

        float returnDuration = 0.12f;
        elapsed = 0f;

        Vector2 currentPosition = target.anchoredPosition;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            target.anchoredPosition =
                Vector2.Lerp(
                    currentPosition,
                    originalPosition,
                    t
                );

            yield return null;
        }

        // 오차 방지를 위해 정확한 원위치 지정
        target.anchoredPosition = originalPosition;
    }

    // ========================================
    // 플레이어 공격 연출
    // 1. 앞으로 돌진
    // 2. 검 휘두르기
    // 3. 데미지 적용
    // 4. 원위치 복귀
    // ========================================
    IEnumerator PlayerAttackSequence()
    {
        if (playerVisual == null)
            yield break;

        // 현재 용사의 원래 위치 저장
        Vector2 originalPlayerPos = playerVisual.anchoredPosition;

        // 현재 나무검 각도 저장
        Quaternion originalWeaponRotation =
            weaponVisual != null
                ? weaponVisual.localRotation
                : Quaternion.identity;


        // ========================================
        // 1. 슬라임 방향으로 빠르게 돌진
        // ========================================

        Vector2 attackPosition =
            originalPlayerPos + new Vector2(110f, 0f);

        float rushDuration = 0.12f;
        float elapsed = 0f;

        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / rushDuration;

            playerVisual.anchoredPosition =
                Vector2.Lerp(
                    originalPlayerPos,
                    attackPosition,
                    t
                );

            yield return null;
        }

        playerVisual.anchoredPosition = attackPosition;


        // ========================================
        // 2. 나무검 휘두르기
        // ========================================

        if (weaponVisual != null)
        {
            float swingDuration = 0.13f;
            elapsed = 0f;

            Quaternion startRotation =
                Quaternion.Euler(0f, 0f, 20f);

            Quaternion endRotation =
                Quaternion.Euler(0f, 0f, -55f);

            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;

                float t = elapsed / swingDuration;

                weaponVisual.localRotation =
                    Quaternion.Lerp(
                        startRotation,
                        endRotation,
                        t
                    );

                yield return null;
            }
        }


        // ========================================
        // 3. 검이 닿는 순간 실제 데미지 적용
        // ========================================

        // 검이 닿는 순간 타격 이펙트!
        if (impactEffect != null)
            StartCoroutine(ImpactEffect());

        // 실제 데미지
        slimeHp -= playerAttack;
        slimeHp = Mathf.Max(slimeHp, 0);

        UpdateUI();

        chainHintText.text =
            $"공격 성공! 슬라임에게 {playerAttack} 데미지!";

        // -20 데미지 숫자
        ShowDamageText(slimeVisual, playerAttack);

        // 슬라임 피격 흔들림
        if (slimeVisual != null)
            StartCoroutine(HitEffect(slimeVisual));


        // ========================================
        // 4. 검 원래 각도로 복구
        // ========================================

        if (weaponVisual != null)
            weaponVisual.localRotation = originalWeaponRotation;

        yield return new WaitForSeconds(0.08f);


        // ========================================
        // 5. 용사 원위치 복귀
        // ========================================

        float returnDuration = 0.15f;
        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            playerVisual.anchoredPosition =
                Vector2.Lerp(
                    attackPosition,
                    originalPlayerPos,
                    t
                );

            yield return null;
        }

        playerVisual.anchoredPosition = originalPlayerPos;


        // ========================================
        // 6. 슬라임 사망 체크
        // ========================================

        if (slimeHp <= 0)
        {
            // 슬라임 사망 연출이 끝난 후 승리 처리
            yield return StartCoroutine(SlimeDeathSequence());

            WinBattle();
            yield break;
        }


        // ========================================
        // 7. 슬라임 반격 시작
        // ========================================

        StartCoroutine(SlimeTurn());
    }

    // ========================================
    // 슬라임 사망 연출
    // 1. 살짝 위로 튀어오름
    // 2. 회전하면서 작아짐
    // 3. 완전히 사라짐
    // ========================================
    IEnumerator SlimeDeathSequence()
    {
        if (slimeVisual == null)
            yield break;

        Vector2 originalPosition = slimeVisual.anchoredPosition;
        Vector3 originalScale = slimeVisual.localScale;
        Quaternion originalRotation = slimeVisual.localRotation;


        // ========================================
        // 1. 죽기 직전 살짝 위로 튀어오름
        // ========================================

        Vector2 jumpPosition =
            originalPosition + new Vector2(0f, 35f);

        float jumpDuration = 0.12f;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / jumpDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    jumpPosition,
                    t
                );

            yield return null;
        }


        // ========================================
        // 2. 회전하면서 작아짐
        // ========================================

        float disappearDuration = 0.30f;
        elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / disappearDuration;

            // 점점 작아짐
            slimeVisual.localScale =
                Vector3.Lerp(
                    originalScale,
                    Vector3.zero,
                    t
                );

            // 오른쪽으로 빙글 회전
            slimeVisual.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(0f, -180f, t)
                );

            // 살짝 아래로 떨어지는 느낌
            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    jumpPosition,
                    jumpPosition + new Vector2(20f, -40f),
                    t
                );

            yield return null;
        }


        // ========================================
        // 3. 완전히 숨김
        // ========================================

        slimeVisual.localScale = Vector3.zero;
    }

    // ========================================
    // 기본 공격 타격 이펙트
    // 검이 맞는 순간 크게 나타났다 사라지는 연출
    // ========================================
    IEnumerator ImpactEffect()
    {
        if (impactEffect == null)
            yield break;

        // 공격 순간 활성화
        impactEffect.gameObject.SetActive(true);

        // 기존보다 크게 시작해서 눈에 잘 보이도록 설정
        Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);
        Vector3 endScale   = new Vector3(1.5f, 1.5f, 1f);

        impactEffect.localScale = startScale;

        // 기존 0.10초 → 0.18초로 조금 길게
        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            impactEffect.localScale =
                Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        // 타격 모양을 잠깐 유지
        yield return new WaitForSeconds(0.08f);

        // 다시 숨김
        impactEffect.gameObject.SetActive(false);

        // 다음 공격을 위해 초기화
        impactEffect.localScale = Vector3.one;
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