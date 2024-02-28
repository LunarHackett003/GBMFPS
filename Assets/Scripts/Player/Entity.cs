using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Starlight
{
    public class Entity : MonoBehaviour
    {
        [SerializeField] internal float maxHealth;
        [SerializeField] internal float currentHealth;
        [SerializeField] internal bool immortal;
        [SerializeField] internal UnityEvent deathEvents;
        [SerializeField] internal Rigidbody rb;
        [SerializeField] internal bool applyForce;
        [SerializeField] internal float forceInMultiplier;
        internal void TakeDamage(float damageIn)
        {
            if (!immortal)
            {
                currentHealth = Mathf.Clamp(currentHealth - damageIn, 0, maxHealth);
                if (currentHealth <= 0)
                    Die();
            }
        }
        internal void Heal(float healthIn)
        {
            if(!immortal)
                currentHealth = Mathf.Clamp(currentHealth + healthIn, 0, maxHealth);
        }
        internal void Die()
        {
            deathEvents?.Invoke();
        }
    }
}