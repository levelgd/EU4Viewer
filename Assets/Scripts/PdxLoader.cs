using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class PdxLoader : MonoBehaviour
{
    public Material[] pdxMaterialsArray;
    public Dictionary<string, Material> pdxMaterials = new Dictionary<string, Material>();

    public Texture2D atlasExample;
    
    public Material materialHighlight;
    public GameObject selectedObject;
    public GameObject attachedObject;
    public List<GameObject> loadedObjects = new List<GameObject>();

    public PdxShape selectedShape;

    public PdxDataService.Base meshBase;
    public PdxDataService.AnimationData animBase;
    
    Dictionary<string, Transform> bonesByName = new Dictionary<string, Transform>();

    PdxDataService pds;
    // Use this for initialization
    void Start()
    {
        pds = new PdxDataService();
        
        foreach(Material m in pdxMaterialsArray)
        {
            pdxMaterials.Add(m.name, m);
        }
    }

    public void ClearAttach()
    {
        if (attachedObject != null) Destroy(attachedObject);
    }

    public void ClearScene()
    {
        meshBase = null;
        animBase = null;
        bonesByName.Clear();
        
        foreach (GameObject g in loadedObjects)
        {
            Destroy(g);
        }

        loadedObjects.Clear();
        selectedObject = null;
        selectedShape = null;
        attachedObject = null;
    }

    public void LoadMesh(string path, Transform attach = null)
    {
        if (attach == null)
        {
            ClearScene();
        }
        else
        {
            ClearAttach();
        }

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            byte[] file = File.ReadAllBytes(path);
            meshBase = pds.ReadFromBuffer(file);

            if(meshBase == null)
            {
                return;
            }

            BuildMesh(path, meshBase, attach);
        }
    }

    public void SaveMesh(string folder)
    {
        if (selectedObject != null)
        {
            string f = folder.TrimEnd('/', '\\') + Path.DirectorySeparatorChar;

            pds.WriteToFile(pds.BaseFromGameObject(selectedObject), f + selectedObject.name.ToLower().Replace(".mesh","").Replace(".fbx", "") + ".mesh");

            PdxShape[] shapes = selectedObject.GetComponentsInChildren<PdxShape>();

            foreach (PdxShape sh in shapes)
            {
                sh.CopyDiffuse(f);
                sh.CopyNormal(f);
                sh.CopySpecular(f);
            }
        }
    }

    public void SaveAnim(string file)
    {
        if(selectedObject != null)
        {
            var p = selectedObject.GetComponent<PdxAnimationPlayer>();
            if(p != null) pds.WriteToFile(pds.BaseFromAnimation(p.animationData), file);
        }
    }

    public void LoadAnim(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            byte[] file = File.ReadAllBytes(path);

            if (selectedObject != null)
            {
                PdxAnimationPlayer p = selectedObject.GetComponent<PdxAnimationPlayer>();
                if (p != null)
                {
                    p.SetTpose();
                    Destroy(p);
                }

                p = selectedObject.AddComponent<PdxAnimationPlayer>();
                animBase = p.animationData = CreatePdxAnimation(pds.ReadFromBuffer(file));
            }
        }
    }

    void BuildMesh(string path, PdxDataService.Base _baseMesh, Transform attach)
    {
        Matrix4x4 m = Matrix4x4.identity;

        PdxDataService.Base pdxData = _baseMesh;

        float min = float.MaxValue;
        GameObject goRoot = new GameObject();
        goRoot.name = Path.GetFileName(path);

        List<Material> snow = new List<Material>();
        List<Material> color = new List<Material>();
        List<Material> atlas = new List<Material>();

        for (int i = 0; i < pdxData.subNodes["object"].subNodes.Count; i++)
        {
            List<string> ikeys = new List<string>(pdxData.subNodes["object"].subNodes.Keys);

            if (pdxData.subNodes["object"].subNodes[ikeys[i]].type != "object") continue;

            PdxDataService.Base pdxShape = pdxData.subNodes["object"].subNodes[ikeys[i]];

            bool skinned = false;
            Transform[] bones = null;
            string shapeName = pdxShape.name;

            if (pdxShape.subNodes.ContainsKey("skeleton"))
            {
                PdxDataService.Base skeleton = pdxShape.subNodes["skeleton"];
                bones = new Transform[skeleton.subNodes.Count];
                
                for (int j = 0; j < skeleton.subNodes.Count; j++)
                {
                    List<string> jkeys = new List<string>(skeleton.subNodes.Keys);

                    GameObject bone = new GameObject(skeleton.subNodes[jkeys[j]].name);
                    bone.tag = "Bone";

                    var pdxBone = skeleton.subNodes[jkeys[j]].subNodes;
                    List<PdxDataService.Base.Data> boneTx = pdxBone["tx"].value;

                    //Transform parent = null;

                    //if (pdxBone.ContainsKey("pa")) parent = bones[pdxBone["pa"].value[0].i];

                    Matrix4x4 matrix = new Matrix4x4();
                    matrix.SetRow(0, new Vector4(boneTx[0].f, boneTx[3].f, boneTx[6].f, boneTx[9].f));
                    matrix.SetRow(1, new Vector4(boneTx[1].f, boneTx[4].f, boneTx[7].f, boneTx[10].f));
                    matrix.SetRow(2, new Vector4(boneTx[2].f, boneTx[5].f, boneTx[8].f, boneTx[11].f));
                    matrix.SetRow(3, new Vector4(0, 0, 0, 1));

                    bone.transform.FromMatrix(matrix.inverse);
                    //bone.transform.parent = parent;

                    bones[j] = bone.transform;

                    bonesByName[bone.name] = bone.transform;
                }

                for (int j = 0; j < skeleton.subNodes.Count; j++)
                {
                    List<string> jkeys = new List<string>(skeleton.subNodes.Keys);

                    Transform bone = bones[j];

                    var pdxBone = skeleton.subNodes[jkeys[j]].subNodes;

                    if (pdxBone.ContainsKey("pa"))
                    {
                        bone.parent = bones[pdxBone["pa"].value[0].i];
                    }
                    else
                    {
                        bone.parent = goRoot.transform;
                    }
                }
            }

            for (int j = 0; j < pdxShape.subNodes.Count; j++)
            {
                List<string> jkeys = new List<string>(pdxShape.subNodes.Keys);

                if (pdxShape.subNodes[jkeys[j]].type != "object") continue;

                var pdxMesh = pdxShape.subNodes[jkeys[j]].subNodes;
              
                if (pdxMesh.ContainsKey("p"))
                {
                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector3> normals = new List<Vector3>();
                    List<Vector4> tangents = new List<Vector4>();
                    List<Vector2> uv = new List<Vector2>();
                    List<Vector2> uv2 = new List<Vector2>();
                    List<int> triangles = new List<int>();
                    List<BoneWeight> boneWeights = new List<BoneWeight>();

                    List<PdxDataService.Base.Data> vValue = pdxMesh["p"].value;

                    for (int k = 0; k < vValue.Count; k += 3)
                        vertices.Add(new Vector3(vValue[k].f, vValue[k + 1].f, vValue[k + 2].f));

                    if (pdxMesh.ContainsKey("n"))
                    {
                        List<PdxDataService.Base.Data> nValue = pdxMesh["n"].value;

                        for (int k = 0; k < nValue.Count; k += 3)
                            normals.Add(new Vector3(nValue[k].f, nValue[k + 1].f, nValue[k + 2].f));
                    }

                    if (pdxMesh.ContainsKey("ta"))
                    {
                        List<PdxDataService.Base.Data> taValue = pdxMesh["ta"].value;

                        for (int k = 0; k < taValue.Count; k += 4)
                            tangents.Add(new Vector4(taValue[k].f, taValue[k + 1].f, taValue[k + 2].f, taValue[k + 3].f));
                    }

                    if (pdxMesh.ContainsKey("u0"))
                    {
                        List<PdxDataService.Base.Data> uvValue = pdxMesh["u0"].value;

                        for (int k = 0; k < uvValue.Count; k += 2)
                            uv.Add(new Vector2(uvValue[k].f, uvValue[k + 1].f));
                    }

                    if (pdxMesh.ContainsKey("u1"))
                    {
                        List<PdxDataService.Base.Data> uv2Value = pdxMesh["u1"].value;

                        for (int k = 0; k < uv2Value.Count; k += 2)
                            uv2.Add(new Vector2(uv2Value[k].f, uv2Value[k + 1].f));
                    }
                    
                    if (pdxShape.subNodes[jkeys[j]].subNodes.ContainsKey("skin"))
                    {
                        skinned = true;
                        var skin = pdxShape.subNodes[jkeys[j]].subNodes["skin"].subNodes;
                        int influencesPerVertex = skin["bones"].value[0].i;

                        int indexCount = skin["ix"].value.Count;
                        for (int k = 0; k < indexCount; k += 4)
                        {
                            BoneWeight bw = new BoneWeight();

                            var ixValue = skin["ix"].value;

                            bw.boneIndex0 = ixValue[k].i;
                            if (influencesPerVertex > 1) bw.boneIndex1 = ixValue[k + 1].i;
                            if (influencesPerVertex > 2) bw.boneIndex2 = ixValue[k + 2].i;
                            if (influencesPerVertex > 3) bw.boneIndex3 = ixValue[k + 3].i;

                            if (bw.boneIndex0 < 0) bw.boneIndex0 = 0;
                            if (bw.boneIndex1 < 0) bw.boneIndex1 = 0;
                            if (bw.boneIndex2 < 0) bw.boneIndex2 = 0;
                            if (bw.boneIndex3 < 0) bw.boneIndex3 = 0;

                            var wvalue = skin["w"].value;

                            bw.weight0 = wvalue[k].f;
                            if (influencesPerVertex > 1) bw.weight1 = wvalue[k + 1].f;
                            if (influencesPerVertex > 2) bw.weight2 = wvalue[k + 2].f;
                            if (influencesPerVertex > 3) bw.weight3 = wvalue[k + 3].f;

                            boneWeights.Add(bw);
                        }
                    }
                    else
                    {
                        skinned = false;
                    }

                    List<PdxDataService.Base.Data> triValue = pdxMesh["tri"].value;

                    for (int k = 0; k < triValue.Count; k++)
                    {
                        triangles.Add(triValue[k].i);
                    }

                    Texture2D diffuse = null;
                    Texture2D normal = null;
                    Texture2D specular = null;

                    bool collisionMesh = false;

                    string shaderName = "";
                    string texDiffusePath = "";
                    string texNormalPath = "";
                    string texSpecularPath = "";
                    
                    if (pdxShape.subNodes[jkeys[j]].subNodes.ContainsKey("material"))
                    {
                        string folderPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;

                        PdxDataService.Base matBase = pdxShape.subNodes[jkeys[j]].subNodes["material"];

                        shaderName = matBase.subNodes["shader"].value[0].s;

                        switch (shaderName)
                        {
                            case "Collision":
                                collisionMesh = true;
                                break;

                            default:
                                texDiffusePath = folderPath + matBase.subNodes["diff"].value[0].s;
                                diffuse = LoadDDS(texDiffusePath);
                                if (diffuse != null)
                                {
                                    diffuse.name = Path.GetFileName(texDiffusePath);
                                }
                                
                                texNormalPath = folderPath + matBase.subNodes["n"].value[0].s;
                                normal = LoadDDS(texNormalPath);
                                if (normal != null)
                                {
                                    normal.name = Path.GetFileName(texNormalPath);
                                }
                                
                                texSpecularPath = folderPath + matBase.subNodes["spec"].value[0].s;
                                specular = LoadDDS(texSpecularPath);
                                if (specular != null)
                                {
                                    specular.name = Path.GetFileName(texSpecularPath);
                                }
                                //Grayscale(specular);
                                break;
                        }
                    }

                    Mesh mesh = new Mesh();
                    mesh.name = shapeName;
                    mesh.vertices = vertices.ToArray();
                    mesh.normals = normals.ToArray();
                    mesh.triangles = triangles.ToArray();
                    mesh.tangents = tangents.ToArray();
                    mesh.uv = uv.ToArray();
                    if (uv2.Count > 0) mesh.uv2 = uv2.ToArray();

                    mesh.RecalculateBounds();
                    //mesh.RecalculateNormals();
                    mesh.RecalculateTangents();
                    
                    GameObject go = new GameObject();
                    go.name = shapeName;
                    go.tag = "Shape";

                    go.transform.SetParent(goRoot.transform);
                    //if (!collisionMesh && bones != null && bones.Length > 0) bones[0].SetParent(goRoot.transform);

                    if (!skinned && !collisionMesh)
                    {
                        MeshFilter mf = go.AddComponent<MeshFilter>();
                        mf.mesh = mesh;
                    }

                    SkinnedMeshRenderer smr = null;
                    MeshRenderer mr = null;

                    if (skinned) smr = go.AddComponent<SkinnedMeshRenderer>();
                    else if (!collisionMesh) mr = go.AddComponent<MeshRenderer>();

                    Material _mat = null;
                    if (pdxMaterials.ContainsKey(shaderName)) _mat = new Material(pdxMaterials[shaderName]);
                    else _mat = new Material(pdxMaterials["PdxMeshStandard"]);
                    _mat.name = shaderName;

                    switch (shaderName)
                    {
                        case "PdxMeshSnow":
                            snow.Add(_mat);
                            break;

                        case "PdxMeshColor":
                            color.Add(_mat);
                            break;

                        case "PdxMeshTextureAtlas":
                            atlas.Add(_mat);
                            _mat.SetTexture("_Atlas", atlasExample);
                            break;
                    }
                    

                    Material _hl = new Material(materialHighlight);

                    _mat.SetTexture("_Diffuse", diffuse);
                    _mat.SetTexture("_Normal", normal);
                    _mat.SetTexture("_Specular", specular);

                    if (skinned)
                    {
                        smr.bones = bones;
                        smr.rootBone = FindBone(bones, "root");

                        smr.quality = SkinQuality.Bone4;
                        smr.updateWhenOffscreen = true;

                        Matrix4x4[] bindposes = new Matrix4x4[bones.Length];

                        for (int b = 0; b < bones.Length; b++)
                        {
                            bindposes[b] = bones[b].worldToLocalMatrix * goRoot.transform.localToWorldMatrix;
                        }

                        mesh.RecalculateBounds();
                        mesh.bindposes = bindposes;

                        mesh.boneWeights = boneWeights.ToArray();

                        smr.sharedMesh = mesh;
                        smr.sharedMaterials = new Material[] { _mat, _hl };
                    }
                    else if (!collisionMesh)
                    {
                        //mr.material = _mat;
                        mr.sharedMaterials = new Material[] { _mat, _hl };
                    }

                    if (collisionMesh)
                    {
                        //go.tag = "Collision";
                        MeshFilter mc = go.AddComponent<MeshFilter>();
                        mc.mesh = mesh;

                        MeshRenderer cmr = go.AddComponent<MeshRenderer>();
                        cmr.sharedMaterial = _mat;
                    }

                    if(attach == null)
                    {
                        PdxShape sh = go.AddComponent<PdxShape>();

                        if (smr != null) sh.smr = smr;
                        else sh.mr = mr;

                        sh.shader = shaderName;
                        sh.diffuse = texDiffusePath;
                        sh.normal = texNormalPath;
                        sh.specular = texSpecularPath;

                        if (min > mesh.bounds.min.y) min = mesh.bounds.min.y;
                    }
                }
            }
        }

        if (attach == null && _baseMesh.subNodes.ContainsKey("locator") && _baseMesh.subNodes["locator"].subNodes.Count > 0)
        {
            var locators = _baseMesh.subNodes["locator"].subNodes;

            //print(locators.Count);

            for (int l = 0; l < locators.Count; l++)
            {
                List<string> lkeys = new List<string>(locators.Keys);

                GameObject loc = new GameObject(locators[lkeys[l]].name);
                loc.tag = "Locator";

                var lks = locators[lkeys[l]].subNodes;
                
                if (lks.ContainsKey("pa") && bonesByName.ContainsKey(lks["pa"].value[0].s)) loc.transform.SetParent(bonesByName[lks["pa"].value[0].s]);
                else if(bonesByName.ContainsKey("Root")) loc.transform.SetParent(bonesByName["Root"]);
                else if(bonesByName.ContainsKey("root")) loc.transform.SetParent(bonesByName["root"]);

                var lP = lks["p"].value;
                loc.transform.localPosition = new Vector3(lP[0].f, lP[1].f, lP[2].f);

                var lQ = lks["q"].value;
                loc.transform.localRotation = new Quaternion(lQ[0].f, lQ[1].f, lQ[2].f, lQ[3].f);
                
                Locator locComp = loc.AddComponent<Locator>();
                locComp.Init(loc.name);
            }
        }

        if(attach != null)
        {
            attachedObject = goRoot;

            SkinnedMeshRenderer smr = goRoot.GetComponentInChildren<SkinnedMeshRenderer>();

            if(smr != null)
            {
                smr.rootBone.transform.SetParent(attach);
                smr.rootBone.transform.localPosition = Vector3.zero;
                smr.rootBone.transform.localRotation = Quaternion.identity;
            }
            else
            {
                goRoot.transform.SetParent(attach);
                goRoot.transform.localPosition = Vector3.zero;
                goRoot.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            selectedObject = goRoot;
            loadedObjects.Add(goRoot);
            goRoot.transform.Translate(0, -min, 0, Space.World);

            goRoot.AddComponent<BoneViewer>();

            EditorController.instance.SceneHasSnowMaterial(snow.Count > 0 ? snow.ToArray() : null);
            EditorController.instance.SceneHasColorMaterial(color.Count > 0 ? color.ToArray() : null);
            EditorController.instance.SceneHasAtlasMaterial(atlas.Count > 0 ? atlas.ToArray() : null);
        }
    }

    PdxDataService.AnimationData CreatePdxAnimation(PdxDataService.Base _baseAnim)
    {
        var pdxAnimProps = _baseAnim.subNodes["info"].subNodes;

        PdxDataService.AnimationData animationData = new PdxDataService.AnimationData()
        {
            name = "",
            fps = pdxAnimProps["fps"].value[0].f,
            length = pdxAnimProps["sa"].value[0].i / pdxAnimProps["fps"].value[0].f,
            sampleCount = pdxAnimProps["sa"].value[0].i
        };

        Dictionary<string, string> alternativeNames = new Dictionary<string, string>();
        alternativeNames["attack_L_hand"] = "Left_hand_node";
        alternativeNames["attack_R_hand"] = "Right_hand_node";

        for (int k = 0; k < _baseAnim.subNodes["info"].subNodes.Count; k++)
        {
            List<string> kkeys = new List<string>(_baseAnim.subNodes["info"].subNodes.Keys);

            var pdxAnimBone = _baseAnim.subNodes["info"].subNodes[kkeys[k]];

            if (pdxAnimBone.type != "object") continue;

            Transform bone = null;

            if (bonesByName.ContainsKey(pdxAnimBone.name))
                bone = bonesByName[pdxAnimBone.name];

            if (bone == null && alternativeNames.ContainsKey(pdxAnimBone.name) && bonesByName.ContainsKey(alternativeNames[pdxAnimBone.name]))
                bone = bonesByName[alternativeNames[pdxAnimBone.name]];

            var _t = pdxAnimBone.subNodes["t"].value;
            var _q = pdxAnimBone.subNodes["q"].value;
            var _s = pdxAnimBone.subNodes["s"].value;
            string _sa = pdxAnimBone.subNodes["sa"].value[0].s;

            PdxDataService.AnimationData.Animation a = new PdxDataService.AnimationData.Animation()
            {
                parent = bone != null ? bone.parent : null,
                name = pdxAnimBone.name,
                keys = new List<PdxDataService.AnimationData.Key>() { new PdxDataService.AnimationData.Key()
                    {
                        time = 0,
                        pos = new Vector3(_t[0].f, _t[1].f, _t[2].f),
                        rot = new Quaternion(_q[0].f, _q[1].f, _q[2].f, _q[3].f),
                        scl = new Vector3(_s[0].f, _s[0].f, _s[0].f)
                    } },
                sampleT = _sa.IndexOf('t') > -1,
                sampleQ = _sa.IndexOf('q') > -1,
                sampleS = _sa.IndexOf('s') > -1,
                skipData = false
            };

            animationData.hierarchy.Add(a);
        }

        int offsetT = 0;
        int offsetQ = 0;
        int offsetS = 0;

        var pdxAnimSamples = _baseAnim.subNodes["samples"].subNodes;

        for (int sample = 0; sample < animationData.sampleCount; sample++)
        {
            for (int k = 0; k < animationData.hierarchy.Count; k++)
            {
                var hier = animationData.hierarchy[k];

                if (hier.sampleT || hier.sampleQ || hier.sampleS)
                {
                    PdxDataService.AnimationData.Key key = new PdxDataService.AnimationData.Key();

                    key.time = (float)sample * (1f / animationData.fps);
                    
                    if (hier.sampleT)
                    {
                        var _t = pdxAnimSamples["t"].value;
                        key.pos = new Vector3(_t[offsetT].f, _t[offsetT + 1].f, _t[offsetT + 2].f);
                        offsetT += 3;
                    }

                    if (hier.sampleQ)
                    {
                        var _q = pdxAnimSamples["q"].value;
                        key.rot = new Quaternion(_q[offsetQ].f, _q[offsetQ + 1].f, _q[offsetQ + 2].f, _q[offsetQ + 3].f);
                        offsetQ += 4;
                    }

                    if (hier.sampleS)
                    {
                        var _s = pdxAnimSamples["s"].value;
                        key.scl = new Vector3(_s[offsetS].f, _s[offsetS].f, _s[offsetS].f);
                        offsetS++;
                    }

                    hier.keys.Add(key);
                }
            }
        }

        return animationData;
    }

    public static Texture2D LoadDDS(string path)
    {
        Texture2D tex = null;

        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);

            int h = BitConverter.ToInt32(bytes, 12);
            int w = BitConverter.ToInt32(bytes, 16);

            TextureFormat format = GetDDSFormat(bytes);

            tex = new Texture2D(w, h, format, true);

            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[bytes.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(bytes, DDS_HEADER_SIZE, dxtBytes, 0, bytes.Length - DDS_HEADER_SIZE);

            try
            {
                tex.LoadRawTextureData(dxtBytes);
            }
            catch (UnityException)
            {
                tex = new Texture2D(w, h, format, false);
                tex.LoadRawTextureData(dxtBytes);
            }

            tex.Apply();
        }

        return tex;
    }

    public static TextureFormat GetDDSFormat(byte[] ddsBytes)
    {
        int index = 20;

        while (index + 4 < ddsBytes.Length)
        {
            if (ddsBytes[index] == 'D' && ddsBytes[index + 1] == 'X' && ddsBytes[index + 2] == 'T')
            {
                switch (ddsBytes[index + 3])
                {
                    case (byte)'1':
                        return TextureFormat.DXT1;
                    case (byte)'5':
                        return TextureFormat.DXT5;
                }
            }

            index++;
        }

        return TextureFormat.DXT1;
    }

    Transform FindBone(Transform[] bones, string find)
    {
        foreach(Transform b in bones)
        {
            if (b.name.ToLower() == find.ToLower()) return b;
        }

        return bones[0];
    }
}
