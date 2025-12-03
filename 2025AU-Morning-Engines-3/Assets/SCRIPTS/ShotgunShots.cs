using UnityEngine;

public class ShotgunShooter : MonoBehaviour
{
    [Header("Auto Attach To Player")]
    [Tooltip("Usually your Player camera transform. The shotgun will sit in front of this.")]
    public Transform playerForwardReference;

    [Tooltip("Local offset from the player/camera: X = right, Y = up, Z = forward.")]
    public Vector3 localOffset = new Vector3(0.3f, -0.3f, 0.8f);

    [Header("Weapon Setup")]
    [Tooltip("Where pellets spawn (tip of barrel). Should be a child of this shotgun.")]
    public Transform barrelTip;

    [Tooltip("Pellet prefab with Pellet.cs, small sphere + trigger collider.")]
    public GameObject pelletPrefab;

    [Header("Shot Settings")]
    public int pelletCount = 8;
    public float pelletSpread = 6f;
    public float pelletSpeed = 60f;
    public float pelletDamage = 10f;
    public float fireRate = 0.7f;

    private float lastFireTime = 0f;

    private void LateUpdate()
    {
        // 1) SNAP SHOTGUN IN FRONT OF PLAYER, BLUE ARROW FORWARD
        if (playerForwardReference != null)
        {
            // Position: camera position + (camera's local right/up/forward * offset)
            transform.position = playerForwardReference.position +
                                 playerForwardReference.TransformDirection(localOffset);

            // Rotation: match camera's forward/up so Z+ (blue arrow) points forward
            transform.rotation = Quaternion.LookRotation(
                playerForwardReference.forward,
                playerForwardReference.up
            );
        }

        // 2) SHOOT INPUT
        if (Input.GetMouseButton(0) && Time.time >= lastFireTime + fireRate)
        {
            Shoot();
            lastFireTime = Time.time;
        }
    }

    private void Shoot()
    {
        if (barrelTip == null || pelletPrefab == null)
        {
            Debug.LogWarning("ShotgunShooter: Missing barrelTip or pelletPrefab.");
            return;
        }

        for (int i = 0; i < pelletCount; i++)
        {
            // Start from barrel forward
            Vector3 dir = barrelTip.forward;

            // Add some spread
            dir.x += Random.Range(-pelletSpread, pelletSpread) * 0.02f;
            dir.y += Random.Range(-pelletSpread, pelletSpread) * 0.02f;
            dir.Normalize();

            // Spawn pellet
            GameObject pelletObj = Instantiate(
                pelletPrefab,
                barrelTip.position,
                Quaternion.LookRotation(dir)
            );

            // Configure pellet
            Pellet p = pelletObj.GetComponent<Pellet>();
            if (p != null)
            {
                p.damage = pelletDamage;
                p.speed = pelletSpeed;
            }
        }
    }
}
