using UnityEngine;
using System.Collections.Generic;

public class SemiPistol : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;
    public int poolSize = 10;

    private List<GameObject> bulletPool = new List<GameObject>();

    void Start()
    {
        // Crear pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }
    }

    void Update()
    {
        // Semiautomático (clic izquierdo)
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        GameObject bullet = GetBulletFromPool();

        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.Init(firePoint.forward * bulletSpeed);

            bullet.SetActive(true);
        }
    }

    GameObject GetBulletFromPool()
    {
        foreach (var bullet in bulletPool)
        {
            if (!bullet.activeInHierarchy)
                return bullet;
        }
        return null;
    }
}