using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using SFB;

public class EditorController : MonoBehaviour
{
    public static EditorController instance;

    public Text textStatus;
    public Text meshPath;
    public Text animPath;
    public Text fbxPath;
    public Text matShader;
    public Text matDiffuse;
    public Text matNormal;
    public Text matSpecular;
    public Text bigStatus;
    public Text textAttach;

    public Button saveMeshButton;
    public Button saveAnimButton;
    public Button saveFbxButton;
    
    public GameObject plane;
    public GameObject grid;

    public Transform hierarchyPanel;
    public GameObject objectButtonPrefab;

    public GameObject jointPrefab;
    public Material boneMaterial;
    public Font defaultFont;
    public Transform canvas;
    public Transform canvasBones;
    public Transform canvasLocators;
    public Transform panelShaders;

    public GameObject locatorPrefab;

    [HideInInspector]
    public Transform attach;

    public GameObject attachButton;
    public GameObject shaderPanel;

    public Light mainLight;

    public Toggle toggleShadows;
    public Toggle toggleAA;
    public Toggle togglePlane;
    public Toggle toggleLocators;
    public Toggle toggleLocatorNames;
    public Toggle toggleReimport;

    public Toggle toggleBones;
    [HideInInspector]
    public bool showBones;

    public Toggle toggleBoneNames;
    [HideInInspector]
    public bool showBoneNames;

    public InputField customShader;
    public Slider snowSlider;
    public Slider priSlider;
    public Slider secSlider;
    public Slider terSlider;
    //public Slider cutSlider;
    public Slider halfSlider;

    public RawImage priImage;
    public RawImage secImage;
    public RawImage terImage;
    public RawImage halfImage;

    [HideInInspector]
    public PdxLoader loader;
    AssimpLoader assimpLoader;
    /*FBXExporterForUnity fbxExporter;
    FBXImporterForUnity fbxImporter;*/
    Material[] snowMaterials;
    Material[] colorMaterials;
    Material[] atlasMaterials;

    AnimationPanel animationPanel;

    bool firstHL = true;
    // Use this for initialization
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        loader = GetComponent<PdxLoader>();
        animationPanel = GetComponent<AnimationPanel>();
        assimpLoader = GetComponent<AssimpLoader>();
        /*fbxExporter = GetComponent<FBXExporterForUnity>();
        fbxImporter = GetComponent<FBXImporterForUnity>();*/

        toggleShadows.isOn = 1 == PlayerPrefs.GetInt("toggleshadows", 1);
        toggleAA.isOn = 1 == PlayerPrefs.GetInt("toggleaa", 1);
        togglePlane.isOn = 1 == PlayerPrefs.GetInt("toggleplane", 1);
        toggleLocators.isOn = 1 == PlayerPrefs.GetInt("togglelocators", 1);
        toggleLocatorNames.isOn = 1 == PlayerPrefs.GetInt("togglelocatornames", 1);
        toggleBoneNames.isOn = 1 == PlayerPrefs.GetInt("togglebonenames", 1);
        toggleBones.isOn = 1 == PlayerPrefs.GetInt("togglebones", 1);

        showBones = toggleBones.isOn;
        showBoneNames = toggleBoneNames.isOn;

        ToggleShadows();
        TooglePlane();
        ToggleAA();
        ToggleBoneNames();
        ToggleBones();
        ToggleLocatorNames();
        ToggleLocators();

        panelShaders.gameObject.SetActive(false);

        meshPath.text = animPath.text = fbxPath.text = "";
        saveAnimButton.interactable = saveMeshButton.interactable = saveFbxButton.interactable = false;

        bigStatus.gameObject.SetActive(false);

