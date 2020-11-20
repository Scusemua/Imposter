using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Lean.Gui;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject menuButtons;

    [SerializeField]
    private GameObject backButton;

    [SerializeField]
    private GameObject roomCodeInputBox;

    [SerializeField]
    private GameObject nicknameInputBox;

    [SerializeField]
    private GameObject goButton;

    [SerializeField]
    private GameObject warningPopup;

    [SerializeField]
    private GameObject errorDismissButton;

    [SerializeField]
    private Text errorMessageText;

    [SerializeField]
    private LeanPulse errorNotification;

    // 17 is the number of A's that can appear (i.e., "AAAAAAAAAAAAAAAAA") before it wraps around.
    private static int MAX_NAME_LENGTH = 17;

    private NetworkGameManager Manager
    {
        get
        {
            return NetworkManager.singleton as NetworkGameManager;
        }
    }

    private bool hostingGame;

    private string ERROR_INVALID_NICKNAME_LENGTH { get => "ERROR: Nickname should be between 1 and " + MAX_NAME_LENGTH + " characters in length."; }
    private const string ERROR_INVALID_NICKNAME_CONTENT = "ERROR: Invalid nickname (letters & spaces only, please).";
    private const string ERROR_INVALID_IP = "ERROR: Incorrectly formatted IPv4 address.";

    // Start is called before the first frame update
    void Start()
    {
        backButton.SetActive(false);
        goButton.SetActive(false);
        roomCodeInputBox.SetActive(false);
        nicknameInputBox.SetActive(false);
        warningPopup.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HostGameClicked()
    {
        //Debug.Log("User clicked 'Host Game' button.");
        menuButtons.SetActive(false);
        backButton.SetActive(true);
        goButton.SetActive(true);
        roomCodeInputBox.SetActive(false);
        nicknameInputBox.SetActive(true);

        hostingGame = true;
    }

    public void JoinGameClicked()
    {
        //Debug.Log("User clicked 'Join Game' button.");
        menuButtons.SetActive(false);
        backButton.SetActive(true);
        goButton.SetActive(true);
        roomCodeInputBox.SetActive(true);
        nicknameInputBox.SetActive(true);

        hostingGame = false;
    }

    public void BackButtonClicked()
    {
        //Debug.Log("User clicked 'Back' button.");
        menuButtons.SetActive(true);
        backButton.SetActive(false);
        goButton.SetActive(false);
        roomCodeInputBox.SetActive(false);
        nicknameInputBox.SetActive(false);

        hostingGame = false;
    }

    public void GoButtonClicked()
    {
        //Debug.Log("User clicked 'Go' button.");

        string enteredNickname = nicknameInputBox.GetComponent<InputField>().text;
        string enteredIPAddress = roomCodeInputBox.GetComponent<InputField>().text;

        //Debug.Log("Nickname: \"" + enteredNickname + "\", IP Address: " + enteredIPAddress + ".");

        if (!Regex.IsMatch(enteredNickname, @"^[a-zA-Z ]+$"))
        {
            //Debug.LogWarning("User entered invalid nickname: \"" + enteredNickname + "\".");
            errorMessageText.text = ERROR_INVALID_NICKNAME_CONTENT;
            //warningPopup.SetActive(true);
            errorNotification.Pulse();
            return;
        }

        if (enteredNickname.Length == 0 || enteredNickname.Length > MAX_NAME_LENGTH)
        {
            //Debug.LogWarning("User entered invalid nickname: \"" + enteredNickname + "\".");
            errorMessageText.text = ERROR_INVALID_NICKNAME_LENGTH;
            //warningPopup.SetActive(true);
            errorNotification.Pulse();
            return;
        }

        // So we can retrieve this in the next scene! 
        PlayerPrefs.SetString("nickname", enteredNickname);

        if (!hostingGame)
        {
            if (enteredIPAddress.Length > 0 && !ValidateIPv4(enteredIPAddress))
            {
                Debug.LogWarning("User did not enter a valid IP address: " + enteredIPAddress + ".");
                errorMessageText.text = ERROR_INVALID_IP;
                //warningPopup.SetActive(true);
                errorNotification.Pulse();
                return;
            }

            if (enteredIPAddress.Length == 0)
                enteredIPAddress = "127.0.0.1";

            Manager.networkAddress = enteredIPAddress;
            Manager.StartClient();
        }
        else
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                Manager.StartHost();
        }
    }

    public void ErrorDismissClicked()
    {
        //Debug.Log("User clicked error message dismiss button.");
        warningPopup.SetActive(false);
    }

    /// <summary>
    /// Source: https://stackoverflow.com/questions/11412956/what-is-the-best-way-of-validating-an-ip-address
    /// </summary>
    /// <param name="ipString">The IP address to validate.</param>
    /// <returns>True if the given string is a valid IPv4 address.</returns>
    private bool ValidateIPv4(string ipString)
    { 
        if (String.IsNullOrWhiteSpace(ipString))
        {
            return false;
        }

        string[] splitValues = ipString.Split('.');
        if (splitValues.Length != 4)
        {
            return false;
        }

        byte tempForParsing;

        return splitValues.All(r => byte.TryParse(r, out tempForParsing));
    }
}
