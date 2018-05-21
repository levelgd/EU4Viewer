using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PdxAnimationPlayer : MonoBehaviour
{
    public PdxDataService.AnimationData animationData;
    public PdxDataService.AnimationData tPose;

    public int currentKey = 1;

    Dictionary<string, Transform> bones;

    public bool interpolation = true;

    public bool playing = true;

    float frameDelay;
    float frameTime;

    bool needInit = false;
    // Use this for initialization
    void Start()
    {
        bones = new Dictionary<string, Transform>();

        SkinnedMeshRenderer[] smr = GetComponentsInChildren<SkinnedMeshRenderer>();

        if(smr == null || smr.Length < 1)
        {
            DestroyImmediate(this);
            return;
        }

        foreach(SkinnedMeshRenderer sm in smr)
        {
            foreach (Transform b in sm.bones)
            {
                if(!bones.ContainsKey(b.name)) bones.Add(b.name, b);
            }
        }

        frameDelay = 1f / animationData.fps;
        frameTime = 0;

        CreateTPose();
        InitAnimation();
    }

    void InitAnimation()
    {
        needInit = false;

        List<string> boneNames = new List<string>();

        foreach (var a in animationData.hierarchy)
        {
            boneNames.Add(a.name);

            if (!bones.ContainsKey(a.name)) continue;
            //a.name - bone name
            Transform bone = bones[a.name];

            if (a.keys.Count == 0) continue;

            var key = a.keys[0];

            bone.localPosition = key.pos;
            bone.localRotation = key.rot;
            bone.localScale = key.scl;
        }

        SkinnedMeshRenderer[] smr = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer sm in smr)
        {
            foreach (Transform b in sm.bones)
            {
                if (!boneNames.Contains(b.name))
                {
                    var a = new PdxDataService.AnimationData.Animation();
                    a.parent = b;
                    a.name = b.name;
                    //a.sampleQ = a.sampleS = a.sampleT = true;

                    var k = new PdxDataService.AnimationData.Key();

                    bool rt = false;

                    if (a.name.ToLower().Equals("root"))
                    {
                        rt = true;

                        k.pos = b.position;
                        k.rot = b.rotation;
                    }
                    else
                    {
                        k.pos = b.localPosition;
                        k.rot = b.localRotation;
                        
                    }

                    k.scl = b.localScale;

                    a.keys.Add(k);
                    if (!rt) animationData.hierarchy.Add(a);
                    else animationData.hierarchy.Insert(0, a);
                }
            }
        }
    }

    public void CreateTPose()
    {
        SkinnedMeshRenderer[] smr = GetComponentsInChildren<SkinnedMeshRenderer>();

        tPose = new PdxDataService.AnimationData();

        foreach (SkinnedMeshRenderer sm in smr)
        {
            foreach (Transform b in sm.bones)
            {
                var a = new PdxDataService.AnimationData.Animation();
                a.parent = b;
                a.name = b.name;
                a.sampleQ = a.sampleS = a.sampleT = true;

                var k = new PdxDataService.AnimationData.Key();
                k.pos = b.localPosition;
                k.rot = b.localRotation;
                k.scl = b.localScale;

                a.keys.Add(k);
                tPose.hierarchy.Add(a);
            }
        }
    }

    public void SetTpose()
    {
        if (tPose == null || tPose.hierarchy == null) return;

        currentKey = 1;

        needInit = true;

        foreach (var a in tPose.hierarchy)
        {
            if (!bones.ContainsKey(a.name)) continue;

            Transform bone = bones[a.name];

            if (0 < a.keys.Count)
            {
                var current = a.keys[0];

                if (a.sampleT)
                {
                    bone.localPosition = current.pos;
                }

                if (a.sampleQ)
                {
                    bone.localRotation = current.rot;
                }

                if (a.sampleS)
                {
                    bone.localScale = current.scl;
                }
            }
        }
    }

    public void SetFrame(int frame)
    {
        if (frame < 0 || frame > animationData.sampleCount)
        {
            return;
        }

        currentKey = frame;
        
        foreach (var a in animationData.hierarchy)
        {
            if (!bones.ContainsKey(a.name)) continue;

            Transform bone = bones[a.name];

            if (currentKey < a.keys.Count)
            {
                var current = a.keys[frame];

                if (a.sampleT)
                {
                    bone.localPosition = current.pos;
                }

                if (a.sampleQ)
                {
                    bone.localRotation = current.rot;
                }

                if (a.sampleS)
                {
                    bone.localScale = current.scl;
                }
            } 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playing) return;

        if (needInit)
        {
            InitAnimation();
            return;
        }

        bool newKey = false;

        frameTime += Time.deltaTime;
        
        if (frameTime >= frameDelay)
        {
            frameTime -= frameDelay;
            currentKey = NextKey();
            newKey = true;
        }

        foreach (var a in animationData.hierarchy)
        {
            if (!bones.ContainsKey(a.name)) continue;

            Transform bone = bones[a.name];

            if (currentKey < a.keys.Count)
            {
                if (interpolation)
                {
                    var current = a.keys[currentKey];
                    var next = a.keys[NextKey()];

                    if (a.sampleT)
                    {
                        bone.localPosition = Vector3.Lerp(current.pos, next.pos, frameTime / frameDelay);
                    }

                    if (a.sampleQ)
                    {
                        bone.localRotation = Quaternion.Lerp(current.rot, next.rot, frameTime / frameDelay);
                    }

                    if (a.sampleS)
                    {
                        bone.localScale = Vector3.Lerp(current.scl, next.scl, frameTime / frameDelay);
                    }
                }
                else
                {
                    if (newKey)
                    {
                        var current = a.keys[currentKey];

                        if (a.sampleT)
                        {
                            bone.localPosition = current.pos;
                        }

                        if (a.sampleQ)
                        {
                            bone.localRotation = current.rot;
                        }

                        if (a.sampleS)
                        {
                            bone.localScale = current.scl;
                        }
                    }
                }
            }
        }
    }

    int NextKey()
    {
        if (currentKey + 1 > animationData.sampleCount)
        {
            return 2;
        }
        else
        {
            return currentKey + 1;
        }
    }
}
