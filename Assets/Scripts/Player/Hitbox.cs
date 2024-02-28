using Starlight;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starlight 
{
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] internal Entity entity;
        [SerializeField] internal float damageMultiplier = 1;
        private void Start()
        {
            if(!TryGetComponent(out Entity ent))
                ent = GetComponentInParent<Entity>();
            if (ent)
                entity = ent;
        }
        public void HitEntity(float damageIn, Vector3 damageOrigin)
        {
            entity.TakeDamage(damageIn * damageMultiplier);
            if (entity.applyForce)
            {
                entity.rb.AddForceAtPosition((transform.position - damageOrigin) * damageIn * damageMultiplier * entity.forceInMultiplier, transform.position);
            }
        }
    }
}