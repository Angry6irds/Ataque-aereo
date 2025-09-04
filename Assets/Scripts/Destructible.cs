using UnityEngine;

public class Destructible : MonoBehaviour
{
    public float maxHealth = 50f;
    public GameObject deathVfx; 
    public bool destroyOnDeath = true;

    private float _health;

    void Awake()
    {
        _health = maxHealth;
    }

    public void ApplyDamage(float dmg, Vector3 hitPoint, Vector3 hitNormal)
    {
        _health -= dmg;

        if (_health <= 0f)
        {
            if (deathVfx)
            {
                Instantiate(deathVfx, hitPoint, Quaternion.LookRotation(hitNormal));
            }

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                var col = GetComponent<Collider>();
                if (col) col.enabled = false;
                var rend = GetComponentInChildren<Renderer>();
                if (rend) rend.enabled = false;
            }
        }
    }
}