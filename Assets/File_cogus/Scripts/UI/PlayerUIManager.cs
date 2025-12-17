using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class PlayerUIManager : MonoBehaviour
{
    [Header("HP UI")]
    [SerializeField] private TMP_Text hpTextTMP;
    [SerializeField] private Image hpImage;

    [Header("Attack UI")]
    [SerializeField] private GameObject attackRoot;
    [SerializeField] private TMP_Text attackTextTMP;

    [Header("Banana UI")]
    [SerializeField] private GameObject bananaRoot;
    [SerializeField] private Image bananaAttackCoolImage;
    [SerializeField] private Image bananaHealCoolImage;
    [SerializeField] private TMP_Text bananaCountTMP;

    private PlayerController player;
    private PlayerSetting setting;
    private PlayerVitals vitals;

    private void Update()
    {
        player = PlayerController.Instance;
        setting = player.Setting;
        vitals = player.Vitals;

        PlayerSkin skin = player.CurrentSkin;

        int maxHp = setting.GetMaxHealth(skin);
        int curHp = vitals.GetHealth(skin);

        hpTextTMP.text = $"{curHp} / {maxHp}";
        hpImage.fillAmount = (float)curHp / maxHp;

        bool isEgg = skin == PlayerSkin.Egg;

        if (attackRoot.activeSelf != isEgg)
            attackRoot.SetActive(isEgg);

        if (isEgg)
        {
            int atk = setting.baseEggProjectileDamage + setting.extraEggProjectileDamage;
            attackTextTMP.text = atk.ToString();
        }

        //banana

        bool isBanana = skin == PlayerSkin.Banana;

        if (bananaRoot.activeSelf != isBanana)
            bananaRoot.SetActive(isBanana);

        if (isBanana)
        {
            float peelCooldown = player.CurrentBananaPeelCooldown;
            float peelMaxCooldown = setting.bananaPeelCooldown;

            float peelFill = Mathf.Clamp01((peelMaxCooldown - peelCooldown) / peelMaxCooldown);
            bananaAttackCoolImage.fillAmount = peelFill;

            int currentCount = player.ActiveBananaPeelCount;
            int maxCount = setting.maxBananaPeelCount;

            bananaCountTMP.text = $"{maxCount - currentCount}";

            float healCooldown = player.CurrentHealingBananaCooldown;
            float healMaxCooldown = setting.healingBananaCooldown;

            float healFill = Mathf.Clamp01((healMaxCooldown - healCooldown) / healMaxCooldown);
            bananaHealCoolImage.fillAmount = healFill;
        }
    }
}