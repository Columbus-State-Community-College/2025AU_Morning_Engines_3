using UnityEngine;

public class ShotgunShoot : MonoBehaviour
{
    [Header("References")]
    public Transform muzzlePoint;             // barrel tip
    public Transform shootDirectionReference; // usually the player camera
    public GameObject pelletPrefab;           // prefab with ShotgunPellet + collider + rigidbody

    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 6f;
    public float pelletSpeed = 80f;
    public float pelletDamage = 10f;
    public float pelletLifeTime = 2f;

    public void Fire()
    {
        if (muzzlePoint == null || pelletPrefab == null || shootDirectionReference == null)
            return;

        Vector3 baseDir = shootDirectionReference.forward;

        for (int i = 0; i < pelletsPerShot; i++)
        {
            Vector3 dir = GetSpreadDirection(baseDir);

            GameObject pelletObj = Instantiate(
                pelletPrefab,
                muzzlePoint.position,
                Quaternion.LookRotation(dir)
            );

            ShotgunPellet pellet = pelletObj.GetComponent<ShotgunPellet>();
            if (pellet != null)
            {
                pellet.Init(dir, pelletSpeed, pelletDamage, pelletLifeTime);
            }
        }
    }

    private Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        float yaw = Random.Range(-spreadAngle, spreadAngle);
        float pitch = Random.Range(-spreadAngle, spreadAngle);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 dir = rot * baseDirection;
        return dir.normalized;
    }
}
