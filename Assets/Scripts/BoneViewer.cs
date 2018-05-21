using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoneViewer : MonoBehaviour
{
    public GUIStyle style;
    public bool show = true;
    public bool showInEditor = false;

    Material boneMaterial;

    public Transform[] bones;
    List<BoneLine> lines = new List<BoneLine>();
    // Use this for initialization
    void Start()
    {
        style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.richText = false;
        style.stretchWidth = style.stretchHeight = false;
        
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();

        if(smr == null)
        {
            Destroy(this);
            return;
        }

        foreach (Transform b in smr.bones)
        {
            if (b.GetComponent<BoneLine>() != null) continue;

            BoneLine bl = b.gameObject.AddComponent<BoneLine>();
            bl.root = smr.rootBone;
            bl.lineMaterial = EditorController.instance.boneMaterial;
            lines.Add(bl);
        }
    }

    public void Update()
    {
        foreach (BoneLine l in lines)
        {
            if (l != null)
            {
                l.Show(EditorController.instance.showBones, EditorController.instance.showBoneNames);
                l.UpdateLine();
            }
        }

        //Canvas.ForceUpdateCanvases();
    }

    /*void GetAllChildren(List<Transform> tList, Transform t)
    {
        tList.Add(t);

        foreach (Transform child in t)
        {
            GetAllChildren(tList, child);
        }
    }*/
}
