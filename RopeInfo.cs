using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RopeInfo
{
    public RopeState state;

    List<Weapon> _weaponsInRope;
    public List<Weapon> WeaponsInRope
    {
        get
        {
            if (_weaponsInRope == null)
                _weaponsInRope = new();

            return _weaponsInRope;
        }

        set { _weaponsInRope = value; }
    }
}

// Enum to check which side this rope belongs to.
public enum Orientation { left, right }

// Property used to indicate and change the state of the rope.
public enum RopeState { empty, drawing, full }
