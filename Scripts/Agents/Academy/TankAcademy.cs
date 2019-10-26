using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;


namespace TankDemoML {
    /// <summary>
    /// Academy for Tanks ML demo
    /// </summary>
    public class TankAcademy : Academy
    {
        public GameManager gameManager;
        private Transform monitor;
        private const string LOGTAG = "TankAcademy";


        public override void InitializeAcademy()
        {
            Debug.unityLogger.Log(LOGTAG, "InitializeAcademy");
            gameManager?.Initialize();
            monitor = GameObject.Find("Monitoring")?.transform;
        }

        public override void AcademyReset()
        {
            Debug.unityLogger.Log(LOGTAG, "AcademyReset");
        }

        public override void AcademyStep()
        {
            Monitor.Log("Step", GetStepCount().ToString(), monitor);
            Monitor.Log("Agent 0", gameManager.tankAgents[0].GetCumulativeReward(), monitor);
            Monitor.Log("Agent 1", gameManager.tankAgents[1].GetCumulativeReward(), monitor);
        }
    }
}
