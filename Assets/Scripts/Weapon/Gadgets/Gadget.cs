using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starlight.Weapons {
    /// <summary>
    /// Base class for all gadgets.
    /// </summary>
    public class Gadget : Equippable
    {
        public virtual void UseGadget()
        {
            print("player has used a " + gameObject.name);
        }
    }
}