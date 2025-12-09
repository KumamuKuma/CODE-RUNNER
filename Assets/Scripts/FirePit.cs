using UnityEngine;
using System.Collections;

/// <summary>
/// Fire pit / trap / chest logic: supports multi-frame open animation.
/// </summary>
public class FirePit : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("When checked, this object starts lit/open (commonly used for traps). When unchecked, it must be triggered by a priest (commonly used for chests).")]
    public bool isLit = false;
    public Vector2Int gridPos;

    [Header("Function Settings")]
    [Tooltip("When checked, this object is treated as a pure trap and does NOT count toward the number of targets required to complete the level. Uncheck for chests.")]
    public bool isTrap = false;

    [Header("Sprite Settings")]
    public Sprite unlitSprite; // Sprite when unlit/closed (e.g., closed chest)

    [Tooltip("Sprite sequence after being lit/opened (e.g., 4-frame chest opening animation in order).")]
    public Sprite[] litSpriteSequence; // Array to support multi-frame open animation
    [Tooltip("Time interval between animation frames (seconds).")]
    public float animationInterval = 0.1f; // Animation speed

    [Header("Fire Effects (assign child objects from prefab)")]
    public GameObject flameEffect;
    public GameObject glowEffect;
    public GameObject sparkEffect;

    [Header("Optional Particle Effects")]
    public ParticleSystem extraFireParticle;

    // Global count of unlit, non-trap fire pits/chests
    public static int UnlitFireCount { get; private set; }

    private SpriteRenderer _sr;
    private Coroutine _animationCoroutine;

    void Update()
    {
        if (_sr != null)
        {
            // Use the same 500-based formula.
            // Chest does not add +1; when sharing the same cell, its sortingOrder is 1 lower than the priest,
            // so it is correctly occluded by the priest.
            _sr.sortingOrder = 500 - (gridPos.y * 10);
        }
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) return;

        UpdateFireVisual();
        SetEffectActive(isLit);

        if (extraFireParticle != null)
        {
            extraFireParticle.Stop();
            if (isLit) extraFireParticle.Play();
        }
    }

    void Start()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.SetGridType(gridPos, GridType.FirePit);
            // Only count non-trap and initially unlit objects as goals
            if (!isTrap && !isLit)
            {
                UnlitFireCount++;
            }
        }
    }

    /// <summary>
    /// Ignite fire pit / open chest (called externally).
    /// </summary>
    public void LightFire()
    {
        if (isLit) return; // Already lit

        isLit = true;

        // Only non-trap objects decrease the goal counter
        if (!isTrap)
        {
            UnlitFireCount--;
            UnlitFireCount = Mathf.Max(0, UnlitFireCount);
        }

        // Play open animation
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(PlayOpenAnimation());

        SetEffectActive(true);
        if (extraFireParticle != null) extraFireParticle.Play();
    }

    /// <summary>
    /// Coroutine: play multi-frame open animation.
    /// </summary>
    private IEnumerator PlayOpenAnimation()
    {
        // If no animation sequence is configured, just switch to the last sprite if available
        if (litSpriteSequence == null || litSpriteSequence.Length == 0)
        {
            if (litSpriteSequence != null && litSpriteSequence.Length > 0)
                _sr.sprite = litSpriteSequence[litSpriteSequence.Length - 1];
            yield break;
        }

        // Play each frame in sequence
        foreach (Sprite frame in litSpriteSequence)
        {
            if (frame != null)
            {
                _sr.sprite = frame;
                // Wait before showing the next frame
                yield return new WaitForSeconds(animationInterval);
            }
        }

        // Ensure we end on the last frame
        if (litSpriteSequence.Length > 0 && litSpriteSequence[litSpriteSequence.Length - 1] != null)
        {
            _sr.sprite = litSpriteSequence[litSpriteSequence.Length - 1];
        }

        _animationCoroutine = null;
    }

    private void UpdateFireVisual()
    {
        if (isLit)
        {
            // If already lit at start (e.g., traps), directly show the last frame of the lit sequence
            if (litSpriteSequence != null && litSpriteSequence.Length > 0)
                _sr.sprite = litSpriteSequence[litSpriteSequence.Length - 1];
        }
        else if (!isLit && unlitSprite != null)
        {
            _sr.sprite = unlitSprite;
        }
    }

    private void SetEffectActive(bool isActive)
    {
        if (flameEffect != null) flameEffect.SetActive(isActive);
        if (glowEffect != null) glowEffect.SetActive(isActive);
        if (sparkEffect != null) sparkEffect.SetActive(isActive);
    }

    public static void ResetFireCount()
    {
        UnlitFireCount = 0;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        float size = 1f;
        if (GridManager.Instance != null) size = GridManager.Instance.gridCellSize;

        int x = Mathf.RoundToInt(transform.position.x / size);
        int y = Mathf.RoundToInt(transform.position.y / size);

        if (gridPos.x != x || gridPos.y != y)
        {
            gridPos = new Vector2Int(x, y);
            transform.position = new Vector3(x * size, y * size, 0);
        }
    }
#endif
}
