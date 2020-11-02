using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class LobbyPlayerList : MonoBehaviour
{
    public GameObject LobbyPlayerListEntryGameObject;

    public GameOptions gameOptions;

    private int maxEntries;
    private int numRealEntries;
    private int numPlaceholderEntries;
    private int nextValidIndex;

    public List<GameObject> LobbyPlayerListEntries = new List<GameObject>();
    public Dictionary<string, int> NicknameToIndexMap = new Dictionary<string, int>();

    public Color ReadyColor;
    public Color NotReadyColor;
    public Color WaitingNameColor;

    void Start()
    {
        gameOptions = GameOptions.singleton;

        maxEntries = gameOptions.numPlayers;
        numRealEntries = 0;
        nextValidIndex = 0;

        PopulateInitialList();
    }

    void PopulateInitialList()
    {
        for (int i = 0; i < maxEntries; i++)
        {
            AddPlaceholder();
        }
    }

    private void AddPlaceholder()
    {
        GameObject lobbyPlayerListEntry = Instantiate(LobbyPlayerListEntryGameObject, transform.position, transform.rotation, transform);
        lobbyPlayerListEntry.transform.localScale = new Vector3(1, 1, 1);
        
        TextMeshProUGUI[] TMPs = lobbyPlayerListEntry.GetComponentsInChildren<TextMeshProUGUI>();
        TMPs[0].color = WaitingNameColor;

        LobbyPlayerListEntries.Add(lobbyPlayerListEntry);
        numPlaceholderEntries++;
    }

    public void AddEntry(string playerName, bool readyStatus)
    {
        if (numRealEntries == maxEntries)
            Debug.LogError("ERROR: Cannot add another entry to lobby player list. Already at maximum capacity (" + maxEntries + ").");

        GameObject lobbyPlayerListEntry = Instantiate(LobbyPlayerListEntryGameObject, transform.position, transform.rotation, transform);
        lobbyPlayerListEntry.transform.localScale = new Vector3(1, 1, 1);
        TextMeshProUGUI[] TMPs = lobbyPlayerListEntry.GetComponentsInChildren<TextMeshProUGUI>();

        TMPs[0].text = playerName;
        TMPs[0].color = Color.white;

        if (readyStatus)
        {
            TMPs[1].text = "READY";
            TMPs[1].color = ReadyColor;
        } 
        else
        {
            TMPs[1].text = "NOT READY";
            TMPs[1].color = NotReadyColor;
        }

        if (LobbyPlayerListEntries[nextValidIndex] != null)
        {
            Destroy(LobbyPlayerListEntries[nextValidIndex]);
            numPlaceholderEntries--;
        }

        Debug.Log("Adding player lobby list entry for " + playerName + " at index " + nextValidIndex);

        lobbyPlayerListEntry.transform.SetSiblingIndex(nextValidIndex); // Position the entry correctly.
        NicknameToIndexMap.Add(playerName, nextValidIndex);
        LobbyPlayerListEntries[nextValidIndex++] = lobbyPlayerListEntry;
    }

    public void ModifyReadyStatus(string playerName, bool readyStatus)
    {
        int index = NicknameToIndexMap[playerName];

        GameObject lobbyPlayerListEntry = LobbyPlayerListEntries[index];
        TextMeshProUGUI[] TMPs = lobbyPlayerListEntry.GetComponentsInChildren<TextMeshProUGUI>();

        if (readyStatus)
        {
            TMPs[1].text = "READY";
            TMPs[1].color = ReadyColor;
        }
        else
        {
            TMPs[1].text = "NOT READY";
            TMPs[1].color = NotReadyColor;
        }
    }

    public void RemoveEntry(string playerName, bool readyStatus)
    {
        int index = NicknameToIndexMap[playerName];

        GameObject lobbyPlayerListEntry = LobbyPlayerListEntries[index];
        nextValidIndex--;
        Destroy(lobbyPlayerListEntry);
        NicknameToIndexMap.Remove(playerName);

        AddPlaceholder();
    }
}
