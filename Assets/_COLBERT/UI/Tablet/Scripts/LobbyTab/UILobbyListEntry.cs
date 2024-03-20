using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UILobbyListEntry : MonoBehaviour
{
    [SerializeField]
    private UILobbyList lobbyList;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI countText;

    private Lobby data;


    public void Initialize(Lobby lobbyData)
    {
        data = lobbyData;
        if (!string.IsNullOrEmpty(data.name))
        {
            nameText.text = data.name;
        }

        countText.text = $"{data.members.Count}";
    }

    public void Join()
    {
        lobbyList.JoinLobby(data);
    }

}
