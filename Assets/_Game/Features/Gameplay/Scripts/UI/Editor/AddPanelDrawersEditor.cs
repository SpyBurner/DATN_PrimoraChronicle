#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            var serialized = new SerializedObject(drawer);

            // Auto-wire _panel to the first non-OpenPosition child RectTransform
            var panelProp = serialized.FindProperty("_panel");
            RectTransform panelRt = null;
            if (panelProp != null && panelProp.objectReferenceValue == null)
            {
                foreach (Transform child in go.transform)
                {
                    if (child.name == "OpenPosition") continue;
                    var rt = child.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        panelProp.objectReferenceValue = rt;
                        panelRt = rt;
                        Debug.Log($"[AddPanelDrawers] Wired _panel on '{anchorName}' to '{child.name}'.");
                        break;
                    }
                }
            }
            else if (panelProp != null)
            {
                panelRt = panelProp.objectReferenceValue as RectTransform;
            }

            // Auto-wire _toggle to Toggle_Sidebar inside the panel
            var toggleProp = serialized.FindProperty("_toggle");
            if (toggleProp != null && toggleProp.objectReferenceValue == null && panelRt != null)
            {
                var toggleTransform = panelRt.transform.Find("Toggle_Sidebar");
                if (toggleTransform != null)
                {
                    var tog = toggleTransform.GetComponent<UnityEngine.UI.Toggle>();
                    if (tog != null)
                    {
                        toggleProp.objectReferenceValue = tog;
                        Debug.Log($"[AddPanelDrawers] Wired _toggle on '{anchorName}' to 'Toggle_Sidebar'.");
                    }
                    else
                    {
                        Debug.LogWarning($"[AddPanelDrawers] No Toggle component on 'Toggle_Sidebar' in '{anchorName}'.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AddPanelDrawers] 'Toggle_Sidebar' child not found inside panel on '{anchorName}'.");
                }
            }

            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(go);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AddPanelDrawers] Done.");
    }

    [MenuItem("Tools/Primora/Apply PanelDrawer Prefabs and Remove from Scene")]
    public static void ApplyAndRemove()
    {
        string[] anchorNames = { "HandPanelAnchor", "SkillPanelAnchor", "TurnOrderPanelAnchor" };

        foreach (string anchorName in anchorNames)
        {
            var go = GameObject.Find(anchorName);
            if (go == null)
            {
                Debug.LogWarning($"[AddPanelDrawers] '{anchorName}' not found in scene — skipping.");
                continue;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
                Debug.Log($"[AddPanelDrawers] Applied overrides to prefab for '{anchorName}'.");
            }

            Object.DestroyImmediate(go);
            Debug.Log($"[AddPanelDrawers] Removed '{anchorName}' from scene.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[AddPanelDrawers] Apply and remove done.");
    }
}
#endif
