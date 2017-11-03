using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

#region MESSAGE CLASS

public class ADFMessageType
{
    public static short Score = MsgType.Highest + 1;
}

// Message class used to send big ADF file by chunks
public class ADFMessage : MessageBase
{
    public int adfTotalSize;        // Total size of the ADF file
    public int chunkIndex;          // Index of this chunk in the complete file
    public int adfChunkSize;        // Size of this chunk
    public byte[] adfChunkData;     // Chunk of data

    public override void Deserialize(NetworkReader reader)
    {
        adfTotalSize = (int)reader.ReadPackedUInt32();
        chunkIndex = (int)reader.ReadPackedUInt32();
        adfChunkData = reader.ReadBytesAndSize();
        if (adfChunkData == null)
        {
            adfChunkSize = 0;
        }
        else
        {
            adfChunkSize = adfChunkData.Length;
        }
    }

    public override void Serialize(NetworkWriter writer)
    {
        writer.WritePackedUInt32((uint)adfTotalSize);
        writer.WritePackedUInt32((uint)chunkIndex);
        writer.WriteBytesAndSize(adfChunkData, adfChunkSize);
    }
}

public class AskForADFMessageType
{
    public static short Score = MsgType.Highest + 2;
}

#endregion // MESSAGE CLASS



public class CustomNetworkManager : NetworkManager
{
    public enum PLAYER_TYPES { DEFAULT = 0, TANGO = 1 }

    const float NETWORK_DELAY_MSG = 0.5f;
    const int MAX_MSG_SIZE = 64000;

    byte[] adf;
    int adf_size = 0;
    int adf_data_received = 0;
    bool adfReceived = false;

