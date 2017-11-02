using System;
using UnityEngine;
using UnityEngine.Networking;

public class CustomNetworkDiscovery : NetworkDiscovery
{
    // Conversion Array of Bytes -> String
    static string BytesToString(byte[] bytes)
    {
        char[] chars = new char[bytes.Length / sizeof(char)];
        Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
        return new string(chars);
    }

    // Callback when broadcast messages received
    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        Debug.Log("Got broadcast from '" + fromAddress + "'  -  " + data);

        var value = base.broadcastsReceived[fromAddress];
        string dataString = BytesToString(value.broadcastData);
        var items = dataString.Split(':');

        if (items.Length == 3 && items[0] == "NetworkManager")
        {
            int port = Convert.ToInt32(items[2]);
            items = fromAddress.Split(':');
            string ipAdress = items[items.Length - 1]; // ::ffff:ipadrress
            FindObjectOfType<OfflineManager>().AddSession(ipAdress, port);
        }

    }
}
