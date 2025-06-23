using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Per ogni anchor di superficie TABLE instanzia TUTTI i prefab della lista,
/// distribuiti in fila sul piano.
/// </summary>
public class TableTopSpawner : AnchorPrefabSpawner
{

    /* ------------- non usiamo più CustomPrefabSelection ------------- */
    public override GameObject CustomPrefabSelection(
        MRUKAnchor anchor, List<GameObject> prefabs) => null;

    /* ------------- disattiva auto-scaling ------------- */
    public override Vector3 CustomPrefabScaling(Vector3 _) => Vector3.one;
    public override Vector2 CustomPrefabScaling(Vector2 _) => Vector2.one;

    /* ------------- appoggiamo al piano (pivot al centro) ------------- */
    public override Vector3 CustomPrefabAlignment(
        Rect plane, Bounds? bounds)
    {
        float halfH = bounds?.extents.y ?? 0f;               // spessore prefab
        return new Vector3(plane.center.x, plane.center.y,   // centro tavolo
                           halfH);                           // alza di metà h
    }

    /* ------------- qui generiamo tutti i prefab ------------- */
    protected override void SpawnPrefab(MRUKAnchor anchor)
    {
        // solo plane-surface dei tavoli
        if (!anchor.PlaneRect.HasValue ||
            (anchor.Label & MRUKAnchor.SceneLabels.TABLE) == 0)
            return;

        var plane = anchor.PlaneRect.Value;
        var center = CustomPrefabAlignment(plane, null);

        float yaw = anchor.transform.rotation.eulerAngles.y;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        // Soglia minima di scala accettabile
        float minScale = 0.5f;

        // ciclo su tutti i prefab configurati nel gruppo
        foreach (var group in PrefabsToSpawn)
        {
            if ((group.Labels & MRUKAnchor.SceneLabels.TABLE) == 0) continue;

            foreach (var p in group.Prefabs)
            {
                // Ottieni i bounds del prefab
                var bounds = Utilities.GetPrefabBounds(p);
                if (bounds == null) continue;

                float prefabWidth = bounds.Value.size.x;
                float prefabDepth = bounds.Value.size.z;

                // Calcola la scala massima possibile per far entrare il prefab
                float scaleX = plane.width / prefabWidth;
                float scaleZ = plane.height / prefabDepth;
                float scale = Mathf.Min(scaleX, scaleZ, 1f); // Non scalare sopra 1

                Debug.Log($"Prefab: {p.name}, scaleX: {scaleX}, scaleZ: {scaleZ}, chosen scale: {scale}");

                if (scale >= minScale)
                {
                    // Posiziona il prefab perfettamente al centro della superficie, 5cm più in alto
                    var localPos = new Vector3(center.x, center.y, center.z + 0.05f);

                    var go = Instantiate(p,
                        anchor.transform.TransformPoint(localPos),
                        rot,
                        anchor.transform);

                    go.transform.localScale = go.transform.localScale * scale;

                    // Instanzia solo il primo prefab che ci sta
                    return;
                }
            }
        }
    }
}
