using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//base class for a player object
public abstract class Player : MonoBehaviour
{
    [SerializeField]
    private new Camera camera;

    public static readonly List<Player> players = new List<Player>();


    public Camera Camera => camera;
    public bool IsActive => isActiveAndEnabled;
    public virtual bool IsLocal => true;


    protected virtual void OnEnable()
    {
        players.Add(this);
    }

    protected virtual void OnDisable()
    {
        players.Remove(this);
    }
    
}
