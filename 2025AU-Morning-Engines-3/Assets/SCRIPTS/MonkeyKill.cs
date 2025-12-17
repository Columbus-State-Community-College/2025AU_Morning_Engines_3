using UnityEngine;

public class MonkeyKillOnTouch : MonoBehaviour
{
    public string playerTag = "Player";
    public float instantKillDamage = 9999f;
    public float killRadius = 1.1f;

    private PlayerHealth playerHealth;
    private Transform player;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogError("MonkeyKillOnTouch: Player not found.");
        }
    }

    private void Update()
    {
        if (player == null || playerHealth == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= killRadius)
        {
            playerHealth.TakeDamage(instantKillDamage);
            Debug.Log("🐒 MONKEY KILLED PLAYER");
            enabled = false; // prevent repeat damage
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRadius);
    }
}
