using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrawlerAcademy : Academy
{
    public float targetSpawnRadius;
    public Transform target;
    // public float changeTargetPosFrequency; //how often should the target pos change

    public override void InitializeAcademy()
    {
        Monitor.verticalOffset = 1f;
        GetRandomTargetPos();
    }

    public override void AcademyReset()
    {


    }

    public override void AcademyStep()
    {


    }

    public void GetRandomTargetPos()
    {
        Vector3 newTargetPos = Random.insideUnitSphere * targetSpawnRadius;
		newTargetPos.y = 5;
		target.position = newTargetPos;
    }
    // IEnumerator MoveTarget()
    // { 
    //     // * not using new WaitForSeconds to avoid garbage alloc
    //     float elapsed = 0;
    //     while(true)
    //     {
    //         if(elapsed < changeTargetPosFrequency)
    //         {
    //             elapsed += Time.deltaTime;
    //             yield return null;
    //         }
    //         {
    //             target.position = Random.InsideUnitSphere(targetSpawnRadius);
    //         }
    //     }
    // }
}
