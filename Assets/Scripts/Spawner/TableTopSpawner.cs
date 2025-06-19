using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Per ogni anchor di superficie TABLE instanzia TUTTI i prefab della lista,
/// distribuiti in fila sul piano.
/// </summary>
public class TableTopSpawner : AnchorPrefabSpawner
{
    // distanza tra un oggetto e l’altro (in metri)
    [SerializeField] private float spacing = 0.25f;

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

        // larghezza disponibile
        float width = plane.width;
        float startX = center.x - (width * 0.5f) + spacing;

        // ciclo su tutti i prefab configurati nel gruppo
        foreach (var group in PrefabsToSpawn)
        {
            if ((group.Labels & MRUKAnchor.SceneLabels.TABLE) == 0) continue;

            float xPos = startX;
            foreach (var p in group.Prefabs)
            {
                // calcola offset laterale
                var bounds = Utilities.GetPrefabBounds(p);
                float halfW = bounds?.extents.x ?? 0.1f;
                var localPos = new Vector3(xPos + halfW, center.y, center.z);

                var go = Instantiate(p,
                        anchor.transform.TransformPoint(localPos),
                        rot,
                        anchor.transform);

                xPos += (halfW * 2f) + spacing;
            }
        }
    }
}
