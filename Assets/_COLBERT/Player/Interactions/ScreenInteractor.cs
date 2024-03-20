using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

//interactor for pc user - basically a ray interactor with raycast forward from screencenter
public class ScreenInteractor : XRBaseInteractor, IXRActivateInteractor
{
    /// <summary>
    /// This defines the type of input that triggers an interaction.
    /// </summary>
    /// <seealso cref="selectActionTrigger"/>
    public enum InputTriggerType
    {
        /// <summary>
        /// Unity will consider the input active while the button is pressed.
        /// A user can hold the button before the interaction is possible
        /// and still trigger the interaction when it is possible.
        /// </summary>
        /// <remarks>
        /// When multiple interactors select an interactable at the same time and that interactable's
        /// <see cref="InteractableSelectMode"/> is set to <see cref="InteractableSelectMode.Single"/>, you may
        /// experience undesired behavior of selection repeatedly passing between the interactors and the select
        /// interaction events firing each frame. State Change is the recommended and default option. 
        /// </remarks>
        /// <seealso cref="InteractionState.active"/>
        /// <seealso cref="InteractableSelectMode"/>
        State,

        /// <summary>
        /// The interaction starts on the frame the input is pressed
        /// and remains engaged until the second time the input is pressed.
        /// </summary>
        Toggle,
    }


    bool m_ToggleSelectActive;
    bool m_ToggleSelectDeactivatedThisFrame;
#pragma warning disable CS0414
    bool m_WaitingForSelectDeactivate; //unused?
#pragma warning restore CS0414

    [SerializeField]
    InputTriggerType m_SelectActionTrigger = InputTriggerType.State;
    /// <summary>
    /// Choose how Unity interprets the select input action from the controller.
    /// Controls between different input styles for determining if this Interactor can select,
    /// such as whether the button is currently pressed or just toggles the active state.
    /// </summary>
    /// <seealso cref="InputTriggerType"/>
    /// <seealso cref="isSelectActive"/>
    public InputTriggerType selectActionTrigger
    {
        get => m_SelectActionTrigger;
        set => m_SelectActionTrigger = value;
    }

    [SerializeField]
    InputActionProperty m_SelectAction;
    /// <summary>
    /// The Input System action to use for selecting an Interactable.
    /// Must be an action with a button-like interaction where phase equals performed when pressed.
    /// Typically a <see cref="ButtonControl"/> Control or a Value type action with a Press or Sector interaction.
    /// </summary>
    /// <seealso cref="selectActionValue"/>
    public InputActionProperty selectAction
    {
        get => m_SelectAction;
        set => SetInputActionProperty(ref m_SelectAction, value);
    }


    [SerializeField]
    private Camera cameraNonXR;

    [SerializeField]
    LayerMask m_RaycastMask = -1;
    /// <summary>
    /// Gets or sets layer mask used for limiting ray cast targets.
    /// </summary>
    public LayerMask raycastMask
    {
        get => m_RaycastMask;
        set => m_RaycastMask = value;
    }
    [SerializeField]
    float m_MaxRaycastDistance = 30f;
    /// <summary>
    /// Gets or sets the max distance of ray cast when the line type is a straight line.
    /// Increasing this value will make the line reach further.
    /// </summary>
    /// <seealso cref="LineType.StraightLine"/>
    public float maxRaycastDistance
    {
        get => m_MaxRaycastDistance;
        set => m_MaxRaycastDistance = value;
    }

    bool m_AllowActivate = true;
    /// <summary>
    /// Defines whether this interactor allows sending activate and deactivate events.
    /// </summary>
    /// <seealso cref="allowHoveredActivate"/>
    /// <seealso cref="shouldActivate"/>
    /// <seealso cref="shouldDeactivate"/>
    public bool allowActivate
    {
        get => m_AllowActivate;
        set => m_AllowActivate = value;
    }


    /// <inheritdoc />
    public virtual bool shouldActivate =>
    m_AllowActivate && (hasSelection);

    /// <inheritdoc />
    public virtual bool shouldDeactivate =>
    m_AllowActivate && (hasSelection);

    static readonly List<IXRActivateInteractable> s_ActivateTargets = new List<IXRActivateInteractable>();

    readonly LinkedPool<ActivateEventArgs> m_ActivateEventArgs = new LinkedPool<ActivateEventArgs>(() => new ActivateEventArgs(), collectionCheck: false);
    readonly LinkedPool<DeactivateEventArgs> m_DeactivateEventArgs = new LinkedPool<DeactivateEventArgs>(() => new DeactivateEventArgs(), collectionCheck: false);


