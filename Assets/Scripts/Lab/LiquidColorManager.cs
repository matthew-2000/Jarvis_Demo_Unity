using System.Collections.Generic;
using UnityEngine;

public class LiquidColorManager : MonoBehaviour
{
    [System.Serializable]
    public class LiquidEntry
    {
        public Renderer targetRenderer;
        public Color topColor;
        public Color sideColor;
    }

    public List<LiquidEntry> liquids = new();

    void Start()
    {
        foreach (var entry in liquids)
        {
            if (entry.targetRenderer == null) continue;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            entry.targetRenderer.GetPropertyBlock(mpb);

            mpb.SetColor("_TopColor", entry.topColor);
            mpb.SetColor("_SideColor", entry.sideColor);

            entry.targetRenderer.SetPropertyBlock(mpb);
        }
    }
}
