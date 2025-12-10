using UnityEngine;
using UnityEngine.UI;

public class UI_SuccessPanel : MonoBehaviour
{
    public Image star1;
    public Image star2;
    public Image star3;

    public void ShowStars(int starCount)
    {
        starCount = Mathf.Clamp(starCount, 0, 3);

        SetStarActive(star1, starCount >= 1);
        SetStarActive(star2, starCount >= 2);
        SetStarActive(star3, starCount >= 3);
    }

    private void SetStarActive(Image img, bool on)
    {
        if (img == null) return;
        img.enabled = on;
    }
}
