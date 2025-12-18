using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteSilhouetteFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Shader Properties")]
    [SerializeField] private string silhouetteAmountProperty = "_SilhouetteAmount";
    [SerializeField] private string silhouetteColorProperty = "_SilhouetteColor";

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float fadeInSeconds = 0.03f;
    [SerializeField] private float holdSeconds = 0.04f;
    [SerializeField] private float fadeOutSeconds = 0.06f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool restartIfPlaying = true;

    private MaterialPropertyBlock mpb;
    private int amountId;
    private int colorId;
    private Coroutine routine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        mpb = new MaterialPropertyBlock();
        amountId = Shader.PropertyToID(silhouetteAmountProperty);
        colorId = Shader.PropertyToID(silhouetteColorProperty);

        Apply(0f);
    }

    public void Play()
    {
        Play(1f);
    }

    public void Play(float peakAmount)
    {
        if (spriteRenderer == null) return;

        peakAmount = Mathf.Clamp01(peakAmount);

        if (routine != null)
        {
            if (!restartIfPlaying) return;
            StopCoroutine(routine);
        }

        routine = StartCoroutine(Flash(peakAmount));
    }

    public void StopAndReset()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = null;
        Apply(0f);
    }

    private IEnumerator Flash(float peak)
    {
        float inDur = Mathf.Max(0f, fadeInSeconds);
        float holdDur = Mathf.Max(0f, holdSeconds);
        float outDur = Mathf.Max(0f, fadeOutSeconds);

        if (inDur > 0f)
            yield return Fade(0f, peak, inDur);
        else
            Apply(peak);

        if (holdDur > 0f)
            yield return new WaitForSeconds(holdDur);

        if (outDur > 0f)
            yield return Fade(peak, 0f, outDur);
        else
            Apply(0f);

        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float u = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            float e = ease != null ? ease.Evaluate(u) : u;

            Apply(Mathf.LerpUnclamped(from, to, e));
            yield return null;
        }

        Apply(to);
    }

    private void Apply(float amount)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(amountId, Mathf.Clamp01(amount));
        mpb.SetColor(colorId, flashColor);
        spriteRenderer.SetPropertyBlock(mpb);
    }
}