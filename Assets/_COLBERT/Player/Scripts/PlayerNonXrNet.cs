using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//remote non-xr player
public class PlayerNonXrNet : PlayerNonXr
{
    public override bool IsLocal => false;

}
