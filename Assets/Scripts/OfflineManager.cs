using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class OfflineManager : MonoBehaviour
{
    NetworkDiscovery netDiscovery;
    NetworkManager netManager;

    [Tooltip("Prefab for list elements")]
    public GameObject listTogglePrefab;

    [Tooltip("Prefab for list elements")]
    public InputField ipInputField;

    public GameObject sessionsListContent;
    List<string> availableSessionIp = new List<string>(); // List of IP address of all the available sessions
    Dictionary<string, GameObject> availableSessionsListElt = new Dictionary<string, GameObject>(); // Reference list elements by IP address
    Dictionary<string, float> availableSessionsTime = new Dictionary<string, float>(); // available sessions ip address and their last received timestamp
    float timeToKillSession = 5f;

    #region INITIALIZATION

    private void Awake()
    {        
        netDiscovery = FindObjectOfType<NetworkDiscovery>();
        netManager = FindObjectOfType<NetworkManager>();
    }
    
    private void Start()
    {
        netManager.networkAddress = Network.player.ipAddress;

        // Start listening for available sessions on the network
        netDiscovery.Initialize();
        netDiscovery.StartAsClient();
    }

    private void Update()
    {
        // Refresh time since we last received broadcast for each available session
        for(int i = availableSessionIp.Count - 1; i >= 0; i--)
        {
            string ipAddress = availableSessionIp[i];

            availableSessionsTime[ipAddress] += Time.deltaTime;

            if (availableSessionsTime[ipAddress] > timeToKillSession)
            {
                RemoveSession(ipAddress);
            }
        }
    }

    #endregion


    #region UI MANAGEMENT

    public void AddSession(string ipAddress, int port)
    {
        // If already informed about this session, refresh its last received timestamp
        if (availableSessionsTime.ContainsKey(ipAddress))
        {
            availableSessionsTime[ipAddress] = 0f;
            return;
        }
        // Else add a new session to the available sessions
        else
        {
            GameObject newSessionListElement = GameObject.Instantiate(listTogglePrefab, sessionsListContent.transform);
            ListToggleBehavior ltb = newSessionListElement.GetComponent<ListToggleBehavior>();
            ltb.toggle.group = sessionsListContent.GetComponent<ToggleGroup>();
            ltb.label.text = "Session at: " + ipAddress;
            ltb.networkAdress = ipAddress;
            ltb.networkPort = port;

            availableSessionIp.Add(ipAddress);
            availableSessionsListElt.Add(ipAddress, newSessionListElement);
            availableSessionsTime.Add(ipAddress, 0f);
        }
    }

    void RemoveSession(string ipAddress)
    {
        GameObject trash = availableSessionsListElt[ipAddress];
        availableSessionsListElt.Remove(ipAddress);
        availableSessionsTime.Remove(ipAddress);
        availableSessionIp.Remove(ipAddress);

        DestroyImmediate(trash);
    }

    #endregion


    #region NETWORK CONNECTION MANAGEMENT
    
    public void HostClicked()
    {
        // Start the Server
        netManager.StartHost();
    }

    public void JoinSession()
    {
        Toggle toggled = sessionsListContent.GetComponent<ToggleGroup>().ActiveToggles().FirstOrDefault();
        if (toggled == null)
        {
            Debug.Log("Join Session : No Session selected !");
            //AndroidHelper.ShowAndroidToastMessage("No ADF selected.");
            return;
        }
        else
        {
            // Get session info for connection
            ListToggleBehavior ltb = toggled.gameObject.GetComponent<ListToggleBehavior>();
            netManager.networkAddress = ltb.networkAdress;
            //netManager.networkPort = ltb.networkPort;

            Debug.Log("Try connect to: " + netManager.networkAddress + ":" + netManager.networkPort);            
            
            // Connect
            netManager.StartClient();
        }
    }

    public void JoinManually()
    {
        netManager.networkAddress = ipInputField.text;

        Debug.Log("Try connect to: " + netManager.networkAddress + ":" + netManager.networkPort);

        // Connect
        netManager.StartClient();
    }

    #endregion
}
