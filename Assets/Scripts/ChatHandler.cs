using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using System;
using Lean.Gui;

public class ChatHandler : NetworkBehaviour
{
    [SerializeField] private GameObject chatContent = null;
    [SerializeField] private LeanButton chatSendButton = null;
    [SerializeField] private TMP_InputField inputField = null;

    public CustomNetworkRoomPlayer CustomNetworkRoomPlayer;

    public GameObject TextMeshProUGUIPrefab;

    private static event Action<string> OnMessage;
    
    public void CreateUIHooks()
    {
        if (!CustomNetworkRoomPlayer.isLocalPlayer) return;

        chatContent = GameObject.FindGameObjectWithTag("ChatContent");
        chatSendButton = GameObject.FindGameObjectWithTag("ChatSendButton").GetComponent<LeanButton>();
        inputField = GameObject.FindGameObjectWithTag("ChatInputField").GetComponent<TMP_InputField>();

        chatSendButton.OnClick.AddListener(Send);

        // When edit is ended, check if we're also pressing enter. This prevents chat from being sent just on deselection.
        inputField.onEndEdit.AddListener(message =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendParameterized(message);
                SelectInputField(); // Retain focus so users can continue to type.
            }
        });

        OnMessage += HandleNewMessage;
    }

    /// <summary>
    /// Focuses on the chat input field at the end of the current frame.
    /// </summary>
    IEnumerator SelectInputField()
    {
        yield return new WaitForEndOfFrame();
        inputField.Select();
    }

    [ClientCallback]
    private void OnDestroy()
    {
        if (!hasAuthority) { return; }

        OnMessage -= HandleNewMessage;

        chatSendButton.OnClick.RemoveListener(Send);
    }

    [Client]
    private void HandleNewMessage(string message)
    {
        if (!CustomNetworkRoomPlayer.isLocalPlayer) return;

        GameObject TextMeshProUGUIGameObject = Instantiate(TextMeshProUGUIPrefab, transform);
        TextMeshProUGUI textMeshProUGUI = TextMeshProUGUIGameObject.GetComponent<TextMeshProUGUI>();
        textMeshProUGUI.text = message;

        // Move this to the chat content.
        TextMeshProUGUIGameObject.transform.SetParent(chatContent.transform);
        TextMeshProUGUIGameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    [Client]
    public void Send()
    {
        if (!CustomNetworkRoomPlayer.isLocalPlayer) return;

        if (string.IsNullOrWhiteSpace(inputField.text)) { return; }

        CmdSendMessage(inputField.text);

        inputField.text = string.Empty;
    }

    [Client]
    public void SendParameterized(string message)
    {
        if (!CustomNetworkRoomPlayer.isLocalPlayer) return;

        if (string.IsNullOrWhiteSpace(inputField.text)) { return; }

        CmdSendMessage(inputField.text);

        inputField.text = string.Empty;
    }

    [Command]
    private void CmdSendMessage(string message)
    {
        RpcHandleMessage($"[{CustomNetworkRoomPlayer.DisplayName}]: {message}");
    }

    [ClientRpc]
    private void RpcHandleMessage(string message)
    {
        OnMessage?.Invoke($"{message}");
    }
}
