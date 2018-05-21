using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Locator : MonoBehaviour
{
    GameObject locator;
    GameObject locatorName;
    Text locatorNameText;

    public bool show = true;
    public bool showName = true;
    // Use this for initialization
    void Start()
    {
        
    }

    public void Init(string _name)
    {
        locator = Instantiate(EditorController.instance.locatorPrefab);
        locator.name = _name;
        locator.transform.SetParent(EditorController.instance.canvasLocators);

        CreateName();
    }

    void CreateName()
    {
        locatorName = new GameObject(gameObject.name);

        locatorNameText = locatorName.AddComponent<Text>();
        //boneName.AddComponent<Shadow>();
        locatorNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        locatorNameText.raycastTarget = false;
        locatorNameText.alignment = TextAnchor.MiddleLeft;
        locatorNameText.fontSize = 12;
        locatorNameText.color = new Color(1, 0.7f, 0.2f, 0.5f);
        locatorNameText.text = "  " + gameObject.name;
        locatorNameText.font = EditorController.instance.defaultFont;

        locatorNameText.transform.SetParent(locator.transform);
        locatorNameText.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (locator == null)
        {
            Destroy(this);
            return;
        }

        if (!show)
        {
            if (locator.activeSelf) locator.SetActive(false);
        }
        else
        {
            if (!locator.activeSelf) locator.SetActive(true);
            locator.transform.position = Camera.main.WorldToScreenPoint(transform.position);
        }

        if (!showName)
        {
            if (locatorName.activeSelf) locatorName.SetActive(false);
        }
        else
        {
            if (!locatorName.activeSelf) locatorName.SetActive(true);
        }
    }
}
