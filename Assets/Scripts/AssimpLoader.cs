using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using Assimp;

public class AssimpLoader : MonoBehaviour
{
    public Texture2D atlasExample;
    public UnityEngine.Material highlightMaterial;
    
    // Use this for initialization
    void Start()
    {
        
    }

    public GameObject LoadMesh(string path)
    {
        AssimpContext context = new AssimpContext();
        Scene scene = context.ImportFile(path, PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipWindingOrder);

        if (!scene.HasMeshes) return null;

        List<UnityEngine.Material> snow = new List<UnityEngine.Material>();
        List<UnityEngine.Material> color = new List<UnityEngine.Material>();
        List<UnityEngine.Material> atlas = new List<UnityEngine.Material>();

        Dictionary<string, Transform> bonesByName = new Dictionary<string, Transform>();
        GameObject goRoot = new GameObject(Path.GetFileName(path));

        float min = 0;

        int idx = 0;

        foreach (Assimp.Mesh sceneMesh in scene.Meshes)
        {
            GameObject go = new GameObject("pShape" + idx);
            go.tag = "Shape";
            
            bool skinned = false;
            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name = "pMeshShape" + idx;

            idx++;
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector2> uv2 = null;

            List<BoneWeight> boneWeights = new List<BoneWeight>();

            Transform[] bones = null;

            foreach (var sceneVertex in sceneMesh.Vertices)
            {
                vertices.Add(new Vector3(sceneVertex.X, sceneVertex.Y, sceneVertex.Z));
            }

            foreach (var sceneNormal in sceneMesh.Normals)
            {
                normals.Add(new Vector3(sceneNormal.X, sceneNormal.Y, sceneNormal.Z));
            }

            foreach (var sceneUV in sceneMesh.TextureCoordinateChannels[0])
            {
                uv.Add(new Vector2(sceneUV.X, 1f - sceneUV.Y));
            }

            if(sceneMesh.TextureCoordinateChannelCount > 1)
            {
                uv2 = new List<Vector2>();

                foreach (var sceneUV2 in sceneMesh.TextureCoordinateChannels[1])
                {
                    uv2.Add(new Vector2(sceneUV2.X, 1f - sceneUV2.Y));
                }
            }

            Transform rootBone = null;

            if (sceneMesh.HasBones)
            {
                for (int j = 0; j < sceneMesh.VertexCount; j++) boneWeights.Add(new BoneWeight());

                skinned = true;

                bones = new Transform[sceneMesh.BoneCount];

                int boneIndex = 0;
                foreach (var sceneBone in sceneMesh.Bones)
                {
                    GameObject bone = new GameObject(sceneBone.Name);
                    bone.tag = "Bone";

                    UnityEngine.Matrix4x4 matrix = new UnityEngine.Matrix4x4();

                    matrix.SetRow(0, new Vector4(sceneBone.OffsetMatrix.A1, sceneBone.OffsetMatrix.A2, sceneBone.OffsetMatrix.A3, sceneBone.OffsetMatrix.A4));
                    matrix.SetRow(1, new Vector4(sceneBone.OffsetMatrix.B1, sceneBone.OffsetMatrix.B2, sceneBone.OffsetMatrix.B3, sceneBone.OffsetMatrix.B4));
                    matrix.SetRow(2, new Vector4(sceneBone.OffsetMatrix.C1, sceneBone.OffsetMatrix.C2, sceneBone.OffsetMatrix.C3, sceneBone.OffsetMatrix.C4));
                    matrix.SetRow(3, new Vector4(sceneBone.OffsetMatrix.D1, sceneBone.OffsetMatrix.D2, sceneBone.OffsetMatrix.D3, sceneBone.OffsetMatrix.D4));

                    bone.transform.FromMatrix(matrix.inverse);
                    
                    bonesByName[bone.name] = bone.transform;
                    bones[boneIndex] = bone.transform;

                    if (sceneBone.HasVertexWeights)
                    {
                        for (int i = 0; i < sceneBone.VertexWeights.Count; i++)
                        {
                            BoneWeight bw = boneWeights[sceneBone.VertexWeights[i].VertexID];
                            if (bw.boneIndex0 == 0 && bw.weight0 == 0) { bw.boneIndex0 = boneIndex; bw.weight0 = sceneBone.VertexWeights[i].Weight; }
                            else if (bw.boneIndex1 == 0 && bw.weight1 == 0) { bw.boneIndex1 = boneIndex; bw.weight1 = sceneBone.VertexWeights[i].Weight; }
                            else if (bw.boneIndex2 == 0 && bw.weight2 == 0) { bw.boneIndex2 = boneIndex; bw.weight2 = sceneBone.VertexWeights[i].Weight; }
                            else if (bw.boneIndex3 == 0 && bw.weight3 == 0) { bw.boneIndex3 = boneIndex; bw.weight3 = sceneBone.VertexWeights[i].Weight; }

                            /*if (bw.weight0 < 0.0011f) bw.weight0 = 0f;
                            if (bw.weight1 < 0.0011f) bw.weight1 = 0f;
                            if (bw.weight2 < 0.0011f) bw.weight2 = 0f;
                            if (bw.weight3 < 0.0011f) bw.weight3 = 0f;*/

                            boneWeights[sceneBone.VertexWeights[i].VertexID] = bw;
                        }
                    }
                    
                    boneIndex++;
                }

                foreach (var bone in bonesByName)
                {
                    if (bone.Key.ToLower().Equals("root")) rootBone = bone.Value;

                    string parent = "";

                    FindParentInHierarchy(bone.Key, scene.RootNode, out parent);

                    if (parent.Length > 0 && bonesByName.ContainsKey(parent)) bone.Value.SetParent(bonesByName[parent]);
                    else bone.Value.SetParent(goRoot.transform);
                }
            }

            UnityEngine.Matrix4x4[] bindposes = null;

            if (bones != null)
            {
                bindposes = new UnityEngine.Matrix4x4[bones.Length];

                for (int b = 0; b < bones.Length; b++)
                {
                    bindposes[b] = bones[b].worldToLocalMatrix * go.transform.localToWorldMatrix;
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = sceneMesh.GetIndices();
            mesh.uv = uv.ToArray();

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            if (min < mesh.bounds.max.z) min = mesh.bounds.max.z;
            //print(mesh.bounds.min.ToString());
            //print(mesh.bounds.max.ToString());

            PdxShape shape = go.AddComponent<PdxShape>();

            Assimp.Material _mat = scene.Materials[sceneMesh.MaterialIndex];

            //print(_mat.Name);
            
            Shader shader = Shader.Find("PDX/" + _mat.Name);
            shape.shader = _mat.Name;
            if (shader == null)
            {
                shader = Shader.Find("PDX/PdxMeshStandard");
                shape.shader = "PdxMeshStandard";
            }

            UnityEngine.Material mat = new UnityEngine.Material(shader);
            bool collision = false;

            switch (shape.shader)
            {
                case "Collision":
                    collision = true;
                    break;

                case "PdxMeshSnow":
                    snow.Add(mat);
                    break;

                case "PdxMeshColor":
                    color.Add(mat);
                    break;

                case "PdxMeshTextureAtlas":
                    atlas.Add(mat);
                    mat.SetTexture("_Atlas", atlasExample);
                    break;
            }

            if (_mat.HasTextureDiffuse)
            {
                if (_mat.TextureDiffuse.FilePath.EndsWith(".dds"))
                {
                    string texPath = _mat.TextureDiffuse.FilePath;
                    if (texPath[1] != ':') texPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + _mat.TextureDiffuse.FilePath;
                    
                    if (File.Exists(texPath))
                    {
                        shape.diffuse = texPath;
                        Texture2D tex = PdxLoader.LoadDDS(texPath);
                        tex.name = Path.GetFileName(texPath);
                        mat.SetTexture("_Diffuse", tex);
                    }
                }
            }
            if (_mat.HasTextureHeight)
            {
                if (_mat.TextureHeight.FilePath.EndsWith(".dds"))
                {
                    string texPath = _mat.TextureHeight.FilePath;
                    if (texPath[1] != ':') texPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + _mat.TextureHeight.FilePath;

                    if (File.Exists(texPath))
                    {
                        shape.normal = texPath;
                        Texture2D tex = PdxLoader.LoadDDS(texPath);
                        tex.name = Path.GetFileName(texPath);
                        mat.SetTexture("_Normal", tex);
                    }
                }
            }
            if (_mat.HasTextureSpecular)
            {
                if (_mat.TextureSpecular.FilePath.EndsWith(".dds"))
                {
                    string texPath = _mat.TextureSpecular.FilePath;
                    if (texPath[1] != ':') texPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + _mat.TextureSpecular.FilePath;

                    if (File.Exists(texPath))
                    {
                        shape.specular = texPath;
                        Texture2D tex = PdxLoader.LoadDDS(texPath);
                        tex.name = Path.GetFileName(texPath);
                        mat.SetTexture("_Specular", tex);
                    }
                }
            }

            if (skinned)
            {
                SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.bones = bones;
                smr.rootBone = rootBone;

                mesh.bindposes = bindposes;
                mesh.boneWeights = boneWeights.ToArray();

                smr.sharedMesh = mesh;
                if (collision) smr.sharedMaterial = mat;
                else smr.sharedMaterials = new UnityEngine.Material[] { mat, highlightMaterial };

                shape.smr = smr;
            }
            else
            {
                MeshFilter mf = go.AddComponent<MeshFilter>();
                mf.mesh = mesh;

                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                if (collision) mr.sharedMaterial = mat;
                else mr.sharedMaterials = new UnityEngine.Material[] { mat, highlightMaterial };

                shape.mr = mr;
            }

            go.transform.SetParent(goRoot.transform);
        }

#region import animation
        foreach (var a in scene.Animations)
        {
            if ((int)a.TicksPerSecond == 1)
            {
                EditorController.instance.Status("Can't load animation with custom frame rate", 1);
                break;
            }

            bool resampled = false;

            if (a.HasNodeAnimations)
            {
                PdxDataService.AnimationData adata = new PdxDataService.AnimationData();
#if UNITY_EDITOR
                print("fps: " + a.TicksPerSecond + ", duration: " + a.DurationInTicks);
#endif
                adata.fps = 15;// (float)a.TicksPerSecond;
                adata.sampleCount = (int)a.DurationInTicks;
                adata.length = adata.sampleCount / adata.fps;

                foreach (var nac in a.NodeAnimationChannels)
                {
                    resampled = nac.PositionKeyCount == adata.sampleCount+1 || nac.RotationKeyCount == adata.sampleCount+1 || nac.ScalingKeyCount == adata.sampleCount+1;

                    if (resampled) break;
                }

#if UNITY_EDITOR
                print("resampled " + resampled);
#endif

                foreach (var nac in a.NodeAnimationChannels)
                {
                    if (!bonesByName.ContainsKey(nac.NodeName)) continue;

                    var animation = new PdxDataService.AnimationData.Animation();
                    animation.name = nac.NodeName;

                    animation.parent = bonesByName[nac.NodeName];
                    animation.keys = new List<PdxDataService.AnimationData.Key>();
                    for (int i = 0; i < adata.sampleCount + 1; i++) animation.keys.Add(new PdxDataService.AnimationData.Key());
                    
                    animation.keys[0].pos = animation.parent.transform.localPosition;
                    animation.keys[0].rot = animation.parent.transform.localRotation;
                    animation.keys[0].scl = animation.parent.transform.localScale;

                    //animation.sampleT = true; // or joint mistmatch error

                    if (nac.HasPositionKeys)
                    {
                        if (resampled)
                        {
                            foreach(var pk in nac.PositionKeys)
                            {
                                int i = (int)pk.Time + 1;
                                if (i >= animation.keys.Count) break;
                                
                                animation.keys[i].pos = new Vector3(pk.Value.X, pk.Value.Y, pk.Value.Z);
                                animation.keys[i].time = (float)pk.Time / (float)adata.fps;
                            }
                        }
                        else
                        {
                            List<VectorKey> posKeys = nac.PositionKeys;
                            if (ListVectorKey(ref posKeys, adata.sampleCount))
                            {
                                animation.sampleT = true;

                                foreach (var pk in posKeys)
                                {
                                    int i = (int)pk.Time + 1;
                                    if (i >= animation.keys.Count) break;

                                    animation.keys[i].pos = new Vector3(pk.Value.X, pk.Value.Y, pk.Value.Z);
                                    animation.keys[i].time = (float)pk.Time / (float)adata.fps;
                                }
                            }
                        }
                    }

                    if (nac.HasRotationKeys)
                    {
                        if (resampled)
                        {
                            foreach (var rk in nac.RotationKeys)
                            {
                                int i = (int)rk.Time + 1;
                                if (i >= animation.keys.Count) break;
                                
                                animation.keys[i].rot = new UnityEngine.Quaternion(rk.Value.X, rk.Value.Y, rk.Value.Z, rk.Value.W);
                                animation.keys[i].time = (float)rk.Time / (float)adata.fps;
                            }
                        }
                        else
                        {
                            List<QuaternionKey> rotKeys = nac.RotationKeys;
                            if (ListQuaternionKey(ref rotKeys, adata.sampleCount))
                            {
                                animation.sampleQ = true;

                                foreach (var rk in rotKeys)
                                {
                                    int i = (int)rk.Time + 1;
                                    if (i >= animation.keys.Count) break;

                                    animation.keys[i].rot = new UnityEngine.Quaternion(rk.Value.X, rk.Value.Y, rk.Value.Z, rk.Value.W);
                                    animation.keys[i].time = (float)rk.Time / (float)adata.fps;
                                }
                            }
                        }
                    }

                    if (nac.HasScalingKeys)
                    {
                        if (resampled)
                        {
                            foreach (var sk in nac.ScalingKeys)
                            {
                                int i = (int)sk.Time + 1;
                                if (i >= animation.keys.Count) break;
                                
                                animation.keys[i].scl = new Vector3(sk.Value.X, sk.Value.Y, sk.Value.Z);
                                animation.keys[i].time = (float)sk.Time / (float)adata.fps;
                            }
                        }
                        else
                        {
                            List<VectorKey> sclKeys = nac.ScalingKeys;
                            if (ListVectorKey(ref sclKeys, adata.sampleCount))
                            {
                                animation.sampleS = true;

                                foreach (var sk in sclKeys)
                                {
                                    int i = (int)sk.Time + 1;
                                    if (i >= animation.keys.Count) break;

                                    animation.keys[i].scl = new Vector3(sk.Value.X, sk.Value.Y, sk.Value.Z);
                                    animation.keys[i].time = (float)sk.Time / (float)adata.fps;
                                }
                            }
                        }
                    }

                    adata.hierarchy.Add(animation);
                }

                var player = goRoot.AddComponent<PdxAnimationPlayer>();
                adata.Fix();
                player.animationData = adata;
            }
        }
#endregion

        EditorController.instance.SceneHasSnowMaterial(snow.Count > 0 ? snow.ToArray() : null);
        EditorController.instance.SceneHasColorMaterial(color.Count > 0 ? color.ToArray() : null);
        EditorController.instance.SceneHasAtlasMaterial(atlas.Count > 0 ? atlas.ToArray() : null);

        if (!path.ToLower().EndsWith(".obj"))
        {
            goRoot.transform.Translate(0, min, 0, Space.World);
            goRoot.transform.rotation = UnityEngine.Quaternion.Euler(90, 0, 0);
        }

        context.Dispose();

        return goRoot;
    }

    public void ExportMesh(GameObject go, string path)
    {
        Vector3 lastPos = go.transform.position;
        go.transform.position = Vector3.zero;

        Scene scene = new Scene();

        List<string> sameNames = new List<string>();
        List<Transform> children = new List<Transform>();
        Dictionary<Transform, Node> nodesByTransform = new Dictionary<Transform, Node>();
        
        GetAllChildren(children, go.transform);
        
        foreach (Transform child in children)
        {
            string nname = child.name;

            if (sameNames.Contains(nname)) nname += "_";
            else sameNames.Add(nname);

            Node node = new Node(nname);
            UnityEngine.Matrix4x4 m = child.localToWorldMatrix.inverse;
            node.Transform = new Assimp.Matrix4x4(
                m.m00, m.m01, m.m02, m.m03,
                m.m10, m.m11, m.m12, m.m13,
                m.m20, m.m21, m.m22, m.m23,
                m.m30, m.m31, m.m32, m.m33
            );

            nodesByTransform.Add(child, node);

            if (child == go.transform)
            {
                Node rootNode = new Node(Path.GetFileNameWithoutExtension(path));
                rootNode.Children.Add(node);
                scene.RootNode = rootNode;
            }
        }

        foreach (Transform child in children)
        {
            foreach(Transform c in child)
            {
                if(child == go.transform)
                {
                    scene.RootNode.Children.Add(nodesByTransform[c]);
                }
                else
                {
                    nodesByTransform[child].Children.Add(nodesByTransform[c]);
                }
            }
        }

        sameNames.Clear();

        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
        SkinnedMeshRenderer[] smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();

        int meshIndex = 0;

        foreach(var mf in mfs)
        {
            PdxShape shape = mf.GetComponent<PdxShape>();

            Assimp.Material mat = new Assimp.Material();
            mat.Name = shape.shader;

            TextureSlot diff = new TextureSlot();
            diff.FilePath = shape.diffuse;
            diff.TextureType = TextureType.Diffuse;
            diff.UVIndex = 0;
            //mat.TextureDiffuse = diff;
            mat.AddMaterialTexture(ref diff);

            TextureSlot norm = new TextureSlot();
            norm.FilePath = shape.normal;
            norm.TextureType = TextureType.Normals;
            norm.UVIndex = 0;
            //mat.TextureNormal = norm;
            mat.AddMaterialTexture(ref norm);

            TextureSlot spec = new TextureSlot();
            spec.FilePath = shape.specular;
            spec.TextureType = TextureType.Specular;
            spec.UVIndex = 0;
            //mat.TextureSpecular = spec;
            mat.AddMaterialTexture(ref spec);

            scene.Materials.Add(mat);

            Assimp.Mesh am = null;
            
            am = FromUnityMesh(mf.mesh, "pShape" + meshIndex, go.transform);

            if (sameNames.Contains(am.Name)) am.Name += "_";
            else sameNames.Add(am.Name);

            am.MaterialIndex = meshIndex;
            scene.Meshes.Add(am);

            nodesByTransform[mf.transform].MeshIndices.Add(meshIndex);
            meshIndex++;
        }

        foreach (var smr in smrs)
        {
            PdxShape shape = smr.GetComponent<PdxShape>();

            Assimp.Material mat = new Assimp.Material();
            mat.Name = shape.shader;

            TextureSlot diff = new TextureSlot();
            diff.FilePath = shape.diffuse;
            diff.TextureType = TextureType.Diffuse;
            diff.UVIndex = 0;
            //mat.TextureDiffuse = diff;
            mat.AddMaterialTexture(ref diff);

            TextureSlot norm = new TextureSlot();
            norm.FilePath = shape.normal;
            norm.TextureType = TextureType.Normals;
            norm.UVIndex = 0;
            //mat.TextureNormal = norm;
            mat.AddMaterialTexture(ref norm);

            TextureSlot spec = new TextureSlot();
            spec.FilePath = shape.specular;
            spec.TextureType = TextureType.Specular;
            spec.UVIndex = 0;
            //mat.TextureSpecular = spec;
            mat.AddMaterialTexture(ref spec);

            scene.Materials.Add(mat);
            
            Assimp.Mesh am = null;

            UnityEngine.Mesh baked = new UnityEngine.Mesh();
            smr.BakeMesh(baked);

            am = FromUnityMesh(baked/*smr.sharedMesh*/, "pShape" + meshIndex, go.transform);

            if (sameNames.Contains(am.Name)) am.Name += "_";
            else sameNames.Add(am.Name);

            am.MaterialIndex = meshIndex;
            scene.Meshes.Add(am);

            nodesByTransform[smr.transform].MeshIndices.Add(meshIndex);
            meshIndex++;
        }

        AssimpContext context = new AssimpContext();

        bool result = context.ExportFile(scene, path, "obj", PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipWindingOrder);

        context.Dispose();

        go.transform.position = lastPos;

        if (result)
        {
            EditorController.instance.Status("Object saved as " + path);
        }
        else
        {
            EditorController.instance.Status("Export failed :(", 2);
        }
    }

    Assimp.Mesh FromUnityMesh(UnityEngine.Mesh mesh, string _name, Transform rootGo, Transform[] bones = null)
    {
        UnityEngine.Matrix4x4 world = rootGo.transform.localToWorldMatrix;

        Assimp.Mesh assimpMesh = new Assimp.Mesh(_name, Assimp.PrimitiveType.Triangle);

        foreach(Vector3 v in mesh.vertices)
        {
            Vector3 vW = world.MultiplyPoint3x4(v);
            assimpMesh.Vertices.Add(new Vector3D(vW.x, vW.y, vW.z));
        }

        foreach (Vector3 n in mesh.normals)
        {
            Vector3 nW = world.MultiplyPoint3x4(n);
            assimpMesh.Normals.Add(new Vector3D(nW.x, nW.y, nW.z));
        }

        foreach (Vector2 u in mesh.uv)
        {
            assimpMesh.TextureCoordinateChannels[0].Add(new Vector3D(u.x, 1f - u.y, 0));
        }

        if(mesh.uv2 != null && mesh.uv2.Length > 0)
        {
            foreach (Vector2 u in mesh.uv2)
            {
                assimpMesh.TextureCoordinateChannels[1].Add(new Vector3D(u.x, 1f - u.y, 0));
            }
        }

        assimpMesh.SetIndices(mesh.triangles, 3);

        /*if(bones != null)
        {
            foreach(Transform b in bones)
            {
                Bone bone = new Bone();
                bone.Name = b.name;

                UnityEngine.Matrix4x4 m = b.worldToLocalMatrix.inverse;
                bone.OffsetMatrix = new Assimp.Matrix4x4(
                    m.m00, m.m01, m.m02, m.m03,
                    m.m10, m.m11, m.m12, m.m13,
                    m.m20, m.m21, m.m22, m.m23,
                    m.m30, m.m31, m.m32, m.m33
                );
                
                assimpMesh.Bones.Add(bone);
            }

            int vertId = 0;

            foreach(BoneWeight bw in mesh.boneWeights)
            {
                assimpMesh.Bones[bw.boneIndex0].VertexWeights.Add(new VertexWeight(vertId, bw.weight0));
                assimpMesh.Bones[bw.boneIndex1].VertexWeights.Add(new VertexWeight(vertId, bw.weight1));
                assimpMesh.Bones[bw.boneIndex2].VertexWeights.Add(new VertexWeight(vertId, bw.weight2));
                assimpMesh.Bones[bw.boneIndex3].VertexWeights.Add(new VertexWeight(vertId, bw.weight3));
                vertId++;
            }
        }*/

        return assimpMesh;
    }

    void GetAllChildren(List<Transform> tList, Transform t)
    {
        tList.Add(t);

        foreach(Transform child in t)
        {
            GetAllChildren(tList, child);
        }
    }

    bool ListVectorKey(ref List<VectorKey> list, int totalFrames)
    {
        bool hasChanges = false;

        for(int i = 0; i < list.Count; i++)
        {
            if(list[i] != list[GetNextIndex(list, i)] || list[i] != list[GetPrevIndex(list, i)])
            {
                hasChanges = true;
                break;
            }
        }

        if (!hasChanges) return false;

        List<VectorKey> interpolated = new List<VectorKey>(totalFrames);
        for (int i = 0; i < totalFrames; i++) interpolated.Add(new VectorKey());

        VectorKey first = list[0];
        VectorKey from;
        VectorKey to;
        
        for (int i = 0; i < list.Count; i++)
        {
            from = list[i];
            to = list[GetNextIndex(list, i)];

            if (Mathf.Abs(((int)to.Time - (int)from.Time)) == 1) continue;
            
            Vector3 a = new Vector3(from.Value.X, from.Value.Y, from.Value.Z);
            Vector3 b = new Vector3(to.Value.X, to.Value.Y, to.Value.Z);

            int stepCount = Mathf.Abs((int)to.Time - (int)from.Time);
            float step = 1f / stepCount;
            if (to.Time < from.Time) step = 1f / (float)(totalFrames - (int)from.Time + (int)to.Time);
            float t = 0f;
            
            for (int j = (int)from.Time; j != (int)to.Time; j = GetNextIndex(interpolated, j))
            {
                VectorKey ik = interpolated[j];
                ik.Time = j;
                Vector3 ab = Vector3.Lerp(a, b, t);
                ik.Value = new Vector3D(ab.x, ab.y, ab.z);
                interpolated[j] = ik;

                t += step;

                if (stepCount-- < 0) break;
            }
            
            if (to == first) break;
        }

        list = interpolated;

        return true;
    }

    bool ListQuaternionKey(ref List<QuaternionKey> list, int totalFrames)
    {
        bool hasChanges = false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != list[GetNextIndex(list, i)])
            {
                hasChanges = true;
                break;
            }
        }

        if (!hasChanges) return false;

        List<QuaternionKey> interpolated = new List<QuaternionKey>(totalFrames);
        for (int i = 0; i < totalFrames; i++) interpolated.Add(new QuaternionKey());

        QuaternionKey first = list[0];
        QuaternionKey from;
        QuaternionKey to;
        
        for (int i = 0; i < list.Count; i++)
        {
            from = list[i];
            to = list[GetNextIndex(list, i)];

            if (Mathf.Abs(((int)to.Time - (int)from.Time)) == 1) continue;

            bool aNan = (float.IsNaN(from.Value.X) || float.IsNaN(from.Value.Y) || float.IsNaN(from.Value.Z) || float.IsNaN(from.Value.W));
            bool bNan = (float.IsNaN(to.Value.X) || float.IsNaN(to.Value.Y) || float.IsNaN(to.Value.Z) || float.IsNaN(to.Value.W));

            UnityEngine.Quaternion a = aNan ? UnityEngine.Quaternion.identity : new UnityEngine.Quaternion(from.Value.X, from.Value.Y, from.Value.Z, from.Value.W);
            UnityEngine.Quaternion b = bNan ? UnityEngine.Quaternion.identity : new UnityEngine.Quaternion(to.Value.X, to.Value.Y, to.Value.Z, to.Value.W);

            int stepCount = Mathf.Abs((int)to.Time - (int)from.Time);
            float step = 1f / stepCount;
            if (to.Time < from.Time) step = 1f / (float)(totalFrames - (int)from.Time + (int)to.Time);
            float t = 0f;
            
            for (int j = (int)from.Time; j != (int)to.Time; j = GetNextIndex(interpolated, j))
            {
                QuaternionKey ik = interpolated[j];
                ik.Time = j;
                UnityEngine.Quaternion ab = UnityEngine.Quaternion.Lerp(a, b, t);
                ik.Value = new Assimp.Quaternion(ab.w, ab.x, ab.y, ab.z);
                //print(ik.Value);
                interpolated[j] = ik;

                t += step;

                if (stepCount-- < 0) break;
            }

            if (to == first) break;
        }

        list = interpolated;
        
        return true;
    }

    int GetNextIndex<T>(List<T> list, int index)
    {
        if (index + 1 > list.Count - 1) return 0;

        return index + 1;
    }

    int GetPrevIndex<T>(List<T> list, int index)
    {
        if (index - 1 < 0) return list.Count - 1;

        return index - 1;
    }

    void PrintHierarchy(Node node, int depth)
    {
        string tab = "";
        for (int i = 0; i < depth; i++) tab += '-';
        print(tab + node.Name);

        if(node.ChildCount > 0)
        {
            foreach (var c in node.Children)
            {
                PrintHierarchy(c, depth + 1);
            }
        }
    }

    void FindParentInHierarchy(string name, Node node, out string parent)
    {
        if (node.Name == name && node.Parent != null)
        {
            parent = node.Parent.Name;
            return;
        }
        else parent = "";

        if (node.ChildCount > 0)
        {
            foreach (var c in node.Children)
            {
                if (parent.Length > 0) continue;

                FindParentInHierarchy(name, c, out parent);
            }
        }
    }
}
