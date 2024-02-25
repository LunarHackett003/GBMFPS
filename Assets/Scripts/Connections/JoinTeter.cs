using Starlight.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinTeter : MonoBehaviour
{
    public void TryJoin()
    {
        ConnectionManager.Instance.TryJoinRandomGame();
    }
}
