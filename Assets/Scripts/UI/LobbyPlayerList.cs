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
    public Dictionary<uint, int> NetIDToIndexMap = new Dictionary<uint, int>();
    public Dictionary<uint, string> NetIDToNicknameMap = new Dictionary<uint, string>();

    public Color ReadyColor;
    public Color NotReadyColor;
    public Color WaitingNameColor;

    void Start()
    {
        //Debug.Log("Start() called for LobbyPlayerList.");
        gameOptions = GameOptions.singleton;

        maxEntries = gameOptions.NumPlayers;
        numRealEntries = 0;
        nextValidIndex = 0;
        numPlaceholderEntries = 0; // For now, this will get changed in PopulateInitialList().

        PopulateInitialList();
    }

    void PopulateInitialList()
    {
        for (int i = 0; i < maxEntries; i++)
        {
            // Note that we increment numPlaceholderEntries in here.
            AddPlaceholder();
        }
    }

    public void Clear(bool addPlaceholders)
    {
        LobbyPlayerListEntries.Clear();
        NetIDToIndexMap.Clear();
        NetIDToNicknameMap.Clear();

        numRealEntries = 0;
        nextValidIndex = 0;

        if (addPlaceholders)
            // Add placeholder values back.
            PopulateInitialList();
    }


    public bool ContainsEntry(uint netId)
    {
        return NetIDToIndexMap.ContainsKey(netId);
    }

    private void OnDestroy()
    {
        //Debug.Log("OnDestroy() called for LobbyPlayerList.");
        
        Clear(false);
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

    /// <summary>
    /// Updates an existing entry, if it exists. Otherwise creates a new entry.
    /// </summary>
    /// <returns>False if we update an existing entry. True if we create a new entry.</returns>
    public bool AddOrUpdateEntry(uint netId, string playerName, bool readyStatus)
    {
        //Debug.Log("AddOrUpdateEntry() called. netId = " + netId + ", playerName = " + playerName + ", readyStatus = " + readyStatus + ".");
        if (UpdateEntry(netId, playerName, readyStatus))
            return false;

        AddEntry(netId, playerName, readyStatus);
        return true;
    }

    /// <summary>
    /// Modify attributes of existing list entry.
    /// </summary>
    /// <returns>True if an entry associated with the given netId existed (and was therefore updated). Otherwise, returns false.</returns>
    public bool UpdateEntry(uint netId, string playerName, bool readyStatus)
    {
        if (!NetIDToIndexMap.ContainsKey(netId))
        {
            Debug.LogWarning("Lobby player list does NOT contain an entry for netID \"" + netId + "\". Returning immediately.");
            return false;
        }

        int index = NetIDToIndexMap[netId];

        GameObject lobbyPlayerListEntry = LobbyPlayerListEntries[index];
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

        return true;
    }

    public void AddEntry(uint netId, string playerName, bool readyStatus)
    {
        if (numRealEntries == maxEntries)
            Debug.LogError("ERROR: Cannot add another entry to lobby player list. Already at maximum capacity (" + maxEntries + ").");

        if (NetIDToIndexMap.ContainsKey(netId))
        {
            Debug.LogWarning("Lobby player list already contains entry for netID \"" + netId + "\". Returning immediately.");
            return;
        }

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

        //Debug.Log("Adding player lobby list entry for player \"" + playerName + "\" at index " + nextValidIndex);

        lobbyPlayerListEntry.transform.SetSiblingIndex(nextValidIndex); // Position the entry correctly.
        NetIDToIndexMap.Add(netId, nextValidIndex);
        NetIDToNicknameMap.Add(netId, playerName);
        LobbyPlayerListEntries[nextValidIndex++] = lobbyPlayerListEntry;
    }

    public void ModifyReadyStatus(uint netId, string playerName, bool readyStatus)
    {
        int index = NetIDToIndexMap[netId];

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

    public void RemoveEntry(uint netId, string playerName, bool readyStatus)
    {
        int index = NetIDToIndexMap[netId];

        GameObject lobbyPlayerListEntry = LobbyPlayerListEntries[index];
        nextValidIndex--;
        Destroy(lobbyPlayerListEntry);
        NetIDToIndexMap.Remove(netId);
        NetIDToNicknameMap.Remove(netId);

        AddPlaceholder();
    }
}
