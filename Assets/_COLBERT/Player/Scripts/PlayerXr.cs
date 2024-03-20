using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//local xr player
public class PlayerXr : Player
{
    [SerializeField]
    private Transform left;
    [SerializeField]
    private Transform right;

    public Transform Left => left;
    public Transform Right => right;

}
