using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//show list with members of the lobby (if in the lobby)
public class UILobbyMemberList : MonoBehaviour
{
    [SerializeField]
    private UILobbyMemberListEntry entryPrefab;
    [SerializeField]
    private RectTransform listParent;

    private List<UILobbyMemberListEntry> list = new List<UILobbyMemberListEntry>();

    private void Awake()
    {
        entryPrefab.gameObject.SetActive(false);
    }

    public void UpdateList(Lobby lobby)
    {
        if (lobby.IsEmpty)
        {
            foreach (UILobbyMemberListEntry entry in list)
            {
                Destroy(entry.gameObject);
            }
            list.Clear();
            return;
        }

        var members = lobby.members;
        int memberCount = members != null ? members.Count : 0;
        while (list.Count > memberCount)
        {
            UILobbyMemberListEntry entryToRemove = list[list.Count - 1];
            list.Remove(entryToRemove);
            Destroy(entryToRemove.gameObject);
        }
        while (list.Count < memberCount)
        {
            UILobbyMemberListEntry newEntry = Instantiate(entryPrefab, listParent);
            newEntry.gameObject.SetActive(true);
            list.Add(newEntry);
        }
        int i = 0;
        foreach (var member in members.Values)
        {
            list[i].Initialize(member);
            i++;
        }
    }
}
