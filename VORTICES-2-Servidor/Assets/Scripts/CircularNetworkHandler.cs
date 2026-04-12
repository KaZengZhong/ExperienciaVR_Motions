using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Vortices;
using System.Linq;
using Vuplex.WebView;
using UnityEngine.UI;

public class CircularNetworkHandler : NetworkBehaviour
{
    public static CircularNetworkHandler Instance { get; private set; }

    private RighthandTools righthandTools;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        Debug.Log("[Cliente] Buscando CircularBase local...");
        
        DontDestroyOnLoad(gameObject);

        StartCoroutine(WaitForCircularBase());
    }

    //  Sincronización de URLs
    public void OnElementUrlChanged(Element element, string newUrl)
    {
        Debug.Log($"[Cliente] Notificando cambio de URL en Circular para índice {element.circularIndex}: {newUrl}");

        if (isServer)
        {
            RpcUpdateUrl(element.circularIndex, newUrl);
        }
        else
        {
            CmdUpdateUrl(element.circularIndex, newUrl);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateUrl(int circularIndex, string newUrl)
    {
        Debug.Log($"[Servidor] Recibido cambio de URL en Circular para índice {circularIndex}: {newUrl}");
        RpcUpdateUrl(circularIndex, newUrl);
    }

    [ClientRpc]
    void RpcUpdateUrl(int circularIndex, string newUrl)
    {
        Debug.Log($"[Cliente] Actualizando URL en todos los clientes para índice {circularIndex}: {newUrl}");

        Element element = GetElementByIndex(circularIndex);
        if (element != null)
        {
            element.url = newUrl;
            element.GetComponentInChildren<CanvasWebViewPrefab>().WebView.LoadUrl(newUrl);
            Debug.Log($"[Cliente] URL del Element {circularIndex} actualizada correctamente.");
        }
        else
        {
            Debug.LogError($"[Cliente] No se encontró un elemento con índice {circularIndex}.");
        }
    }

    //  Sincronización de categorías
    [Command(requiresAuthority = false)]
    public void CmdUpdateCategory(string elementUrl, string categoryName, bool isAdding, int elementIndex)
    {
        Debug.Log($"[Servidor] Recibida actualización de categorización para {elementUrl}, categoría: {categoryName}, agregar: {isAdding}, Índice: {elementIndex}");
        RpcUpdateCategory(elementUrl, categoryName, isAdding, elementIndex);
    }

    [ClientRpc]
    void RpcUpdateCategory(string elementUrl, string categoryName, bool isAdding, int elementIndex)
    {
        Debug.Log($"[Cliente] Sincronizando Toggle para categoría '{categoryName}' en '{elementUrl}', isAdding: {isAdding}, Índice: {elementIndex}");

        GameObject infoObjectGroup = GameObject.Find("Information Object Group");

        if (infoObjectGroup == null)
        {
            Debug.LogError("[Cliente] No se encontró el objeto 'Information Object Group' en la escena.");
            return;
        }

        Element[] allElements = infoObjectGroup.GetComponentsInChildren<Element>(true); 

        Element selectedElement = null;
        foreach (Element element in allElements)
        {
            if (element.circularIndex == elementIndex) 
            {
                selectedElement = element;
                break;
            }
        }

        if (selectedElement == null)
        {
            Debug.LogError($"[Cliente] No se encontró el elemento con índice {elementIndex} en 'Information Object Group'. URL esperada: {elementUrl}");
            return;
        }

        righthandTools.UpdateCategorizeSubMenu(selectedElement);

        UIElementCategory[] categoryElements = Resources.FindObjectsOfTypeAll<UIElementCategory>();

        foreach (UIElementCategory categoryElement in categoryElements)
        {
            if (categoryElement.categoryName == categoryName)
            {
                Toggle toggle = categoryElement.GetComponentInChildren<Toggle>();

                if (toggle != null)
                {
                    bool shouldBeOn = isAdding;

                    if (toggle.isOn != shouldBeOn)
                    {
                        toggle.isOn = shouldBeOn;
                        Debug.Log($"[Cliente] Toggle actualizado correctamente para '{categoryName}', isOn: {toggle.isOn}");
                    }
                    else
                    {
                        Debug.Log($"[Cliente] Toggle ya estaba en el estado correcto para '{categoryName}', isOn: {toggle.isOn}");
                    }
                }
                else
                {
                    Debug.LogError($"[Cliente] No se encontró el Toggle en UIElementCategory para '{categoryName}'.");
                }

                break;
            }
        }
    }

    Element GetElementByIndex(int circularIndex)
    {
        Element[] elements = FindObjectsOfType<Element>();
        foreach (Element element in elements)
        {
            if (element.circularIndex == circularIndex)
            {
                return element;
            }
        }
        return null;
    }

    IEnumerator WaitForCircularBase()
    {
        Debug.Log("[Cliente] Esperando a que CircularBase cargue...");
        CircularSpawnBase localCircularBase = null;
        while (localCircularBase == null)
        {
            yield return null;
            localCircularBase = FindObjectOfType<CircularSpawnBase>();
        }
        Debug.Log("[Cliente] CircularBase encontrado. Sincronización lista.");
        righthandTools = GameObject.FindObjectOfType<RighthandTools>();
    }
}
