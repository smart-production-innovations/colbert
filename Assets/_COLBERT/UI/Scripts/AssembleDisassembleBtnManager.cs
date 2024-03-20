using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

//toggles assembly and disassembly UI-Buttons according to the possibilty to assemble or disassemble
public class AssembleDisassembleBtnManager : MonoBehaviour
{
    [SerializeField] public GameObject assemblyButton;
    [SerializeField] private GameObject disassemblyButton;
    [SerializeField] private GameObject showInfo;
    [SerializeField] private GameObject rotateArrows;

    [SerializeField] private Player thisPlayer;

    private void Update()
    {
        bool assemblePossible = NetworkModelExplorer.AssemblePossible(thisPlayer);
        bool disassemblePossible = NetworkModelExplorer.DisassemblePossible(thisPlayer);
        assemblyButton.SetActive(assemblePossible);
        disassemblyButton.SetActive(disassemblePossible);

        if (showInfo != null)
        {
            showInfo.SetActive(assemblePossible || disassemblePossible);
        }
        if (rotateArrows != null)
        {
            NetworkModelInteractable selected = GetSelectedInteractable(thisPlayer);
            rotateArrows.SetActive(selected != null && selected.Initialized);
        }
    }

    public static NetworkModelInteractable GetSelectedInteractable(Player player)
    {
        if (!player.IsActive)
            return null;

        XRBaseInteractor[] interactors = player.GetComponentsInChildren<XRBaseInteractor>();
        foreach (XRBaseInteractor interactor in interactors)
        {
            List<IXRSelectInteractable> interactables = interactor.interactablesSelected;
            if (interactables == null || interactables.Count == 0)
                continue;

            foreach (IXRSelectInteractable interactable in interactables)
            {
                if (interactable.transform.TryGetComponent(out NetworkModelInteractable helper))
                {
                    return helper;
                }
            }
        }
        return null;
    }
}
