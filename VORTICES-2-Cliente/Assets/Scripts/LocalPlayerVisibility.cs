using Mirror;
using UnityEngine;

// Oculta los renderers del avatar para el jugador local.
// Los otros clientes sí ven el modelo normalmente.
public class LocalPlayerVisibility : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }
}
