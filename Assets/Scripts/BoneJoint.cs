using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BoneJoint : MonoBehaviour
{
    static Vector2 size8 = new Vector2(8, 8);
    static Vector2 size16 = new Vector2(16, 16);

    public Transform bone;

    Image image;
    Color defaultColor;
    RectTransform rt;

    private void Start()
    {
        image = GetComponent<Image>();
        defaultColor = image.color;
        rt = GetComponent<RectTransform>();
    }

    public void Click()
    {
        EditorController.instance.SetAttach(bone);
    }

    public void Check(Transform _bone)
    {
        if(bone == _bone)
        {
            rt.sizeDelta = size16;

            image.color = Color.yellow;
        }
        else
        {
            rt.sizeDelta = size8;

            image.color = defaultColor;
        }
    }
}
