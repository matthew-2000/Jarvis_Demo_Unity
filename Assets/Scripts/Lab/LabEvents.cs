using System;
using UnityEngine;

public static class LabEvents
{
    /// <summary>
    /// childId = oggetto agganciato (es. "P1")  
    /// parentId = snap-zone / supporto (es. "S1")
    /// </summary>
    public static event Action<string, string> ObjectAttached;

    /// <summary>
    /// Chiamare questo metodo per notificare l’aggancio.
    /// </summary>
    public static void RaiseObjectAttached(string childId, string parentId)
        => ObjectAttached?.Invoke(childId, parentId);
}
