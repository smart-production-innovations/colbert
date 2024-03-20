using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UILobbyMemberListEntry : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private RectTransform hostEntryIcon;
    [SerializeField]
    private RectTransform ownEntryIcon;
    [SerializeField]
    private Image ownEntryBackground;

    private LobbyMember member;
    private bool isSelf = false;
    private bool isFirst = false;
    private NetworkPlayer player = null;

    private void Update()
    {
        UpdatePlayer();
    }

    public void Initialize(LobbyMember lobbyMember)
    {
        member = lobbyMember;

        nameText.text = member.name;

        isSelf = lobbyMember.isLocal;
        isFirst = transform.parent.childCount > 1 && transform == transform.parent.GetChild(1); //child(0) is the entry prefab

        if (ownEntryBackground != null)
            ownEntryBackground.enabled = isSelf;
        if (hostEntryIcon != null)
            hostEntryIcon.gameObject.SetActive(isFirst);
        if (ownEntryIcon != null)
            ownEntryIcon?.gameObject.SetActive(isSelf);
    }

    private void UpdatePlayer()
    {
        if (player != null)
            return;

        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (NetworkPlayer player in players)
        {
            if (player.ClientId == member.clientId)
            {
                this.player = player;
                return;
            }
        }
    }
}
