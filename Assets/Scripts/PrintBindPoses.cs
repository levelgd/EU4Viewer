using UnityEngine;
using System.Collections;

public class PrintBindPoses : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {

    }

    public void PrintBs()
    {
        /*Matrix4x4[] ms = GetComponent<SkinnedMeshRenderer>().sharedMesh.bindposes;

        int i = 0;
        foreach (Matrix4x4 m in ms)
        {
            print(m.);

            if (++i > 10) return;
        }*/
    }

    public void PrintBodyWeights()
    {
        BoneWeight[] ws = GetComponent<SkinnedMeshRenderer>().sharedMesh.boneWeights;

        int i = 0;
        foreach (BoneWeight w in ws)
        {
            print(w.boneIndex0 + ":" + w.weight0);

            if (++i > 3) return;
        }
    }
}
