using UnityEngine;

public class Bullet : MonoBehaviour
{
    private MeshRenderer renderer;
    private Rigidbody rb;

    private float lifeTime = 5f;
    private float timer;

    private void Awake()
    {
        renderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
    }

    public void Init(Vector3 velocity)
    {
        // Reset estado
        timer = 0f;

        // Reset f�sicas
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = velocity;
        }

        // Reset visual
        renderer.material.color = Color.red;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
        {
            DisableBullet();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DisableBullet();
    }

    void DisableBullet()
    {
        gameObject.SetActive(false);
    }
}