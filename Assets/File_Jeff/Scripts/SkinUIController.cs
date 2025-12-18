using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class SkinUIController : MonoBehaviour
{
    [Header("UI 아이콘 연결 (RectTransform)")]
    public RectTransform[] skinIcons = new RectTransform[3];

    [Header("화살표 UI 연결")]
    public RectTransform leftArrow;  // A키 화살표
    public RectTransform rightArrow; // S키 화살표

    [Header("컬러 설정")]
    public Color centerColor = Color.white;
    public Color sideColor = new(0.6f, 0.6f, 0.6f, 1f);

    [Header("위치 설정")]
    public Vector3 centerPosition = new(0, 0, 0);
    public Vector3 leftPosition = new(-150, -50, 0);
    public Vector3 rightPosition = new(150, -50, 0);

    [Header("애니메이션 설정")]
    public float rotationDuration = 0.35f;
    public Ease rotationEase = Ease.OutBack;
    public float arrowPunchForce = 20f;

    private RectTransform[] currentIconOrder;
    private Vector3[] positions;

    private void OnSkinChangeLeftEvent()
    {
        RotateIcons(1);
        AnimateArrow(leftArrow, -Vector3.right); // 왼쪽 화살표 흔들기
    }

    private void OnSkinChangeRightEvent()
    {
        RotateIcons(-1);
        AnimateArrow(rightArrow, Vector3.right); // 오른쪽 화살표 흔들기
    }

    private void AnimateArrow(RectTransform arrow, Vector3 direction)
    {
        if (arrow == null) return;

        arrow.DOComplete();
        // 화살표를 해당 방향으로 펀치(흔들림) 효과
        arrow.DOPunchAnchorPos(direction * arrowPunchForce, 0.2f, 10, 1);
    }

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
            currentIconOrder[0].GetComponent<Image>().color = centerColor;
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

        nextOrder[0].SetAsLastSibling();

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