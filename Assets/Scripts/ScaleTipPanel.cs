using UnityEngine;
using System.Collections;

public class ScaleTipPanel : MonoBehaviour
{
    public Vector3[] sizes;
    public Transform box;

    Vector2 showPosition;
    Vector2 hidePosition;

    RectTransform rt;

    bool show = false;
    // Use this for initialization
    void Start()
    {
        rt = GetComponent<RectTransform>();

        showPosition = rt.anchoredPosition;

        hidePosition = new Vector2(showPosition.x + rt.rect.width, showPosition.y);

        rt.anchoredPosition = hidePosition;

        box.gameObject.SetActive(false);
    }

    public void ClickShowHide()
    {
        StartCoroutine(ShowHide(!show));
    }

    public void ShowSize(int index)
    {
        box.gameObject.SetActive(true);

        box.localScale = sizes[index];

        box.position = new Vector3(0, box.localScale.y * .5f, 0);
    }

    IEnumerator ShowHide(bool _show)
    {
        show = _show;

        float t = 0;

        if (show)
        {
            while(t < 1)
            {
                t += Time.deltaTime * 8f;

                rt.anchoredPosition = Vector2.Lerp(hidePosition, showPosition, t);

                yield return null;
            }

            rt.anchoredPosition = showPosition;
        }
        else
        {
            box.gameObject.SetActive(false);

            while (t < 1)
            {
                t += Time.deltaTime * 8f;

                rt.anchoredPosition = Vector2.Lerp(showPosition, hidePosition, t);

                yield return null;
            }

            rt.anchoredPosition = hidePosition;
        }
    }
}
