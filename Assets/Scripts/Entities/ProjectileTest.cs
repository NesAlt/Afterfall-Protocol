using UnityEngine;

public class ProjectileTest : MonoBehaviour
{
    void Start()
    {
        GetComponent<Projectile>().Init(Vector2.right);
    }
}
