using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BoneViewer))]
public class BoneViewerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    void OnSceneGUI()
    {
        BoneViewer boneViewer = target as BoneViewer;

        if (!boneViewer.showInEditor) return;

        Transform[] bones = boneViewer.bones;

        foreach (Transform bone in bones)
        {
            if (!bone.gameObject.activeInHierarchy) continue;

            if(bone.parent != null)
            {
                Vector3 pp = bone.parent.position;
                Vector3 r = Vector3.Cross(bone.parent.position, bone.position).normalized * 0.05f; //Camera.current.transform.right * .005f;

                Handles.DrawPolyLine(pp + r, bone.position, pp - r, pp + r);

                //if (self.drawBoneNames)
                //{
                Vector2 screenPoint = HandleUtility.WorldToGUIPoint(bone.position);

                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(screenPoint.x, screenPoint.y, 128, 32));
                GUILayout.Label("●" + bone.name, boneViewer.style);
                GUILayout.EndArea();
                Handles.EndGUI();
                //}
            }
        }
    }
}