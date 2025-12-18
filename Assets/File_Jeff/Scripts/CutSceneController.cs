using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class CutSceneController : MonoBehaviour
{
    [Header("CutScene 1 Settings")]
    [SerializeField] private CanvasGroup cutScene1Group;
    [SerializeField] private TMP_Text[] cutScene1Texts;
    [SerializeField] private float charTypingSpeed = 0.05f;
    [SerializeField] private float textInterval = 1.5f;

    [Header("CutScene 2 Settings")]
    [SerializeField] private CanvasGroup cutScene2Group;
    [SerializeField] private float delayBeforeCS2 = 2.0f;

    [Header("CutScene 2-1 Elements")]
    [SerializeField] private CanvasGroup rayObject;         // Ray (한 번 나오면 유지)
    [SerializeField] private CanvasGroup soulObject;        // Soul (나왔다 사라짐)
    [SerializeField] private TMP_Text cs2_1_Text1;          // 2-1의 텍스트

    [Header("CutScene 2-2 Elements")]
    [SerializeField] private CanvasGroup cs2_2_Elements;
    [SerializeField] private CanvasGroup eggObject;
    [SerializeField] private TMP_Text cs2_2_Text1;
    [SerializeField] private TMP_Text cs2_2_Text2;

    [Header("References")]
    [SerializeField] private TitleManager titleManager;

    private void Start()
    {
        SetupInitialState();
        StartCoroutine(PlayCutSceneSequence());
    }

    private void SetupInitialState()
    {
        cutScene1Group.alpha = 0;
        cutScene2Group.alpha = 0;

        rayObject.alpha = 0;
        soulObject.alpha = 0;
        cs2_1_Text1.alpha = 0;

        cs2_2_Elements.alpha = 0;
        eggObject.alpha = 0;

        foreach (var t in cutScene1Texts) t.alpha = 0;
        cs2_2_Text1.alpha = 0;
        cs2_2_Text2.alpha = 0;
    }

    private IEnumerator PlayCutSceneSequence()
    {
        // --- CutScene 1 ---
        yield return cutScene1Group.DOFade(1, 0.5f).WaitForCompletion();

        foreach (var txtTMP in cutScene1Texts)
        {
            string targetText = txtTMP.text;
            txtTMP.text = "";
            txtTMP.alpha = 1;

            yield return txtTMP.DOText(targetText, targetText.Length * charTypingSpeed)
                               .SetEase(Ease.Linear).WaitForCompletion();

            yield return new WaitForSeconds(textInterval);
            yield return txtTMP.DOFade(0, 0.5f).WaitForCompletion();
        }
        yield return cutScene1Group.DOFade(0, 0.5f).WaitForCompletion();

        // --- Delay ---
        yield return new WaitForSeconds(delayBeforeCS2);

        // --- CutScene 2-1 ---
        cutScene2Group.alpha = 1;

        rayObject.DOFade(1, 0.5f); // Ray 등장 (이후 유지)
        soulObject.DOFade(1, 0.5f);

        string c1t1 = cs2_1_Text1.text;
        cs2_1_Text1.text = "";
        cs2_1_Text1.alpha = 1;
        yield return cs2_1_Text1.DOText(c1t1, c1t1.Length * charTypingSpeed).WaitForCompletion();

        yield return new WaitForSeconds(2.0f); // j초 대기

        // Soul과 Text1만 페이드 아웃 (Ray는 그대로 둠)
        soulObject.DOFade(0, 0.5f);
        yield return cs2_1_Text1.DOFade(0, 0.5f).WaitForCompletion();

        yield return new WaitForSeconds(textInterval);

        // --- CutScene 2-2 ---
        cs2_2_Elements.alpha = 1;
        eggObject.DOFade(1, 0.5f); // Egg 등장 (이후 유지)

        // Text 1
        string t1Content = cs2_2_Text1.text;
        cs2_2_Text1.text = "";
        cs2_2_Text1.alpha = 1;
        yield return cs2_2_Text1.DOText(t1Content, t1Content.Length * charTypingSpeed).WaitForCompletion();
        yield return new WaitForSeconds(2.0f);
        yield return cs2_2_Text1.DOFade(0, 0.5f).WaitForCompletion();

        // Text 2 (Ray와 Egg는 화면에 있는 상태)
        string t2Content = cs2_2_Text2.text;
        cs2_2_Text2.text = "";
        cs2_2_Text2.alpha = 1;
        yield return cs2_2_Text2.DOText(t2Content, t2Content.Length * charTypingSpeed).WaitForCompletion();
        yield return new WaitForSeconds(2.0f);
        yield return cs2_2_Text2.DOFade(0, 0.5f).WaitForCompletion();

        SceneLoader.Instance.LoadScene(SceneType.Stage1Scene);
    }
}