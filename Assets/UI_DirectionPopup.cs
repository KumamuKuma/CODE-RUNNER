using UnityEngine;
using UnityEngine.UI;

public class UI_DirectionPopup : MonoBehaviour
{
    [Header("9-Direction Buttons")]
    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;
    public Button btnUpLeft;
    public Button btnUpRight;
    public Button btnDownLeft;
    public Button btnDownRight;
    public Button btnCenter;

    private UI_PlacedBlock _currentActiveBlock;
    private RectTransform _parentCanvasRect;

    void Start()
    {
        InitializeCanvas();

        // Bind all nine-direction buttons
        if (btnUp) btnUp.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_Up));
        if (btnDown) btnDown.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_Down));
        if (btnLeft) btnLeft.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_Left));
        if (btnRight) btnRight.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_Right));
        if (btnUpLeft) btnUpLeft.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_LeftUp));
        if (btnUpRight) btnUpRight.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_RightUp));
        if (btnDownLeft) btnDownLeft.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_LeftDown));
        if (btnDownRight) btnDownRight.onClick.AddListener(() => OnDirectionSelected(CommandType.Move_RightDown));
        if (btnCenter) btnCenter.onClick.AddListener(ClosePopup);

        gameObject.SetActive(false);
    }

    void InitializeCanvas()
    {
        if (_parentCanvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) _parentCanvasRect = canvas.transform as RectTransform;
        }
    }

    public void OpenPopup(UI_PlacedBlock blockWhoOpenedMe)
    {
        _currentActiveBlock = blockWhoOpenedMe;
        InitializeCanvas();
        gameObject.SetActive(true);

        // Position the popup near the current mouse position
        Vector2 screenPos = Input.mousePosition;
        Vector3 worldPos;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _parentCanvasRect,
            screenPos,
            Camera.main,
            out worldPos))
        {
            transform.position = worldPos;

            Vector3 localPos = transform.localPosition;
            localPos.z = 0f;
            transform.localPosition = localPos;
        }
    }

    void OnDirectionSelected(CommandType selectedDir)
    {
        if (_currentActiveBlock != null)
        {
            _currentActiveBlock.SetDirection(selectedDir);
        }
        ClosePopup();
    }

    void ClosePopup()
    {
        _currentActiveBlock = null;
        gameObject.SetActive(false);
    }
}
