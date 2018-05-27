using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogAcademy : Academy
{

    public override void InitializeAcademy()
    {
        Monitor.verticalOffset = 1f;
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 12;
        // Time.fixedDeltaTime = 0.005f; //(240fps). default is .2 (60fps)
        // Time.fixedDeltaTime = 0.01f; //(120fps). default is .2 (60fps)
        Time.fixedDeltaTime = 0.01333f; //(75fps). default is .2 (60fps)
        Time.maximumDeltaTime = .15f; // default is .33
    }
    public override void AcademyReset()
    {


    }

    public override void AcademyStep()
    {


    }

}
