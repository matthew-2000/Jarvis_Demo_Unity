using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Collections.Generic;

public class TableSurfaceSpawner : AnchorPrefabSpawner
{
    /* 1️⃣  instanzia solo se esiste PlaneRect */
    public override GameObject CustomPrefabSelection(
        MRUKAnchor anchor, List<GameObject> prefabs)
        => anchor.PlaneRect.HasValue ? prefabs[0] : null;

    /* 2️⃣  nessun auto-scaling */
    public override Vector3 CustomPrefabScaling(Vector3 _) => Vector3.one;
    public override Vector2 CustomPrefabScaling(Vector2 _) => Vector2.one;

    /* 3️⃣  alza di metà altezza */
    public override Vector3 CustomPrefabAlignment(
        Rect planeRect, Bounds? prefabBounds)
    {
        float h = prefabBounds?.extents.y ?? 0f;
        return new Vector3(planeRect.center.x, planeRect.center.y, h);
    }

    /* 4️⃣  raddrizza il prefab (toglie pitch/roll, lascia yaw) */
    protected override void SpawnPrefab(MRUKAnchor anchor)
    {
        base.SpawnPrefab(anchor);

        if (!anchor.PlaneRect.HasValue) return;
        if (!AnchorPrefabSpawnerObjects.TryGetValue(anchor, out var go)) return;

        // Yaw reale del tavolo (attorno all’up del mondo)
        float yaw = anchor.transform.rotation.eulerAngles.y;

        // resetta rotazione locale e ri-applica solo lo yaw
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
    }
}
