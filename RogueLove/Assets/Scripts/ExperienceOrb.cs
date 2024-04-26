using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperienceOrb : ContactEnemy
{
    public override void Start()
    {
        base.Start();
        expSpawn = true;
    }

    public override void DirectionFacing()
    {
        return;
    }

    public override void PlayerCheck()
    {
        if (expSpawn) {
            force = Vector2.zero;
            force = Vector2.up * wanderSpeed * Time.deltaTime;
            StartCoroutine(Emerge());
        }
        if (!expSpawn) {
            target = player.position;
            Chase();
        }
    }

    private IEnumerator Emerge() {
        rb.AddForce(Vector2.up * new Vector2(0, 10) * Time.deltaTime, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.3f);
        expSpawn = false;
    }

    public override void RemoveEnemy() {
        PlayerController.SetExperience(PlayerController.GetExperience() + 1);
        Destroy(gameObject);
        //WalkerGenerator.SetDeadEnemy();
        //Debug.Log(WalkerGenerator.GetDeadEnemies() + "/" + WalkerGenerator.GetEnemyTotal());
    }
}