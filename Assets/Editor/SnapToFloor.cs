
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class SnapToFloor
{
    private const float RayHeight = 10000f;

    static SnapToFloor()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type != EventType.KeyDown ||
            e.keyCode != KeyCode.Space ||
            EditorGUIUtility.editingTextField)
            return;

        // Ctrl + Space = snap pivot
        // Space = snap object
        bool snapPivot = e.control;

        SnapSelectedObjects(snapPivot);

        e.Use();
    }

    [MenuItem("Tools/Snap To Floor/Snap Object %#&SPACE")]
    private static void SnapObjectMenu()
    {
        SnapSelectedObjects(false);
    }

    [MenuItem("Tools/Snap To Floor/Snap Pivot")]
    private static void SnapPivotMenu()
    {
        SnapSelectedObjects(true);
    }

    private static void SnapSelectedObjects(bool snapPivot)
    {
        Transform[] selected = Selection.transforms;

        if (selected.Length == 0)
            return;

        // Abaikan child jika parent-nya ikut dipilih
        List<Transform> roots = new List<Transform>();

        foreach (Transform t in selected)
        {
            bool hasSelectedParent = false;

            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (System.Array.Exists(
                    selected, s => s == p))
                {
                    hasSelectedParent = true;
                    break;
                }
            }

            if (!hasSelectedParent)
                roots.Add(t);
        }

        Undo.RecordObjects(
            roots.ConvertAll(t => (Object)t).ToArray(),
            snapPivot ? "Snap Pivot To Floor" : "Snap Object To Floor"
        );

        foreach (Transform t in roots)
        {
            if (!TryGetBounds(t, out Bounds bounds))
            {
                Debug.LogWarning(
                    $"SnapToFloor: {t.name} tidak memiliki Renderer atau Collider."
                );
                continue;
            }

            // Object mode memakai bagian bawah bounds.
            // Pivot mode memakai posisi pivot pada sumbu XZ.
            Vector3 target = snapPivot
                ? t.position
                : new Vector3(
                    bounds.center.x,
                    bounds.min.y,
                    bounds.center.z
                );

            Vector3 origin = new Vector3(
                target.x,
                Mathf.Max(bounds.max.y, target.y) + 1f,
                target.z
            );

            if (!TryGetFloor(
                origin, t, out RaycastHit hit))
            {
                Debug.LogWarning(
                    $"SnapToFloor: lantai tidak ditemukan untuk {t.name}."
                );
                continue;
            }

            Vector3 delta = hit.point - target;

            t.position += delta;
            EditorUtility.SetDirty(t);
        }

        SceneView.RepaintAll();
    }

    private static bool TryGetBounds(
        Transform root, out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>();

        bool found = false;
        bounds = new Bounds(root.position, Vector3.zero);

        foreach (Renderer r in renderers)
        {
            if (!r.enabled)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (found)
            return true;

        Collider[] colliders =
            root.GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            if (!c.enabled)
                continue;

            if (!found)
            {
                bounds = c.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        return found;
    }

    private static bool TryGetFloor(
        Vector3 origin,
        Transform root,
        out RaycastHit floorHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            RayHeight,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits, (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            // Jangan snap ke collider milik objek itu sendiri
            if (hitTransform == root ||
                hitTransform.IsChildOf(root))
                continue;

            floorHit = hit;
            return true;
        }

        floorHit = default;
        return false;
    }
}
