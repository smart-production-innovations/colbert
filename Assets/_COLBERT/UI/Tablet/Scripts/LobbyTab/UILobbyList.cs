using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//a list showing all lobbies available for joining
public class UILobbyList : MonoBehaviour
{
    [SerializeField]
    private UILobby uiLobby;
    [SerializeField]
    private UILobbyListEntry entryPrefab;
    [SerializeField]
    private RectTransform listParent;

    [SerializeField]
    private List<UILobbyListEntry> list = new List<UILobbyListEntry>();

    private void Awake()
    {
        entryPrefab.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        uiLobby.RefreshLobbies();
    }

    public void JoinLobby(Lobby lobby)
    {
        uiLobby.JoinLobby(lobby);
    }

    public void UpdateList(List<Lobby> lobbies)
    {
        while (list.Count > lobbies.Count)
        {
            UILobbyListEntry entryToRemove = list[list.Count - 1];
            list.Remove(entryToRemove);
            Destroy(entryToRemove.gameObject);
        }
        while (list.Count < lobbies.Count)
        {
            UILobbyListEntry newEntry = Instantiate(entryPrefab, listParent);
            newEntry.gameObject.SetActive(true);
            list.Add(newEntry);
        }
        for (int i = 0; i < list.Count; i++)
        {
            list[i].Initialize(lobbies[i]);
        }
    }
}
