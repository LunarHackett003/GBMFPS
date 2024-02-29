using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equippable : MonoBehaviour
{
    public AnimationClipPair[] overrideClips;
}

public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
{
    public AnimationClipOverrides(int capacity) : base(capacity) { }

    public AnimationClip this[string name]
    {
        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
        set
        {
            int index = this.FindIndex(x => x.Key.name.Equals(name));
            if (index != -1)
                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
        }
    }
}
[System.Serializable]
public class AnimationClipPair
{
    public string name;
    public AnimationClip animationClip;
}
