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
        netManager = FindObjectOfType<NetworkManager>();

        Debug.Log("Scene Switched");
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
