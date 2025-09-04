using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 25f;
    public float explosionForce = 0f; 
    public float explosionRadius = 0f;

    [Header("Fragmentation")]
    public bool fragmentOnImpact = false;
    public int fragments = 6;
    public float fragmentSpreadAngle = 20f; 
    public float fragmentSpeedMultiplier = 0.75f;
    public float fragmentAfterSeconds = -1f; 

    [Header("FX")]
    public GameObject impactVfx;
    public LayerMask hitMask = ~0;

    public event Action OnBulletFinished;

    Rigidbody _rb;
    bool _finished;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (fragmentAfterSeconds > 0f)
            StartCoroutine(FragmentTimer(fragmentAfterSeconds));
    }

    IEnumerator FragmentTimer(float t)
    {
        yield return new WaitForSeconds(t);
        DoFragmentation(transform.position, transform.forward);
        Finish();
    }

    void OnCollisionEnter(Collision col)
    {
        if (_finished) return;

        Vector3 point = col.contacts.Length > 0 ? col.contacts[0].point : transform.position;
        Vector3 normal = col.contacts.Length > 0 ? col.contacts[0].normal : -transform.forward;

        if (impactVfx) Instantiate(impactVfx, point, Quaternion.LookRotation(normal));

        Destructible d = col.collider.GetComponentInParent<Destructible>();
        if (d != null) d.ApplyDamage(damage, point, normal);

        if (explosionRadius > 0f && explosionForce > 0f)
        {
            foreach (var hit in Physics.OverlapSphere(point, explosionRadius, hitMask, QueryTriggerInteraction.Ignore))
            {
                Rigidbody hrb = hit.attachedRigidbody;
                if (hrb) hrb.AddExplosionForce(explosionForce, point, explosionRadius, 0.0f, ForceMode.Impulse);
            }
        }

        if (fragmentOnImpact)
        {
            DoFragmentation(point, transform.forward);
        }

        Finish();
    }

    void DoFragmentation(Vector3 origin, Vector3 forward)
    {
        if (fragments <= 0 || fragmentSpeedMultiplier <= 0f) return;

        Rigidbody myRb = _rb;
        Vector3 baseVel = myRb ? GetRbVelocity(myRb) : forward * 30f;
        float speed = baseVel.magnitude * fragmentSpeedMultiplier;

        for (int i = 0; i < fragments; i++)
        {
            float yaw = UnityEngine.Random.Range(-fragmentSpreadAngle, fragmentSpreadAngle);
            float pitch = UnityEngine.Random.Range(-fragmentSpreadAngle, fragmentSpreadAngle);
            Quaternion spread = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = spread * forward;

            GameObject frag = Instantiate(gameObject, origin, Quaternion.LookRotation(dir));
            Bullet b = frag.GetComponent<Bullet>();
            if (b != null)
            {
                b.fragmentOnImpact = false;      
                b.fragmentAfterSeconds = -1f;    
            }

            Rigidbody rb = frag.GetComponent<Rigidbody>();
            if (rb) SetRbVelocity(rb, dir.normalized * speed);

            Destroy(frag, 10f);
        }
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;
        OnBulletFinished?.Invoke();
        Destroy(gameObject);
    }

    Vector3 GetRbVelocity(Rigidbody rb)
    {
        var t = rb.GetType();
        var prop = t.GetProperty("linearVelocity");
        if (prop != null && prop.CanRead)
        {
            return (Vector3)prop.GetValue(rb, null);
        }
        return rb.linearVelocity;
    }

    void SetRbVelocity(Rigidbody rb, Vector3 vel)
    {
        var t = rb.GetType();
        var prop = t.GetProperty("linearVelocity");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(rb, vel, null);
        }
        else
        {
            rb.linearVelocity = vel;
        }
    }
}