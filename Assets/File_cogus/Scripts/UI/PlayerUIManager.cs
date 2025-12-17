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

    private void Update()
    {
        PlayerController player = PlayerController.Instance;
        PlayerSetting setting = player.Setting;
        PlayerVitals vitals = player.Vitals;

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
    }
}