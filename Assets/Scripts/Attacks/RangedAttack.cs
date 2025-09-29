using Unity.Mathematics;
using UnityEngine;

public class RangedAttack : Attack
{
    public override void DoAttack()
    {
        SpawnProjectile();
    }

    void SpawnProjectile()
    {
        ProjectileBase projectile = Instantiate(Data.ProjectilePrefab, transform.position, quaternion.identity).GetComponent<ProjectileBase>();

        if (GetComponentInParent<EnemyBase>())
        {
            projectile.InitProjectile(Data.Damage, Data.ProjectileSpeed, Data.Knockback, true, Player.Instance.transform.position);
        }
        else if (GetComponentInParent<Player>())
        {
            projectile.InitProjectile(Data.Damage, Data.ProjectileSpeed, Data.Knockback, false, Player.Instance.bFacingRight ? transform.position + new Vector3(1, 0, 0) * Data.ProjectileRange : transform.position + new Vector3(-1, 0, 0) * Data.ProjectileRange);
        }
        else Debug.Log("Huh");

        ResetAtkCD();

    }
}
