using UnityEngine;

public class ShotgunShoot : MonoBehaviour
{
    [Header("References")]
    public Transform muzzlePoint;
    public Transform shootDirectionReference;
    public GameObject pelletPrefab;

    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 6f;
    public float pelletSpeed = 80f;
    public float pelletDamage = 10f;
    public float pelletLifeTime = 2f;

    public void Fire()
    {
        if (muzzlePoint == null || shootDirectionReference == null || pelletPrefab == null)
        {
            Debug.LogWarning("ShotgunShoot: missing reference (muzzlePoint, shootDirectionReference, or pelletPrefab).");
            return;
        }

        UpgradeStatsManager.EnsureExists();

        int effectivePellets = Mathf.Max(0, pelletsPerShot + UpgradeStatsManager.GetPelletsBonus());
        float effectiveDamage = Mathf.Max(0f, pelletDamage + UpgradeStatsManager.GetDamageBonus());

        if (effectivePellets <= 0)
        {
            Debug.LogWarning("ShotgunShoot: pelletsPerShot <= 0, nothing will spawn.");
            return;
        }

        if (pelletSpeed <= 0f)
        {
            Debug.LogWarning("ShotgunShoot: pelletSpeed <= 0, pellets will not move.");
        }

        Vector3 baseDir = shootDirectionReference.forward;

        Debug.DrawRay(muzzlePoint.position, baseDir * 5f, Color.red, 0.5f);

        for (int i = 0; i < effectivePellets; i++)
        {
            Vector3 dir = GetSpreadDirection(baseDir);

            GameObject pelletObj = Object.Instantiate(
                pelletPrefab,
                muzzlePoint.position,
                Quaternion.LookRotation(dir)
            );

            ShotgunPellet pellet = pelletObj.GetComponent<ShotgunPellet>();
            if (pellet != null)
            {
                pellet.Init(dir, pelletSpeed, effectiveDamage, pelletLifeTime);
            }
            else
            {
                Debug.LogWarning("ShotgunShoot: spawned pelletPrefab without ShotgunPellet component.");
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
