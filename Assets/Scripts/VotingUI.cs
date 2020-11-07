using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class VotingUI : MonoBehaviour
{
    public Timer CountdownTimer;
    public PlayerController PlayerController;
    public GameObject PlayerUI;
    public GameObject VotingEntryPrefab;
    public GridLayoutGroup ScrollViewContent;

    // Start is called before the first frame update
    void Start()
    {
        CountdownTimer.OnTimerCompleted += OnTimerCompleted;

        NetworkGameManager instance = NetworkManager.singleton as NetworkGameManager;

        foreach (Player gamePlayer in instance.GamePlayers)
        {
            GameObject votingEntryGameObject = Instantiate(VotingEntryPrefab, ScrollViewContent.transform);
            VotingEntry votingEntry = votingEntryGameObject.GetComponent<VotingEntry>();
            votingEntry.PlayerIcon.color = gamePlayer.PlayerColor;
            votingEntry.ButtonText.text = gamePlayer.nickname;
        }
    }

    public void OnTimerCompleted(int arg0)
    {
        Debug.Log("Timer has completed.");

        PlayerUI.SetActive(true);
        PlayerController.MovementEnabled = true;

        Destroy(this, 1.0f);

        // TODO: Handle votes?
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        CountdownTimer.OnTimerCompleted -= OnTimerCompleted;
    }
}
