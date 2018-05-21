using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ObjectButton : MonoBehaviour, IPointerClickHandler
{
    public PdxShape self;
    public Text buttonText;

    Image buttonImage;

    Color initialColor;
    // Use this for initialization
    void Start()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
            initialColor = buttonImage.color;
        }
    }

    public void SetObject(PdxShape go)
    {
        self = go;
        buttonText.text = go.name;
    }

    public void AnotherButtonClick(PdxShape go)
    {
        if(self != go)
        {
            buttonImage.color = initialColor;
        }
    }

    public void OnPointerClick(PointerEventData d)
    {
        if(buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
            initialColor = buttonImage.color;
        }

        buttonImage.color = Color.green;

        EditorController.instance.SelectObjectInHierarchy(self);
    }
}
