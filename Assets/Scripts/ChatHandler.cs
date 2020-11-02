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

    public override void OnStartAuthority()
    {
        chatContent = GameObject.FindGameObjectWithTag("ChatContent");
        chatSendButton = GameObject.FindGameObjectWithTag("ChatSendButton").GetComponent<LeanButton>();
        inputField = GameObject.FindGameObjectWithTag("ChatInputField").GetComponent<TMP_InputField>();

        chatSendButton.OnClick.AddListener(Send);

        OnMessage += HandleNewMessage;
    }

    [ClientCallback]
    private void OnDestroy()
    {
        if (!hasAuthority) { return; }

        OnMessage -= HandleNewMessage;
    }

    private void HandleNewMessage(string message)
    {
        GameObject TextMeshProUGUIGameObject = Instantiate(TextMeshProUGUIPrefab, transform);
        TextMeshProUGUI textMeshProUGUI = TextMeshProUGUIGameObject.GetComponent<TextMeshProUGUI>();
        textMeshProUGUI.text = message;

        // Move this to the chat content.
        TextMeshProUGUIGameObject.transform.parent = chatContent.transform;
    }

    [Client]
    public void Send()
    {
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
