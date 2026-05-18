#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AddPanelDrawersEditor
{
    [MenuItem("Tools/Primora/Add PanelDrawers to Anchors")]
    public static void AddPanelDrawers()
    {
        string[] anchorNames = { "HandPanelAnchor", "SkillPanelAnchor", "TurnOrderPanelAnchor" };

        foreach (string anchorName in anchorNames)
        {
            var go = GameObject.Find(anchorName);
            if (go == null)
            {
                Debug.LogWarning($"[AddPanelDrawers] '{anchorName}' not found in scene.");
                continue;
            }

            var drawer = go.GetComponent<PanelDrawer>();
            if (drawer == null)
            {
                drawer = go.AddComponent<PanelDrawer>();
                Debug.Log($"[AddPanelDrawers] Added PanelDrawer to '{anchorName}'.");
            }
            else
            {
                Debug.Log($"[AddPanelDrawers] '{anchorName}' already has PanelDrawer.");
            }

            // Auto-wire _panel to the first non-OpenPosition child RectTransform
            var serialized = new SerializedObject(drawer);
            var panelProp = serialized.FindProperty("_panel");
            if (panelProp != null && panelProp.objectReferenceValue == null)
            {
                foreach (Transform child in go.transform)
                {
                    if (child.name == "OpenPosition") continue;
                    var rt = child.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        panelProp.objectReferenceValue = rt;
                        serialized.ApplyModifiedProperties();
                        Debug.Log($"[AddPanelDrawers] Wired _panel on '{anchorName}' to '{child.name}'.");
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(go);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AddPanelDrawers] Done.");
    }
}
#endif
