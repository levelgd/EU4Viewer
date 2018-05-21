using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BoneLine : MonoBehaviour
{
    public Material lineMaterial;
    public Transform root;

    public LineRenderer lr;

    GameObject boneName;
    GameObject boneJoint;
    Text boneText;

    bool isRoot = false;
    // Use this for initialization
    void Start()
    {
        if (transform != root)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.material = lineMaterial;

            AnimationCurve c = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(.9f, 1f), new Keyframe(1f, 0f));
            SetCurveLinear(c);
            lr.widthCurve = c;

            lr.startColor = lr.endColor = new Color(0.4f, 0.9f, 0.4f, 0.5f);
            
            lr.positionCount = 3;

            Vector3 mid = (transform.position - transform.parent.position).normalized * .2f;

            lr.SetPositions(new Vector3[]
            {
                transform.position, transform.parent.position + mid, transform.parent.position
            });
        }
        else
        {
            isRoot = true;
        }

        CreateJointName();
    }

    void CreateJointName()
    {
        boneName = new GameObject(gameObject.name);

        boneText = boneName.AddComponent<Text>();
        //boneName.AddComponent<Shadow>();
        boneText.horizontalOverflow = HorizontalWrapMode.Overflow;
        boneText.raycastTarget = false;
        boneText.alignment = TextAnchor.MiddleLeft;
        boneText.fontSize = 12;
        boneText.color = new Color(1, 1, 1, 0.5f);
        boneText.text = " " + gameObject.name;
        boneText.font = EditorController.instance.defaultFont;

        boneName.transform.SetParent(EditorController.instance.canvasBones);
        boneName.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        boneJoint = Instantiate(EditorController.instance.jointPrefab);
        boneJoint.GetComponent<BoneJoint>().bone = transform;
        boneJoint.transform.SetParent(boneName.transform);
    }

    // Update is called once per frame
    public void UpdateLine()
    {
        if (!isRoot && lr != null && lr.enabled)
        {
            lr.SetPosition(0, transform.position);
            Vector3 mid = (transform.position - transform.parent.position).normalized * .2f;
            lr.SetPosition(1, transform.parent.position + mid);
            lr.SetPosition(2, transform.parent.position);

            lr.widthMultiplier = Vector3.Distance(transform.position, transform.parent.position) / 5f;
        }

        if(boneName != null) boneName.transform.position = Camera.main.WorldToScreenPoint(transform.position);
    }

    public void Show(bool _bone, bool _name)
    {
        if (!isRoot)
        {
            if(lr != null) lr.enabled = _bone;
        }

        if (boneJoint != null) boneJoint.SetActive(_bone);
        if (boneText != null) boneText.enabled = _bone && _name;
    }

    private void OnDestroy()
    {
        Destroy(boneName);
    }

    public static void SetCurveLinear(AnimationCurve curve)
    {
        for (int i = 0; i < curve.keys.Length; ++i)
        {
            float intangent = 0;
            float outtangent = 0;
            bool intangent_set = false;
            bool outtangent_set = false;
            Vector2 point1;
            Vector2 point2;
            Vector2 deltapoint;
            Keyframe key = curve[i];

            if (i == 0)
            {
                intangent = 0; intangent_set = true;
            }

            if (i == curve.keys.Length - 1)
            {
                outtangent = 0; outtangent_set = true;
            }

            if (!intangent_set)
            {
                point1.x = curve.keys[i - 1].time;
                point1.y = curve.keys[i - 1].value;
                point2.x = curve.keys[i].time;
                point2.y = curve.keys[i].value;

                deltapoint = point2 - point1;

                intangent = deltapoint.y / deltapoint.x;
            }
            if (!outtangent_set)
            {
                point1.x = curve.keys[i].time;
                point1.y = curve.keys[i].value;
                point2.x = curve.keys[i + 1].time;
                point2.y = curve.keys[i + 1].value;

                deltapoint = point2 - point1;

                outtangent = deltapoint.y / deltapoint.x;
            }

            key.inTangent = intangent;
            key.outTangent = outtangent;
            curve.MoveKey(i, key);
        }
    }
}