        attachButton.SetActive(false);
        snowSlider.gameObject.SetActive(false);
        shaderPanel.SetActive(false);
    }

    public void SceneHasSnowMaterial(Material[] _snowMaterials)
    {
        snowMaterials = _snowMaterials;

        if(snowMaterials == null)
        {
            snowSlider.gameObject.SetActive(false);
        }
        else
        {
            snowSlider.gameObject.SetActive(true);
            snowSlider.value = 0;
        }
    }

    public void SceneHasColorMaterial(Material[] _coloMaterials)
    {
        colorMaterials = _coloMaterials;

        if (colorMaterials == null)
        {
            if(atlasMaterials == null) shaderPanel.gameObject.SetActive(false);
        }
        else
        {
            shaderPanel.gameObject.SetActive(true);
        }
    }

    public void SceneHasAtlasMaterial(Material[] _atlasMaterials)
    {
        atlasMaterials = _atlasMaterials;

        if (atlasMaterials == null)
        {
            if (colorMaterials == null) shaderPanel.gameObject.SetActive(false);
        }
        else
        {
            shaderPanel.gameObject.SetActive(true);
        }
    }

    public void OnSnowChange()
    {
        if (snowMaterials == null) return;

        foreach(Material m in snowMaterials)
        {
            m.SetFloat("_Snow", snowSlider.value);
        }
    } 

    public void OnPrimaryChange()
    {
        if (colorMaterials == null) return;

        priImage.color = Color.HSVToRGB(priSlider.value / 360f, .5f, 1f); //IntColor((uint)priSlider.value);

        foreach (Material m in colorMaterials)
        {
            m.SetColor("_PrimaryColor", priImage.color);
        }
    }

    public void OnSecodaryChange()
    {
        if (colorMaterials == null) return;

        secImage.color = Color.HSVToRGB(secSlider.value / 360f, .5f, 1f); //IntColor((uint)secSlider.value);

        foreach (Material m in colorMaterials)
        {
            m.SetColor("_SecondaryColor", secImage.color);
        }
    }

    public void OnTertiaryChange()
    {
        if (colorMaterials == null) return;

        terImage.color = Color.HSVToRGB(terSlider.value / 360f, .5f, 1f); //IntColor((uint)terSlider.value);

        foreach (Material m in colorMaterials)
        {
            m.SetColor("_TertiaryColor", terImage.color);
        }
    }

    public void OnHalfChange()
    {
        if (atlasMaterials == null && colorMaterials == null) return;

        halfImage.color = Color.HSVToRGB(halfSlider.value / 360f, .5f, 1f); //IntColor((uint)terSlider.value);

        if(colorMaterials != null) foreach (Material m in colorMaterials)
        {
            m.SetColor("_AtlasHalfColor", halfImage.color);
        }

        if (atlasMaterials != null) foreach (Material m in atlasMaterials)
        {
            m.SetColor("_AtlasHalfColor", halfImage.color);
        }
    }

    /*Color32 IntColor(uint color)
    {
        Color32 c;
        c.b = (byte)((color) & 0xFF);
        c.g = (byte)((color >> 8) & 0xFF);
        c.r = (byte)((color >> 16) & 0xFF);
        c.a = 255;//(byte)((color >> 24) & 0xFF);
        return c;
    }*/

    /*public void OnCutoffChange()
    {
        if (atlasMaterials == null) return;

        foreach (Material m in atlasMaterials)
        {
            m.SetFloat("_AtlasCutoff", cutSlider.value);
        }
    }*/

    public void ClearAttach()
    {
        loader.ClearAttach();

        SetAttach(null);
    }

    public void SetAttach(Transform bone)
    {
        attach = bone;

        attachButton.SetActive(attach != null);

        if(attach != null)
        {
            textAttach.text = bone.name;
        }

        BoneJoint[] js = FindObjectsOfType<BoneJoint>();
        foreach (var j in js) j.Check(bone);
    }

    public void Status(string text, int mode = 0)//0 - normal, 1 - warning, 2 - error
    {
        textStatus.text = text;
        textStatus.GetComponent<Animator>().SetTrigger("blink");

        switch (mode)
        {
            case 0:
                textStatus.color = Color.white;
                break;

            case 1:
                textStatus.color = Color.yellow;
                break;

            case 2:
                textStatus.color = Color.red;
                break;
        }
    }

    IEnumerator AddShapesButtons()
    {
        yield return null;

        firstHL = true;

        PdxShape[] ss = FindObjectsOfType<PdxShape>();

        bool selectFirst = false;

        foreach (PdxShape s in ss)
        {
            GameObject ob = Instantiate(objectButtonPrefab);
            ob.transform.SetParent(hierarchyPanel);
            ObjectButton b = ob.GetComponent<ObjectButton>();
            b.SetObject(s);

            if (!selectFirst)
            {
                selectFirst = true;

                b.OnPointerClick(null);
            }
        }
    }

    public void OpenAttach()
    {
        StartCoroutine(_OpenMesh(attach));
    }

    public void OpenMesh()
    {
        StartCoroutine(_OpenMesh());
    }

    IEnumerator _OpenMesh(Transform _attach = null)
    {
        string lastPath = Application.dataPath;

        if (PlayerPrefs.HasKey("lastmeshpath")) lastPath = PlayerPrefs.GetString("lastmeshpath");

        string[] result = StandaloneFileBrowser.OpenFilePanel("Open EU4 object", lastPath, new[] { new ExtensionFilter("EU4 object ", "mesh") }, false);
        
        if (result.Length > 0)
        {
            if (_attach == null)
            {
                SetAttach(null);

                for (int i = 0; i < hierarchyPanel.childCount; i++) Destroy(hierarchyPanel.GetChild(i).gameObject);
                for (int i = 0; i < canvasLocators.childCount; i++) Destroy(canvasLocators.GetChild(i).gameObject);

                meshPath.text = animPath.text = fbxPath.text = "";
                saveAnimButton.interactable = saveMeshButton.interactable = saveFbxButton.interactable = false;
            }

            bigStatus.gameObject.SetActive(true);
            yield return null;
            
            PlayerPrefs.SetString("lastmeshpath", Path.GetDirectoryName(result[0]));
            PlayerPrefs.Save();
            
            loader.LoadMesh(result[0], _attach);
        }
        else
        {
            bigStatus.gameObject.SetActive(false);
            yield break;
        }
        
        if (loader.selectedObject == null)
        {
            meshPath.text = "";
        }
        else
        {
            Status(loader.selectedObject.name + " loaded!");

            if(loader.selectedObject.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                animPath.text = "animation can be loaded";
            }
            else
            {
                animPath.text = "object has no skeleton for animation";
            }

            meshPath.text = Path.GetFileName(result[0]);
            saveMeshButton.interactable = true;

            fbxPath.text = "can be saved as .obj";
            saveFbxButton.interactable = true;

            if (_attach == null)
            {
                ToggleLocators(); ToggleLocatorNames();

                animationPanel.Hide();
                
                StartCoroutine(AddShapesButtons());
            }
        }

        bigStatus.gameObject.SetActive(false);
    }

    public void OpenFbx()
    {
        StartCoroutine(_OpenFbx());
    }

    public IEnumerator _OpenFbx()
    {
        string lastPath = Application.dataPath;

        if (PlayerPrefs.HasKey("lastfbxpath")) lastPath = PlayerPrefs.GetString("lastfbxpath");

        string[] result = StandaloneFileBrowser.OpenFilePanel("Import model", lastPath, new[] { new ExtensionFilter("Supported formats ", "fbx", "obj"), new ExtensionFilter("OBJ file ", "obj") }, false);

        if (result.Length > 0)
        {
            for (int i = 0; i < hierarchyPanel.childCount; i++) Destroy(hierarchyPanel.GetChild(i).gameObject);
            for (int i = 0; i < canvasLocators.childCount; i++) Destroy(canvasLocators.GetChild(i).gameObject);

            meshPath.text = animPath.text = fbxPath.text = "";
            saveAnimButton.interactable = saveMeshButton.interactable = saveFbxButton.interactable = false;

            bigStatus.gameObject.SetActive(true);
            SetAttach(null);
            yield return null;
            
            meshPath.text = result[0].Replace('\\', '/');
            PlayerPrefs.SetString("lastfbxpath", Path.GetDirectoryName(result[0]));
            PlayerPrefs.Save();

            loader.ClearScene();

            GameObject imported = null;

            imported = assimpLoader.LoadMesh(result[0]);

            PdxAnimationPlayer p = null;

            if (imported != null)
            {
                SkinnedMeshRenderer[] smrs = imported.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in smrs)
                {
                    smr.gameObject.AddComponent<BoneViewer>();
                    //smr.sharedMaterials = new Material[] { smr.sharedMaterial, loader.materialHighlight };
                }

                MeshRenderer[] mrs = imported.GetComponentsInChildren<MeshRenderer>();

                foreach (var mr in mrs)
                {
                    mr.enabled = true;
                    //mr.sharedMaterials = new Material[] { mr.sharedMaterial, loader.materialHighlight };
                }

                loader.loadedObjects.Add(imported);
                loader.selectedObject = imported;

                p = imported.GetComponent<PdxAnimationPlayer>();
                animationPanel.SetPlayer(p);
            }
            
            StartCoroutine(AddShapesButtons());

            meshPath.text = "mesh can be saved for EU4";
            saveMeshButton.interactable = true;

            if (p != null)
            {
                animPath.text = "animation can be saved for EU4";
                saveAnimButton.interactable = true;
            }

            fbxPath.text = Path.GetFileName(result[0]);
            saveFbxButton.interactable = true;

            Status(fbxPath.text + " loaded!");
        }
        
        bigStatus.gameObject.SetActive(false);
    }

    public void ExportToObj()
    {
        string lastPath = Application.dataPath;

        if (PlayerPrefs.HasKey("lastexportpath")) lastPath = PlayerPrefs.GetString("lastexportpath");

        string[] result = StandaloneFileBrowser.OpenFolderPanel("Export OBJ to folder", lastPath, false);

        if (result.Length > 0)
        {
            PlayerPrefs.SetString("lastexportpath", Path.GetDirectoryName(result[0]));
            PlayerPrefs.Save();
            
            assimpLoader.ExportMesh(loader.selectedObject, result[0].TrimEnd('\\', '/') + Path.DirectorySeparatorChar + loader.selectedObject.name.Replace(".mesh", "") + ".obj");
        }
    }

    public void SaveMesh()
    {
        StartCoroutine(_SaveMesh());
    }

    public IEnumerator _SaveMesh()
    {
        if (loader.selectedObject == null)
        {
            meshPath.text = "No object for export";
        }
        else
        {
            string lastPath = Application.dataPath;

            if (PlayerPrefs.HasKey("lastsavemeshpath")) lastPath = PlayerPrefs.GetString("lastsavemeshpath");

            string[] result = StandaloneFileBrowser.OpenFolderPanel("Save .mesh to folder", lastPath, false);

            if (result.Length > 0)
            {
                bigStatus.gameObject.SetActive(true);
                yield return null;
                
                Vector3 position = loader.selectedObject.transform.position;
                loader.selectedObject.transform.position = Vector3.zero;

                PlayerPrefs.SetString("lastsavemeshpath", Path.GetDirectoryName(result[0]));
                PlayerPrefs.Save();

                loader.SaveMesh(result[0]);

                loader.selectedObject.transform.position = position;

                Status("Mesh saved as " + result[0] + ".mesh");
            }
        }
        
        bigStatus.gameObject.SetActive(false);
    }

    public void OpenAnim()
    {
        StartCoroutine(_OpenAnim());
    }

    public IEnumerator _OpenAnim()
    {
        if (loader.selectedObject == null)
        {
            animPath.text = "Load object first";
        }
        else
        {
            string lastPath = Application.dataPath;

            if (PlayerPrefs.HasKey("lastanimpath")) lastPath = PlayerPrefs.GetString("lastanimpath");

            string[] result = StandaloneFileBrowser.OpenFilePanel("Open EU4 animation", lastPath, new[] { new ExtensionFilter("EU4 animation ", "anim") }, false);

            if (result.Length > 0)
            {
                bigStatus.gameObject.SetActive(true);
                yield return null;
                
                PlayerPrefs.SetString("lastanimpath", Path.GetDirectoryName(result[0]));
                PlayerPrefs.Save();

                loader.LoadAnim(result[0]);

                animationPanel.SetPlayer(FindObjectOfType<PdxAnimationPlayer>());

                bigStatus.gameObject.SetActive(false);

                saveAnimButton.interactable = true;
                animPath.text = Path.GetFileName(result[0]);
                Status(animPath.text + " loaded!");
            }
        }
    }

    public void SaveAnim()
    {
        StartCoroutine(_SaveAnim());
    }

    public IEnumerator _SaveAnim()
    {
        if (loader.selectedObject == null)
        {
            animPath.text = "Object has no animation";
        }
        else
        {
            string lastPath = Application.dataPath;

            if (PlayerPrefs.HasKey("lastsaveanimpath")) lastPath = PlayerPrefs.GetString("lastsaveanimpath");

            string result = StandaloneFileBrowser.SaveFilePanel("Save EU4 animation to folder", lastPath, loader.selectedObject.name.Substring(0, loader.selectedObject.name.Length - 4), "anim");

            if (result.Length > 0)
            {
                bigStatus.gameObject.SetActive(true);
                yield return null;
                
                PlayerPrefs.SetString("lastsaveanimpath", Path.GetDirectoryName(result));
                PlayerPrefs.Save();

                loader.SaveAnim(result);

                bigStatus.gameObject.SetActive(false);

                Status("Animation saved as " + result);
            }
        }
    }

    public void ChangeShader()
    {
        if(loader.selectedObject != null) panelShaders.gameObject.SetActive(!panelShaders.gameObject.activeSelf);
    }

    public void CustomShader()
    {
        if (loader.selectedShape != null)
        {
            loader.selectedShape.ChangeShader(customShader.text);
        }

        ChangeShader();
    }

    public void ChangeShaderClick(string name)
    {
        if(loader.selectedShape != null)
        {
            loader.selectedShape.ChangeShader(name);
        }

        ChangeShader();
    }

    public void ChangeTexture(int id)
    {
        if (loader.selectedShape == null) return;

        string lastPath = Application.dataPath;

        if (PlayerPrefs.HasKey("lasttexpath")) lastPath = PlayerPrefs.GetString("lasttexpath");

        string[] result = StandaloneFileBrowser.OpenFilePanel("Choose texture", lastPath, new[] { new ExtensionFilter("DDS Texture", "dds") }, false);

        if (result.Length > 0)
        {
            switch (id)
            {
                case 0:
                    loader.selectedShape.SetDiffuse(result[0]);
                    matDiffuse.text = Path.GetFileName(loader.selectedShape.diffuse);
                    break;

                case 1:
                    loader.selectedShape.SetNormal(result[0]);
                    matNormal.text = Path.GetFileName(loader.selectedShape.normal);
                    break;

                case 2:
                    loader.selectedShape.SetSpecular(result[0]);
                    matSpecular.text = Path.GetFileName(loader.selectedShape.specular);
                    break;
            }

            PlayerPrefs.SetString("lasttexpath", Path.GetDirectoryName(result[0]));
            PlayerPrefs.Save();
        }
    }

    public void ToggleAA()
    {
        Camera.main.allowMSAA = toggleAA.isOn;

        PlayerPrefs.SetInt("toggleaa", toggleAA.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleShadows()
    {
        mainLight.shadows = toggleShadows.isOn ? LightShadows.Soft : LightShadows.None;

        PlayerPrefs.SetInt("toggleshadows", toggleShadows.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void TooglePlane()
    {
        plane.SetActive(togglePlane.isOn);
        grid.SetActive(!togglePlane.isOn);

        PlayerPrefs.SetInt("toggleplane", togglePlane.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleBones()
    {
        showBones = toggleBones.isOn;

        toggleBoneNames.interactable = showBones;

        PlayerPrefs.SetInt("togglebones", toggleBones.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleBoneNames()
    {
        showBoneNames = toggleBoneNames.isOn;

        PlayerPrefs.SetInt("togglebonenames", toggleBoneNames.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleLocators()
    {
        Locator[] ls = FindObjectsOfType<Locator>();

        foreach(var l in ls)
        {
            l.show = toggleLocators.isOn;
        }

        toggleLocatorNames.interactable = toggleLocators.isOn;

        PlayerPrefs.SetInt("togglelocators", toggleLocators.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleLocatorNames()
    {
        Locator[] ls = FindObjectsOfType<Locator>();

        foreach (var l in ls)
        {
            l.showName = toggleLocatorNames.isOn;
        }

        PlayerPrefs.SetInt("togglelocatornames", toggleLocatorNames.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SelectObjectInHierarchy(PdxShape go)
    {
        if (!firstHL)
        {
            go.Highlight();
        }

        firstHL = false;

        loader.selectedShape = go;

        matShader.text = go.shader;
        matDiffuse.text = string.IsNullOrEmpty(go.diffuse) ? "no diffuse" : Path.GetFileName(go.diffuse);
        matNormal.text = string.IsNullOrEmpty(go.normal) ? "no normal" : Path.GetFileName(go.normal);
        matSpecular.text = string.IsNullOrEmpty(go.specular) ? "no specular" : Path.GetFileName(go.specular);

        ObjectButton[] buttons = FindObjectsOfType<ObjectButton>();

        foreach(ObjectButton b in buttons)
        {
            b.AnotherButtonClick(go);
        }
    }
}
