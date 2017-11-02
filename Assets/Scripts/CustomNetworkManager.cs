using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;


public class ADFMessageType
{
    public static short Score = MsgType.Highest + 1;
}

public class ADFMessage :MessageBase
{
    public int adfTotalSize;
    public int chunkIndex;
    public int adfChunkSize;
    public byte[] adfChunkData;

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


public class CustomNetworkManager : NetworkManager
{
    const float NETWORK_DELAY_MSG = 0.3f;
    const int ADF_SIZE = 10000000;
    const int MAX_MSG_SIZE = 64000;
    byte[] adf;
    bool adfReceived = false;


    // On SERVER : called when a client connects (before scene switch)
    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("CALL: OnServerConnect");
        base.OnServerConnect(conn);
    }

    // On CLIENT : called when connected to a server (after scene switched)
    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("CALL: OnClientConnect");
        //base.OnClientConnect(conn);

        client.RegisterHandler(ADFMessageType.Score, OnADFMessage);

        //NetworkClient localClient = NetworkClient.allClients.Find(x => x.connection == conn);
        //ClientScene.AddPlayer(conn, 0, new IntegerMessage(1234));
    }

    // On CLIENT : Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        Debug.Log("CALL: OnClientSceneChanged");
        //base.OnClientSceneChanged(conn); // modif this when switching scene

        ClientScene.Ready(conn);
    }

    // On SERVER : called when a client is ready
    public override void OnServerReady(NetworkConnection conn)
    {
        Debug.Log("CALL: OnServerReady");
        base.OnServerReady(conn);
        
        adf = new byte[ADF_SIZE];

        if (conn.address != "localClient")
        {
            StartCoroutine(SendADFToClient(conn));
        }
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
        base.OnServerAddPlayer(conn, playerControllerId, extraMessageReader);
    }

    private IEnumerator SendADFToClient(NetworkConnection conn)
    {
        int nbIter = (ADF_SIZE % MAX_MSG_SIZE == 0) ? ADF_SIZE / MAX_MSG_SIZE : 1 + ADF_SIZE / MAX_MSG_SIZE;

        Debug.Log("Number of iterations: " + nbIter);

        for (int i = 0; i < nbIter; i++)
        {
            int chunkBeginning = i * MAX_MSG_SIZE;
            int chunkEnd = (i + 1) * MAX_MSG_SIZE - 1;
            if (chunkEnd > ADF_SIZE) chunkEnd = ADF_SIZE - 1;
            ADFMessage chunkMsg = new ADFMessage();
            chunkMsg.chunkIndex = i;
            chunkMsg.adfChunkSize = (chunkEnd - chunkBeginning) + 1;
            chunkMsg.adfTotalSize = ADF_SIZE;
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



    public void OnADFMessage(NetworkMessage netMsg)
    {
        ADFMessage msg = netMsg.ReadMessage<ADFMessage>();

        Debug.Log("ADF Message received : " + msg.chunkIndex);
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





