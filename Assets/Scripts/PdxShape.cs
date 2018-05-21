using UnityEngine;
using System.Collections;
using System.IO;

public class PdxShape : MonoBehaviour
{
    public string shader;
    public string diffuse;
    public string normal;
    public string specular;

    public MeshRenderer mr;
    public SkinnedMeshRenderer smr;

    bool blinking = false;

    public void CopyDiffuse(string toFolder)
    {
        if (string.IsNullOrEmpty(diffuse)) return;

        try
        {
            if (File.Exists(diffuse))
            {
                File.Copy(diffuse, toFolder.Trim('/', '\\') + Path.DirectorySeparatorChar + Path.GetFileName(diffuse), true);
            }
        }
        catch(IOException)
        {
            //sharing
        }
    }

    public void CopyNormal(string toFolder)
    {
        if (string.IsNullOrEmpty(normal)) return;

        try
        {
            if (File.Exists(normal))
            {
                File.Copy(normal, toFolder.Trim('/', '\\') + Path.DirectorySeparatorChar + Path.GetFileName(normal), true);
            }
        }
        catch (IOException)
        {
            //sharing
        }
    }

    public void CopySpecular(string toFolder)
    {
        if (string.IsNullOrEmpty(specular)) return;

        try
        {
            if (File.Exists(specular))
            {
                File.Copy(specular, toFolder.Trim('/', '\\') + Path.DirectorySeparatorChar + Path.GetFileName(specular), true);
            }
        }
        catch (IOException)
        {
            //sharing
        }
    }

    public void SetDiffuse(string path)
    {
        Material mat = null;
         
        if(mr != null)
        {
            mat = mr.sharedMaterials[0];
        }
        else if(smr != null)
        {
            mat = smr.sharedMaterials[0];
        }

        if (mat == null) return;

        mat.SetTexture("_Diffuse", PdxLoader.LoadDDS(path));
        
        diffuse = path;
    }

    public void SetNormal(string path)
    {
        Material mat = null;

        if (mr != null)
        {
            mat = mr.sharedMaterials[0];
        }
        else if (smr != null)
        {
            mat = smr.sharedMaterials[0];
        }

        if (mat == null) return;

        mat.SetTexture("_Normal", PdxLoader.LoadDDS(path));
        
        normal = path;
    }

    public void SetSpecular(string path)
    {
        Material mat = null;

        if (mr != null)
        {
            mat = mr.sharedMaterials[0];
        }
        else if (smr != null)
        {
            mat = smr.sharedMaterials[0];
        }

        if (mat == null) return;

        mat.SetTexture("_Specular", PdxLoader.LoadDDS(path));
        
        specular = path;
    }

    public void ChangeShader(string shaderName)
    {
        shader = shaderName;
        EditorController.instance.matShader.text = shader;

        var mats = EditorController.instance.loader.pdxMaterials;
        Material[] newMats = null;
        Material[] oldMats = mr != null ? mr.sharedMaterials : smr.sharedMaterials;

        if (mats.ContainsKey(shaderName))
        {
            if (shaderName != "Collision")
            {
                newMats = new Material[] { mats[shaderName], EditorController.instance.loader.materialHighlight };
            }
            else
            {
                newMats = new Material[] { mats[shaderName] };
            }
        }
        else
        {
            newMats = new Material[] { mats["PdxMeshStandard"], EditorController.instance.loader.materialHighlight };
        }

        if(oldMats[0].HasProperty("_Diffuse") && newMats[0].HasProperty("_Diffuse")) newMats[0].SetTexture("_Diffuse", oldMats[0].GetTexture("_Diffuse"));
        if (oldMats[0].HasProperty("_Normal") && newMats[0].HasProperty("_Normal")) newMats[0].SetTexture("_Normal", oldMats[0].GetTexture("_Normal"));
        if (oldMats[0].HasProperty("_Specular") && newMats[0].HasProperty("_Specular")) newMats[0].SetTexture("_Specular", oldMats[0].GetTexture("_Specular"));

        if (mr != null)
        {
            mr.sharedMaterials = newMats;
        }
        else if (smr != null)
        {
            smr.sharedMaterials = newMats;
        }
    }

    public void Highlight()
    {
        if (blinking || (smr == null && mr == null) || (smr != null && smr.sharedMaterials.Length < 2) || (mr != null && mr.sharedMaterials.Length < 2)) return;
        
        Material mat = (mr == null) ? smr.sharedMaterials[1] : mr.sharedMaterials[1];

        StartCoroutine(BlinkHighlight(mat));
    }

    IEnumerator BlinkHighlight(Material mat)
    {
        blinking = true;

        Color from = mat.color;
        Color to = new Color(from.r, from.g, from.b, 1f);

        float t = 0f;

        for(int i = 0; i < 3; i++)
        {
            while (t < 1f)
            {
                yield return null;

                t += Time.deltaTime * 5f;
                mat.color = Color.Lerp(from, to, t);
            }

            mat.color = to;

            t = 1f;

            while (t > 0)
            {
                yield return null;

                t -= Time.deltaTime * 5f;
                mat.color = Color.Lerp(from, to, t);
            }

            mat.color = from;
        }

        blinking = false;
    }
}
