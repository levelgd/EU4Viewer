using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class PdxDataService
{
    [Serializable]
    public class AnimationData
    {
        [Serializable]
        public class Key
        {
            public float time;
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 scl;
        }

        [Serializable]
        public class Animation
        {
            public Transform parent;//bone index
            public string name;
            public List<Key> keys = new List<Key>();
            public bool sampleT;
            public bool sampleQ;
            public bool sampleS;
            public bool skipData;
        }

        public string name;
        public float fps;
        public float length;
        public int sampleCount;
        public List<Animation> hierarchy = new List<Animation>();

        public void Fix()
        {
            if(sampleCount == 0)
            {
                foreach (var a in hierarchy)
                {
                    sampleCount = sampleCount < a.keys.Count ? a.keys.Count : sampleCount;
                }

                sampleCount--;
            }
            
            foreach (var a in hierarchy)
            {
                a.sampleT = a.sampleQ = a.sampleS = false;

                bool first = true;

                foreach(var k in a.keys)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }

                    if(!a.sampleT) if (k.pos != NextKey(a, k).pos) a.sampleT = true;
                    if(!a.sampleQ) if (k.rot != NextKey(a, k).rot) a.sampleQ = true;
                    if(!a.sampleS) if (k.scl != NextKey(a, k).scl) a.sampleS = true;

                    if (a.sampleT && a.sampleQ && a.sampleS) break;
                }

                if (!a.sampleT && !a.sampleQ && !a.sampleS)
                {
                    a.keys = new List<Key>() { new Key()
                    {
                        pos = a.keys[0].pos,
                        rot = a.keys[0].rot,
                        scl = a.keys[0].scl,
                        time = 0
                    }};
                }
            }

            if (fps == 0) fps = 15;

            if (length == 0) length = (int)sampleCount / fps;
        }

        public Key NextKey(Animation a, Key current)
        {
            int i = a.keys.IndexOf(current);

            if (i + 1 >= a.keys.Count) return a.keys[1];

            return a.keys[i + 1];
        }

        public Key PrevKey(Animation a, Key current)
        {
            int i = a.keys.IndexOf(current);

            if (i - 1 < 0) return a.keys[a.keys.Count - 1];

            return a.keys[i - 1];
        }
    }
    
    public class Base
    {
        public struct Data {
            public int i; public float f; public string s;

            public static Data Int(int _i)
            {
                return new Data() { i = _i };
            }

            public static Data Float(float _f)
            {
                return new Data() { f = _f };
            }

            public static Data String(string _s)
            {
                return new Data() { s = _s };
            }
        }

        public string type;
        public string name;
        public Dictionary<string, Base> subNodes = new Dictionary<string, Base>();
        public int depth = 0;

        public List<Data> value = new List<Data>();
        public int offset;
        public int stringType;

        public bool nullByteString;

        public string nameToWrite;
        
        public void Add(Base node)
        {
            subNodes.Add(node.name, node);
        }

        public Base()
        {

        }

        public Base(string _name, string _type, Base _parent = null)
        {
            name = _name;
            type = _type;

            if (_parent != null)
            {
                while (_parent.subNodes.ContainsKey(name)) name += "*";
                _parent.Add(this);
            }
        }
    }

    public PdxDataService()
    {

    }

    public Base ReadFromBuffer(byte[] buffer)
    {
        if (buffer[2] == 't')
        {
            EditorController.instance.Status("Text format not supported", 1);
            return null; //text format
        }
        // Skip '@@b@' file type marker
        byte[] data = buffer;
        int offset = 4;

        Base _base = new Base()
        {
            type = "object",
            name = "pdxData",
            depth = 0
        };

        offset = ReadObject(_base, data, offset, -1);

        return _base;
    }

    int ReadObject(Base _object, byte[] data, int offset, int objectDepth)
    {
        int depth = 0;

        while(data[offset] == '[')
        {
            depth++;
            offset++;
        }

        if (depth <= objectDepth) return offset - depth;

        if(depth > 0)
        {
            string name = ReadNullByteString(data, offset);
            offset += name.Length + 1;

            Base newObject = new Base()
            {
                type = "object",
                name = name,
                depth = depth
            };

            string _name = name;  while (_object.subNodes.ContainsKey(_name)) _name += "*";
            
            _object.subNodes.Add(_name, newObject);
            _object = newObject;
        }

        while(offset < data.Length)
        {
            if(data[offset] == '!')
            {
                offset = ReadProperty(_object, data, offset);
            }
            else if (data[offset] == '[')
            {
                int newOffset = ReadObject(_object, data, offset, depth);

                if (newOffset == offset) break;

                offset = newOffset;
            }
            else
            {
                //UnityEngine.Debug.Log("Unknown object start byte " + data[offset] + " at " + offset);
                break;
            }
        }

        return offset;
    }

    int ReadProperty(Base _object, byte[] data, int offset)
    {
        offset++;

        int propertyNameLength = data[offset];
        offset++;

        string propertyName = ReadString(data, offset, propertyNameLength);
        offset += propertyNameLength;

        // Value
        Base property = ReadRawData(data, offset);
        property.name = propertyName;
        property.depth = _object.depth + 1;

        _object.subNodes.Add(propertyName, property);

        return property.offset;
    }

    string ReadString(byte[] data, int offset, int length)
    {
        var str = "";
        for (var i = 0; i < length; i++)
            str += (char)data[offset + i];

        return str;
    }

    string ReadNullByteString(byte[] data, int offset)
    {
        string str = "";

        while(data[offset] != 0 && offset < data.Length)
        {
            str += (char)data[offset];
            offset++;
        }

        return str;
    }

    Base ReadRawData(byte[] data, int offset)
    {
        Base result = new Base()
        {
            offset = 0,
            type = ""
        };

        if(data[offset] == 'i')
        {
            result.type = "int";
            offset++;
            
            int length = (int)BitConverter.ToUInt32(data, offset);
            offset += 4;

            if (length == 1) result.value.Add(new Base.Data() { i = BitConverter.ToInt32(data, offset) });
            else
            {
                for (int i = 0; i < length; i++)
                {
                    result.value.Add(new Base.Data() { i = BitConverter.ToInt32(data, offset + i * 4) });
                }
            }

            offset += 4 * length;
        }
        else if(data[offset] == 'f')
        {
            result.type = "float";

            offset++;

            int length = (int)BitConverter.ToUInt32(data, offset);
            offset += 4;

            if (length == 1) result.value.Add(new Base.Data() { f = BitConverter.ToSingle(data, offset) });
            else
            {
                for (int i = 0; i < length; i++) result.value.Add(new Base.Data() { f = BitConverter.ToSingle(data, offset + i * 4) });
            }
            offset += 4 * length;
        }
        else if (data[offset] == 's')
        {
            result.type = "string";

            offset++;
            
            result.stringType = data[offset];

            offset += 4;
            int strLength = (int)BitConverter.ToUInt32(data, offset);
            offset += 4;

            result.value.Add(new Base.Data() { s = ReadString(data, offset, strLength) });

            result.nullByteString = result.value[0].s[strLength - 1] == 0;

            if (result.nullByteString)
            {
                Base.Data d = result.value[0];
                d.s = d.s.Substring(0, strLength - 1);
                result.value[0] = d;
            }

            offset += strLength;
        }

        result.offset = offset;

        return result;
    }

    //WRITE

    class PdxBuffer
    {
        public int byteLength { get { return (int)current.BaseStream.Length; } }
        public BinaryWriter current;
        public int extendSize = 2048;
        public int objectDepth;
    }

    public void WriteToFile(Base data, string path)
    {
        using(BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create), System.Text.Encoding.ASCII))
        {
            PdxBuffer buffer = new PdxBuffer();
            buffer.current = writer;

            buffer.current.Write("@@b@".ToCharArray());
            WriteData(buffer, data);
        }
    }

    void WriteData(PdxBuffer buffer, Base data)
    {
        switch (data.type)
        {
            case "object":
                WriteObject(buffer, data);
                break;

            case "int":
                WriteIntProperty(buffer, data);
                break;

            case "float":
                WriteFloatProperty(buffer, data);
                break;

            case "string":
                WriteStringProperty(buffer, data);
                break;
        }
    }

    void WriteObject(PdxBuffer buffer, Base objectData)
    {
        //Debug.Log("WriteObject " + objectData.name + ": subnodes count = " + objectData.subNodes.Count + ", depth " + buffer.objectDepth);

        if (buffer.objectDepth > 20)
        {
            Debug.LogError("dead loop on " + objectData.name);
            return;
        }

        for (int i = 0; i < buffer.objectDepth; i++)
        {
            buffer.current.Write('[');
        }

        if(buffer.objectDepth > 0)
        {
            buffer.current.Write(objectData.name.TrimEnd('*').ToCharArray()); buffer.current.Write((byte)0);
        }
        buffer.objectDepth++;

        int l = objectData.subNodes.Count;

        List<Base> bases = new List<Base>(objectData.subNodes.Values);

        for (int i = 0; i < l; i++)
        {
            //objectData.subNodes.Values.
            WriteData(buffer, bases[i]);
        }

        buffer.objectDepth--;
    }

    void WriteIntProperty(PdxBuffer buffer, Base data)
    {
        //Debug.Log("WriteIntProperty " + data.name + ": value count = " + data.value.Count);

        buffer.current.Write('!');
        //writeFixedString
        char[] chars = data.name.ToCharArray();
        buffer.current.Write((byte)chars.Length); buffer.current.Write(chars);

        buffer.current.Write('i');

        int l = data.value.Count;
        buffer.current.Write(l);

        for (int i = 0; i < l; i++)
        {
            buffer.current.Write(data.value[i].i);
        }
    }

    void WriteFloatProperty(PdxBuffer buffer, Base data)
    {
        //Debug.Log("WriteFloatProperty " + data.name + ": value count = " + data.value.Count);

        buffer.current.Write('!');
        //writeFixedString
        char[] chars = data.name.ToCharArray();
        buffer.current.Write((byte)chars.Length); buffer.current.Write(chars);

        buffer.current.Write('f');
        
        int l = data.value.Count;
        buffer.current.Write(l);

        for (int i = 0; i < l; i++)
        {
            buffer.current.Write(data.value[i].f);
        }
    }

    void WriteStringProperty(PdxBuffer buffer, Base data)
    {
        //Debug.Log("WriteStringProperty " + data.name + ": value count = " + data.value.Count);

        buffer.current.Write('!');
        //writeFixedString
        char[] chars = data.name.ToCharArray();
        buffer.current.Write((byte)chars.Length); buffer.current.Write(chars);

        buffer.current.Write('s');

        if (data.stringType > 0) buffer.current.Write(data.stringType);
        else buffer.current.Write(1);

        char[] charsVal = data.value[0].s.ToCharArray();
        int l = charsVal.Length + (data.nullByteString ? 1 : 0);
        buffer.current.Write(l);

        buffer.current.Write(charsVal);
        if (data.nullByteString) buffer.current.Write((byte)0);
    }

    //
    public Base BaseFromAnimation(AnimationData animation)
    {
        if (animation == null) return null;

        Base _base = new Base("PdxData", "object");

        Base _pdxasset = new Base("pdxasset", "int", _base);
        _pdxasset.value = new List<Base.Data>() { Base.Data.Int(1), Base.Data.Int(0) };

        Base _info = new Base("info", "object", _base);

        Base _fps = new Base("fps", "float", _info);
        Base _sa = new Base("sa", "int", _info);
        Base _j = new Base("j", "int", _info);

        _fps.value.Add(Base.Data.Float(animation.fps));
        _sa.value.Add(Base.Data.Int(animation.sampleCount));
        _j.value.Add(Base.Data.Int(animation.hierarchy.Count));

        foreach(var node in animation.hierarchy)
        {
            Base _node = new Base(node.name, "object", _info);

            Base _tqs = new Base("sa", "string", _node); _tqs.nullByteString = true;
            Base _t = new Base("t", "float", _node);
            Base _q = new Base("q", "float", _node);
            Base _s = new Base("s", "float", _node);

            string tqs = "";
            if (node.sampleT) tqs += 't';
            if (node.sampleQ) tqs += 'q';
            if (node.sampleS) tqs += 's';

            _tqs.value.Add(Base.Data.String(tqs));

            Vector3 t = node.keys[0].pos;
            Quaternion q = node.keys[0].rot;
            Vector3 s = node.keys[0].scl;

            _t.value.Add(Base.Data.Float(t.x)); _t.value.Add(Base.Data.Float(t.y)); _t.value.Add(Base.Data.Float(t.z));
            _q.value.Add(Base.Data.Float(q.x)); _q.value.Add(Base.Data.Float(q.y)); _q.value.Add(Base.Data.Float(q.z)); _q.value.Add(Base.Data.Float(q.w));
            _s.value.Add(Base.Data.Float(s.x));
        }

        Base _samples = new Base("samples", "object", _base);

        Base _st = new Base("t", "float", _samples);
        Base _sq = new Base("q", "float", _samples);
        Base _ss = new Base("s", "float", _samples);

        bool isT = false;
        bool isQ = false;
        bool isS = false;

        for(int i = 1; i <= animation.sampleCount; i++)
        {
            foreach (var node in animation.hierarchy)
            {
                isT = isT || node.sampleT;
                isQ = isQ || node.sampleQ;
                isS = isS || node.sampleS;

                if (i >= node.keys.Count) continue;
                
                var frame = node.keys[i];
                
                if (node.sampleT)
                {
                    _st.value.Add(Base.Data.Float(frame.pos.x)); _st.value.Add(Base.Data.Float(frame.pos.y)); _st.value.Add(Base.Data.Float(frame.pos.z));
                }

                if (node.sampleQ)
                {
                    _sq.value.Add(Base.Data.Float(frame.rot.x)); _sq.value.Add(Base.Data.Float(frame.rot.y)); _sq.value.Add(Base.Data.Float(frame.rot.z));
                    _sq.value.Add(Base.Data.Float(frame.rot.w));
                }

                if (node.sampleS)
                {
                    _ss.value.Add(Base.Data.Float(frame.scl.x));
                }
            }
        }

        if (!isT) _samples.subNodes.Remove("t");
        if (!isQ) _samples.subNodes.Remove("q");
        if (!isS) _samples.subNodes.Remove("s");

        return _base;
    }

    public Base BaseFromGameObject(GameObject target)
    {
        Matrix4x4 world = target.transform.localToWorldMatrix;

        Vector3 lastPos = target.transform.position;

        PdxAnimationPlayer player = target.GetComponent<PdxAnimationPlayer>();
        if (player != null) player.SetTpose();

        target.transform.position = Vector3.zero;

        List<Transform> shapes = new List<Transform>();
        FindTagInChildren(shapes, target.transform, "Shape", target.transform);

        Base _base = new Base("PdxData", "object");

        Base _pdxasset = new Base("pdxasset", "int", _base);
        _pdxasset.value = new List<Base.Data>() { Base.Data.Int(1), Base.Data.Int(0) };

        Base _object = new Base("object", "object", _base);

        foreach (Transform sh in shapes)
        {
            MeshFilter mf = sh.GetComponent<MeshFilter>();
            MeshRenderer mr = sh.GetComponent<MeshRenderer>();
            SkinnedMeshRenderer smr = sh.GetComponent<SkinnedMeshRenderer>();

            PdxShape shapeData = sh.GetComponent<PdxShape>();

            bool isCollider = sh.gameObject.tag == "Collider";
            bool skinned = smr != null;

            Mesh mesh;
            Material mat;
            if (isCollider)
            {
                mesh = mf.mesh;
                mat = null;
            }
            else if (skinned)
            {
                mesh = smr.sharedMesh;
                mat = smr.material;
            }
            else
            {
                mesh = mf.mesh;
                mat = mr.material;
            }

            Base _shape = _object.subNodes.ContainsKey(sh.name) ? _object.subNodes[sh.name] : new Base(sh.name.Replace(" Instance", ""), "object", _object);

            Base _mesh = new Base("mesh", "object", _shape);

            Base _p = new Base("p", "float", _mesh);
            foreach (Vector3 p in mesh.vertices)
            {
                Vector3 pW = world.MultiplyPoint3x4(p);
                _p.value.Add(Base.Data.Float(pW.x)); _p.value.Add(Base.Data.Float(pW.y)); _p.value.Add(Base.Data.Float(pW.z));
            }

            if (!isCollider)
            {
                Base _n = new Base("n", "float", _mesh);
                foreach (Vector3 n in mesh.normals)
                {
                    Vector3 nW = world.MultiplyPoint3x4(n);
                    _n.value.Add(Base.Data.Float(nW.x)); _n.value.Add(Base.Data.Float(nW.y)); _n.value.Add(Base.Data.Float(nW.z));
                }

                Base _ta = new Base("ta", "float", _mesh);
                foreach (Vector4 ta in mesh.tangents)
                {
                    Vector4 taW = world * ta;
                    _ta.value.Add(Base.Data.Float(taW.x)); _ta.value.Add(Base.Data.Float(taW.y)); _ta.value.Add(Base.Data.Float(taW.z)); _ta.value.Add(Base.Data.Float(taW.w));
                }

                Base _uv = new Base("u0", "float", _mesh);
                foreach (Vector2 uv in mesh.uv)
                {
                    _uv.value.Add(Base.Data.Float(uv.x)); _uv.value.Add(Base.Data.Float(uv.y));
                }

                if (mesh.uv2 != null && mesh.uv2.Length > 0)
                {
                    Base _uv1 = new Base("u1", "float", _mesh);
                    foreach (Vector2 uv2 in mesh.uv2)
                    {
                        _uv1.value.Add(Base.Data.Float(uv2.x)); _uv1.value.Add(Base.Data.Float(uv2.y));
                    }
                }
            }

            Base _tri = new Base("tri", "int", _mesh);
            foreach (int tri in mesh.triangles)
            {
                _tri.value.Add(Base.Data.Int(tri));
            }

            Base _aabb = new Base("aabb", "object", _mesh);

            Base _min = new Base("min", "float", _aabb);

            _min.value.Add(Base.Data.Float(mesh.bounds.min.x));
            _min.value.Add(Base.Data.Float(mesh.bounds.min.y));
            _min.value.Add(Base.Data.Float(mesh.bounds.min.z));

            Base _max = new Base("max", "float", _aabb);

            _max.value.Add(Base.Data.Float(mesh.bounds.max.x));
            _max.value.Add(Base.Data.Float(mesh.bounds.max.y));
            _max.value.Add(Base.Data.Float(mesh.bounds.max.z));

            Base _material = new Base("material", "object", _mesh);

            Base _shader = new Base("shader", "string", _material); _shader.nullByteString = true;
            
            if (!isCollider)
            {
                _shader.value.Add(Base.Data.String(shapeData.shader));

                if (!string.IsNullOrEmpty(shapeData.diffuse))
                {
                    Base _diff = new Base("diff", "string", _material); _diff.nullByteString = true;
                    _diff.value.Add(Base.Data.String(Path.GetFileName(shapeData.diffuse)));
                }

                if (!string.IsNullOrEmpty(shapeData.normal))
                {
                    Base _normal = new Base("n", "string", _material); _normal.nullByteString = true;
                    _normal.value.Add(Base.Data.String(Path.GetFileName(shapeData.normal)));
                }

                if (!string.IsNullOrEmpty(shapeData.specular))
                {
                    Base _spec = new Base("spec", "string", _material); _spec.nullByteString = true;
                    _spec.value.Add(Base.Data.String(Path.GetFileName(shapeData.specular)));
                }
            }
            else
            {
                _shader.value.Add(Base.Data.String("Collision"));
            }

            if (skinned)
            {
                Dictionary<Transform, int> newBoneIndices = new Dictionary<Transform, int>();
                Dictionary<int, Transform> oldBoneIndices = new Dictionary<int, Transform>();

                if (!_shape.subNodes.ContainsKey("skeleton"))
                {
                    Transform root = null;

                    Base _skeleton = new Base("skeleton", "object", _shape);
                    Dictionary<string, Base> nameBones = new Dictionary<string, Base>();

                    foreach (Transform b in smr.bones)
                    {
                        oldBoneIndices.Add(Array.IndexOf(smr.bones, b), b);

                        if (b.name.ToLower().Equals("root")) root = b;
                        Base _bone = new Base(b.name, "object");
                        Base _btx = new Base("tx", "float", _bone);

                        Matrix4x4 m = b.localToWorldMatrix.inverse;

                        _btx.value = new List<Base.Data>()
                        {
                            new Base.Data() { f = m.m00 },new Base.Data() { f = m.m10 },new Base.Data() { f = m.m20 },//new Base.Data() { f = m.m03 },
                            new Base.Data() { f = m.m01 },new Base.Data() { f = m.m11 },new Base.Data() { f = m.m21 },//new Base.Data() { f = m.m13 },
                            new Base.Data() { f = m.m02 },new Base.Data() { f = m.m12 },new Base.Data() { f = m.m22 },//new Base.Data() { f = m.m23 },
                            new Base.Data() { f = m.m03 },new Base.Data() { f = m.m13 },new Base.Data() { f = m.m23 },//new Base.Data() { f = m.m33 },
                        };

                        nameBones.Add(b.name, _bone);
                    }

                    List<Transform> hierBones = new List<Transform>();
                    ObjectHierarchy(hierBones, root);

                    foreach (var hb in hierBones)
                    {
                        newBoneIndices.Add(hb, hierBones.IndexOf(hb));

                        Base _bone = nameBones[hb.name];
                        Base _bix = new Base("ix", "int", _bone);
                        _bix.value.Add(Base.Data.Int(hierBones.IndexOf(hb)));
                        _skeleton.Add(_bone);

                        if (hb != smr.rootBone)
                        {
                            Base _bpa = new Base("pa", "int", _bone);
                            _bpa.value.Add(Base.Data.Int(hierBones.IndexOf(hb.parent)));
                        }
                    }
                }

                Base _skin = new Base("skin", "object", _mesh);

                Base _bonesPerVert = new Base("bones", "int", _skin);

                int num = 1;

                foreach (BoneWeight bw in mesh.boneWeights)
                {
                    if (num < 2 && bw.weight1 > 0)
                    {
                        num = 2;
                    }
                    if (num < 3 && bw.weight2 > 0)
                    {
                        num = 3;
                    }
                    if (num < 4 && bw.weight3 > 0)
                    {
                        num = 4;
                        break;
                    }
                }

                _bonesPerVert.value.Add(Base.Data.Int(num));

                int ixCount = mesh.vertexCount;

                Base _ix = new Base("ix", "int", _skin);
                Base _w = new Base("w", "float", _skin);

                for (int i = 0; i < ixCount; i++)
                {
                    BoneWeight w = mesh.boneWeights[i];

                    int bi0 = newBoneIndices[oldBoneIndices[w.boneIndex0]];
                    int bi1 = newBoneIndices[oldBoneIndices[w.boneIndex1]];
                    int bi2 = newBoneIndices[oldBoneIndices[w.boneIndex2]];
                    int bi3 = newBoneIndices[oldBoneIndices[w.boneIndex3]];

                    _ix.value.Add(Base.Data.Int((bi0 == 0 && w.weight0 == 0) ? -1 : bi0));
                    _ix.value.Add(Base.Data.Int((bi1 == 0 && w.weight1 == 0) ? -1 : bi1));
                    _ix.value.Add(Base.Data.Int((bi2 == 0 && w.weight2 == 0) ? -1 : bi2));
                    _ix.value.Add(Base.Data.Int((bi3 == 0 && w.weight3 == 0) ? -1 : bi3));

                    _w.value.Add(Base.Data.Float(w.weight0));
                    _w.value.Add(Base.Data.Float(w.weight1));
                    _w.value.Add(Base.Data.Float(w.weight2));
                    _w.value.Add(Base.Data.Float(w.weight3));
                }
            }
        }
        
        Base _locator = new Base("locator", "object", _base);

        List<Transform> locators = new List<Transform>();
        FindTagInChildren(locators, target.transform, "Locator", target.transform);

        foreach(Transform loc in locators)
        {
            Base _loc = new Base(loc.name, "object", _locator);

            Base _p = new Base("p", "float", _loc);
            Base _q = new Base("q", "float", _loc);
            Base _pa = new Base("pa", "string", _loc); _pa.nullByteString = true;

            Vector3 pos = loc.transform.localPosition;
            Quaternion rot = loc.transform.localRotation;

            _p.value.Add(Base.Data.Float(pos.x)); _p.value.Add(Base.Data.Float(pos.y)); _p.value.Add(Base.Data.Float(pos.z));

            _q.value.Add(Base.Data.Float(rot.x)); _q.value.Add(Base.Data.Float(rot.y)); _q.value.Add(Base.Data.Float(rot.z)); _q.value.Add(Base.Data.Float(rot.w));

            _pa.value.Add(Base.Data.String(loc.transform.parent.name));
        }

        target.transform.position = lastPos;

        return _base;
    }

    void FindTagInChildren(List<Transform> tList, Transform t, string tagToFind, Transform excludeRoot = null)
    {
        if(tagToFind == t.tag && t != excludeRoot && !tList.Contains(t)) tList.Add(t);

        foreach (Transform child in t)
        {
            FindTagInChildren(tList, child, tagToFind);
        }
    }

    void ObjectHierarchy(List<Transform> hList, Transform t, Transform excludeRoot = null)
    {
        if (t != excludeRoot) hList.Add(t);

        foreach (Transform child in t)
        {
            ObjectHierarchy(hList, child, excludeRoot);
        }
    }
}
