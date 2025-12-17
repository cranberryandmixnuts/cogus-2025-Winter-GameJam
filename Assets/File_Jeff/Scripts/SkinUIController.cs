using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class SkinUIController : MonoBehaviour
{
    [Header("UI 아이콘 연결 (RectTransform)")]
    public RectTransform[] skinIcons = new RectTransform[3];

    [Header("컬러 설정")]
    public Color centerColor = Color.white;
    public Color sideColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("위치 설정")]
    public Vector3 centerPosition = new Vector3(0, 0, 0);
    public Vector3 leftPosition = new Vector3(-150, -50, 0);
    public Vector3 rightPosition = new Vector3(150, -50, 0);

    [Header("애니메이션 설정")]
    public float rotationDuration = 0.35f;
    public Ease rotationEase = Ease.OutBack;

    private RectTransform[] currentIconOrder;
    private Vector3[] positions;

    private void OnSkinChangeLeftEvent() => RotateIcons(1);
    private void OnSkinChangeRightEvent() => RotateIcons(-1);


    private void Awake()
    {
        positions = new Vector3[] { centerPosition, leftPosition, rightPosition };
        currentIconOrder = skinIcons.ToArray();

        if (currentIconOrder.Length == 3)
        {
            currentIconOrder[0].localPosition = positions[0];
            currentIconOrder[1].localPosition = positions[1];
            currentIconOrder[2].localPosition = positions[2];
        }

        if (currentIconOrder.Length > 0)
        {
            Image centerImage = currentIconOrder[0].GetComponent<Image>();

            centerImage.color = centerColor;
            currentIconOrder[1].GetComponent<Image>().color = sideColor;
            currentIconOrder[2].GetComponent<Image>().color = sideColor;

            currentIconOrder[0].SetAsLastSibling();
        }
    }

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnSkinChangeLeft += OnSkinChangeLeftEvent;
            PlayerController.Instance.OnSkinChangeRight += OnSkinChangeRightEvent;
        }
    }

    private void Update()
    {

    }

    public void RotateIcons(int direction)
    {
        if (currentIconOrder.Length != 3) return;

        RectTransform[] nextOrder = new RectTransform[3];

        if (direction == 1)
        {
            nextOrder[0] = currentIconOrder[1];
            nextOrder[1] = currentIconOrder[2];
            nextOrder[2] = currentIconOrder[0];
        }
        else if (direction == -1)
        {
            nextOrder[0] = currentIconOrder[2];
            nextOrder[1] = currentIconOrder[0];
            nextOrder[2] = currentIconOrder[1];
        }
        else
        {
            return;
        }

        RectTransform centerIcon = nextOrder[0];

        centerIcon.SetAsLastSibling();

        for (int i = 0; i < 3; i++)
        {
            RectTransform iconToMove = nextOrder[i];
            Vector3 targetPos = positions[i];

            Image iconImage = iconToMove.GetComponent<Image>();
            Color targetColor = (i == 0) ? centerColor : sideColor;

            iconToMove.DOComplete();
            iconToMove.DOLocalMove(targetPos, rotationDuration).SetEase(rotationEase);

            iconImage.DOColor(targetColor, rotationDuration);
        }

        currentIconOrder = nextOrder;
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnSkinChangeLeft -= OnSkinChangeLeftEvent;
            PlayerController.Instance.OnSkinChangeRight -= OnSkinChangeRightEvent;
        }
    }
}