    protected override void Awake()
    {
        //targetsForSelection = new List<IXRSelectInteractable>();

        base.Awake();

        // If we are toggling selection and have a starting object, start out holding it
        if (m_SelectActionTrigger == InputTriggerType.Toggle && startingSelectedInteractable != null)
            m_ToggleSelectActive = true;
    }

    void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
    {
        if (Application.isPlaying)
            property.DisableDirectAction();

        property = value;

        if (Application.isPlaying && isActiveAndEnabled)
            property.EnableDirectAction();
    }


    public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.PreprocessInteractor(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            // Perform toggling of selection state for isSelectActive
            m_ToggleSelectDeactivatedThisFrame = false;
            if (m_SelectActionTrigger == InputTriggerType.Toggle)
            {
                if (m_ToggleSelectActive && m_SelectAction.action.WasPressedThisFrame())
                {
                    m_ToggleSelectActive = false;
                    m_ToggleSelectDeactivatedThisFrame = true;
                    m_WaitingForSelectDeactivate = true;
                }

                if (m_SelectAction.action.WasReleasedThisFrame())
                    m_WaitingForSelectDeactivate = false;
            }
        }
    }

    public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractor(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            // Send activate/deactivate events as necessary.
            if (m_AllowActivate)
            {
                var sendActivate = shouldActivate;
                var sendDeactivate = shouldDeactivate;
                if (sendActivate || sendDeactivate)
                {
                    GetActivateTargets(s_ActivateTargets);

                    if (sendActivate)
                        SendActivateEvent(s_ActivateTargets);

                    // Note that this makes it possible for an interactable to receive an OnDeactivated event
                    // but not the earlier OnActivated event if it was selected afterward.
                    if (sendDeactivate)
                        SendDeactivateEvent(s_ActivateTargets);
                }
            }
        }
    }

    void SendActivateEvent(List<IXRActivateInteractable> targets)
    {
        foreach (var interactable in targets)
        {
            if (interactable == null || interactable as Object == null)
                continue;

            using (m_ActivateEventArgs.Get(out var args))
            {
                args.interactorObject = this;
                args.interactableObject = interactable;
                interactable.OnActivated(args);
            }
        }
    }

    void SendDeactivateEvent(List<IXRActivateInteractable> targets)
    {
        foreach (var interactable in targets)
        {
            if (interactable == null || interactable as Object == null)
                continue;

            using (m_DeactivateEventArgs.Get(out var args))
            {
                args.interactorObject = this;
                args.interactableObject = interactable;
                interactable.OnDeactivated(args);
            }
        }
    }

    /// <inheritdoc />
    public virtual void GetActivateTargets(List<IXRActivateInteractable> targets)
    {
        targets.Clear();
        if (hasSelection)
        {
            foreach (var interactable in interactablesSelected)
            {
                if (interactable is IXRActivateInteractable activateInteractable)
                {
                    targets.Add(activateInteractable);
                }
            }
        }
    }

    public override bool isSelectActive
    {
        get
        {
            if (!base.isSelectActive)
                return false;

            if (isPerformingManualInteraction)
                return true;

            switch (m_SelectActionTrigger)
            {
                case InputTriggerType.State:
                    return m_SelectAction.action.IsPressed();

                case InputTriggerType.Toggle:
                    return m_ToggleSelectActive ||
                        (m_SelectAction.action.WasPressedThisFrame() && !m_ToggleSelectDeactivatedThisFrame);

                default:
                    return false;
            }
        }
    }

    public override void GetValidTargets(List<IXRInteractable> targets)
    {
        if (!Physics.queriesHitBackfaces) //for models with double-sided materials: detect hits, if backface is outside
            Physics.queriesHitBackfaces = true;

        targets.Clear();
        Ray ray = new Ray(cameraNonXR.transform.position, cameraNonXR.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, m_MaxRaycastDistance, m_RaycastMask))
        {

            XRBaseInteractable interactable = hit.collider.GetComponentInParent<XRBaseInteractable>();
            if (interactable != null)
            {
                Debug.DrawRay(cameraNonXR.transform.position, cameraNonXR.transform.forward, Color.green, 2, false);
                if (!targets.Contains(interactable))
                {
                    targets.Add(interactable);
                }
            }
        }
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);
        m_ToggleSelectActive = true;
        m_WaitingForSelectDeactivate = false;
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);
        if (hasSelection)
            return;

        m_ToggleSelectActive = false;
        m_WaitingForSelectDeactivate = false;
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return base.CanSelect(interactable) && (!hasSelection || IsSelecting(interactable));
    }
}
