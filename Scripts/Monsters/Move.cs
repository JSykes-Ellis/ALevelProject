using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public MoveBase BaseStats { get; set; }
    public int PP { get; set; }

    public Move(MoveBase pMoveBase)
    {
        BaseStats = pMoveBase;
        PP = pMoveBase.PP;
    }
}
