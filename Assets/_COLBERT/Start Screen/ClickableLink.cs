using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//open urls in TMP text components in a browser
//example:
//<link="LINKID"><color=#039CED><u><i>Homepage</i></u></color></link>
//where LINKID is either a url (e.g. www.abc.com) or an index in the array links (e.g. 0)
[RequireComponent(typeof(TextMeshProUGUI))]
public class ClickableLink : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private string[] links;

    public void OnPointerClick(PointerEventData eventData)
    {
        TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, null);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];

            string linkID = linkInfo.GetLinkID();
            if (int.TryParse(linkID, out int i))
            {
                if (links != null && i >= 0 && i < links.Length)
                {
                    string link = links[i];
                    Debug.Log($"open url '{link}'");
                    Application.OpenURL(link);
                }
            }
            else
            {
                Debug.Log($"open url '{linkID}'");
                Application.OpenURL(linkID);
            }
        }
    }
}
