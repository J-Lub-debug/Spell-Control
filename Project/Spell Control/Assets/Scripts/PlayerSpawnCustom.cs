using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class PlayerSpawnCustom : NetworkManager
{
    int playerID = 0;

    public GameObject leftSpawn;
    public GameObject rightSpawn;


    public override void OnClientConnect(NetworkConnection conn)
    {
        ClientScene.AddPlayer(conn);
        DisableHudGui();
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        playerID++;
        Vector2 spawnPos;
        if(playerID % 2 == 0)
        {
            spawnPos = leftSpawn.transform.position;
        }
        else
        {
            spawnPos = rightSpawn.transform.position;
        }
        GameObject currentPlayerObjects = new GameObject("Player" + playerID + "Objects");
        GameObject currentPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity, currentPlayerObjects.transform);
        currentPlayer.name = "Player" + playerID;
        NetworkServer.AddPlayerForConnection(conn, currentPlayer);
    }

    public void DisableHudGui()
    {
        NetworkManagerHUD hud = FindObjectOfType<NetworkManagerHUD>();
        if (hud != null)
            hud.showGUI = false;
    }
}