    // On CLIENT:
    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);

        if (GlobalData.current_adf == null)
        {
            Debug.Log("CLIENT START:  isTango: " + GlobalData.isTango + "  ADF: not got yet");
        }
        else
            Debug.Log("CLIENT START:  isTango: " + GlobalData.isTango + "  ADF: " + GlobalData.current_adf.m_uuid);
    }

    // On SERVER : called when a client connects (before scene switch)
    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("CALL: OnServerConnect  isTango: " + GlobalData.isTango + "  ADF: " + GlobalData.current_adf.m_uuid);
        base.OnServerConnect(conn);
        NetworkServer.RegisterHandler(AskForADFMessageType.Score, OnAskedForADF);
    }

    // On CLIENT : called when connected to a server (after scene switched)
    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("CALL: OnClientConnect.  conn.address: " + conn.address );
        //base.OnClientConnect(conn);  // Disable the default behavior so no player gets added until we get the ADF
        
        if (GlobalData.isTango)
        {
            client.RegisterHandler(ADFMessageType.Score, OnADFMessage);

            // Ask for ADF only if not host
            if (conn.address != "localServer")
            {
                Debug.Log("Ask for ADF");
                client.Send(AskForADFMessageType.Score, new EmptyMessage());
            }
            else
            {
                AddPlayer(PLAYER_TYPES.TANGO, conn);
            }
        }
        else
        {
            // If not tango, just add a default player
            AddPlayer(PLAYER_TYPES.DEFAULT, conn);
        }
    }

    // On CLIENT : Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        Debug.Log("CALL: OnClientSceneChanged");
        //base.OnClientSceneChanged(conn); // modif this when switching scene
        //ClientScene.Ready(conn);
    }

    // On SERVER : called when a client is ready
    public override void OnServerReady(NetworkConnection conn)
    {
        Debug.Log("CALL: OnServerReady");
        base.OnServerReady(conn);
    }

    // On SERVER : called when a new player is added for a client
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        Debug.Log("CALL: OnServerAddPlayer (*,*)");
        base.OnServerAddPlayer(conn, playerControllerId);
    }
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
    {
        Debug.Log("CALL: OnServerAddPlayer (*,*,*)");
        int playerPrefabIdx = 0;
        if (extraMessageReader != null)
            playerPrefabIdx = extraMessageReader.ReadMessage<IntegerMessage>().value;

        playerPrefab = spawnPrefabs[playerPrefabIdx];
        GameObject player = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }


    // On CLIENT : called to tell SERVER to add player
    public void AddPlayer(PLAYER_TYPES player_type, NetworkConnection conn)
    {
        Debug.Log("ADD PLAYER : " + player_type + " : " + (int)player_type);

        ClientScene.Ready(conn);
        IntegerMessage player_type_msg = new IntegerMessage((int)player_type);

        ClientScene.AddPlayer(conn, 0, player_type_msg);
    }

    // On CLIENT : called when an ADF chunk has been received
    public void OnADFMessage(NetworkMessage netMsg)
    {
        ADFMessage msg = netMsg.ReadMessage<ADFMessage>();

        if (adf_data_received == 0)
        {
            adf = new byte[msg.adfTotalSize];
            adf_size = adf.Length;
        }
        
        Array.Copy(msg.adfChunkData, 0, adf, msg.chunkIndex * MAX_MSG_SIZE, msg.adfChunkSize);
        adf_data_received += msg.adfChunkSize;

        Debug.Log("ADF Message received : " + msg.chunkIndex + "  -  " + adf_data_received + "/" + msg.adfTotalSize);

        if (adf_data_received == msg.adfTotalSize)
        {
            string feedback = "ADF fully received";
            Debug.Log(feedback);
            AndroidHelper.ShowAndroidToastMessage(feedback);

            string path = Application.persistentDataPath + "/received.adf";
            File.WriteAllBytes(path, adf);
            adfReceived = true;

            Debug.Log("CALL: IMPORT FROM FILE   " + Tango.AreaDescription.GetList().Length);
            Tango.AreaDescription.ImportFromFile(path);
        }
    }

    // On SERVER : called when a client ask the server to send him the ADF
    public void OnAskedForADF(NetworkMessage netMsg)
    {
        Debug.Log("On Asked for ADF call");
        if (adf_size == 0)
        {
            adf = File.ReadAllBytes(GlobalData.adf_path);
            adf_size = adf.Length;
        }

        StartCoroutine(SendADFToClient(netMsg.conn));
    }

    // Divide the ADF in 64k chunks of bytes and send them to the client
    private IEnumerator SendADFToClient(NetworkConnection conn)
    {
        int nbIter = (adf_size % MAX_MSG_SIZE == 0) ? adf_size / MAX_MSG_SIZE : 1 + adf_size / MAX_MSG_SIZE;

        Debug.Log("Number of iterations: " + nbIter);

        for (int i = 0; i < nbIter; i++)
        {
            int chunkBeginning = i * MAX_MSG_SIZE;
            int chunkEnd = (i + 1) * MAX_MSG_SIZE - 1;
            if (chunkEnd > adf_size) chunkEnd = adf_size - 1;
            ADFMessage chunkMsg = new ADFMessage();
            chunkMsg.chunkIndex = i;
            chunkMsg.adfChunkSize = (chunkEnd - chunkBeginning) + 1;
            chunkMsg.adfTotalSize = adf_size;
            chunkMsg.adfChunkData = new byte[chunkMsg.adfChunkSize];
            Array.Copy(adf, chunkBeginning, chunkMsg.adfChunkData, 0, chunkMsg.adfChunkSize);

            if (NetworkServer.connections.Contains(conn))
            {
                NetworkServer.SendToClient(conn.connectionId, ADFMessageType.Score, chunkMsg);
            }
            else
            {
                break;
            }

            yield return new WaitForSeconds(NETWORK_DELAY_MSG);
        }
    }

}





/*
    // Called ON SERVER/HOST when a new player is added for a client
    /// Copied from Unity's original NetworkManager 'OnServerAddPlayerInternal' script except where noted
    /// Since OnServerAddPlayer calls OnServerAddPlayerInternal and needs to pass the message - just add it all into one.
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
    {
        /// *** additions
        /// I skipped all the debug messages...
        /// This is added to recieve the message from addPlayer()...
        int id = 0;

        if (extraMessageReader != null)
        {
            IntegerMessage i = extraMessageReader.ReadMessage<IntegerMessage>();
            id = i.value;
        }

        Debug.Log("CALL: OnServerAddPlayer : " + id);

        var player = (GameObject)GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }

    // Called ON CLIENT when connected to a server
    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("CALL: OnClientConnect");

        /// Can't directly send an int variable to 'addPlayer()' so you have to use a message service...
        IntegerMessage msg = new IntegerMessage(1234);
        /// ***

        if (!clientLoadedScene)
        {
            // Ready/AddPlayer is usually triggered by a scene load completing. if no scene was loaded, then Ready/AddPlayer it here instead.
            ClientScene.Ready(conn);
            if (autoCreatePlayer)
            {
                Debug.Log("CALL: OnClientConnect => AddPlayer");
                ///***
                /// This is changed - the original calls a differnet version of addPlayer
                /// this calls a version that allows a message to be sent
                ClientScene.AddPlayer(conn, 0, msg);
            }
        }
    }
    */





