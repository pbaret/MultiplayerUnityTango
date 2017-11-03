using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class OnlineManager : MonoBehaviour
{
    NetworkManager netManager;

    private void Awake()
    {
        Debug.Log("Scene Switched");
        netManager = FindObjectOfType<NetworkManager>();

#if !UNITY_ANDROID || UNITY_EDITOR
        Tango.TangoApplication tangoManager = FindObjectOfType<Tango.TangoApplication>();
        DestroyImmediate(tangoManager.gameObject.GetComponent<RelocalizingOverlay>().m_relocalizationOverlay);
        DestroyImmediate(tangoManager.gameObject);
        DestroyImmediate(GetComponent<OnlineADFManager>());
        DestroyImmediate(FindObjectOfType<TangoPoseController>().gameObject);
#else
        DestroyImmediate(GameObject.Find("Main Camera"));
#endif
    }

    private void Start()
    {
        if (NetworkServer.active)
        {
            NetworkDiscovery netDiscovery = FindObjectOfType<NetworkDiscovery>();
            netDiscovery.Initialize();
            netDiscovery.StartAsServer();
        }
    }
    
    public void Disconnect()
    {
        NetworkDiscovery netDiscovery = FindObjectOfType<NetworkDiscovery>();
        if (netDiscovery.running)
            netDiscovery.StopBroadcast();
        DestroyImmediate(netDiscovery.gameObject);

        if (NetworkServer.active || netManager.IsClientConnected())
        {
            netManager.StopHost();
            NetworkServer.Reset();
        }
    }
}
