using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;


namespace TankDemoML {
    /// <summary>
    /// Simple Tank Learning Agent
    /// </summary>
    public class SimpleTankAgent : Agent
    {
        // note that these are all copied from the existing tank scripts...
        // quick and dirty for now, can clean up later to DRY it up
        // maybe use scriptable objects for different tank profiles
        public float tankSpeed = 12f;
        public float turnSpeed = 180f;
        public float minCannonForce = 15f;
        public float maxCannonForce = 30f;
        public Rigidbody shellPrefab;
        public Transform shellSpawnPoint;
        public int id { get; set; }
        public float health;
        public GameObject target;

        private const float maxHealth = 100f;
        private Rigidbody rb;
        private bool cannonCoolingDown = true;
        private float cooldownTimer = 0f;
        private const float cooldownTime = 0.5f;
        private Transform spawnArea;
        private GameManager gameManager;
        private const string LOGTAG = "SimpleTankAgent";


        public override void InitializeAgent()
        {
            Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " InitializeAgent");
            rb = GetComponent<Rigidbody>();
            gameManager = GameObject.Find("Academy")?.GetComponent<GameManager>();
            if (!gameManager) Debug.unityLogger.Log(LOGTAG, "Warning: Could not find Game Manager");
        }

        public override void AgentReset()
        {
            // Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " AgentReset");
            health = maxHealth;
            cannonCoolingDown = true;
            cooldownTimer = 0f;
            gameManager.DestroyShells();  // NOTE: not sure if agents are all reset together. if not, shells may disappear mid-game
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            if (spawnArea)
            {
                float halfwidth = spawnArea.localScale.x / 2.0f;
                float halflength = spawnArea.localScale.z / 2.0f;
                float randomX = halfwidth * 2.0f * UnityEngine.Random.value - halfwidth;
                float randomZ = halflength * 2.0f * UnityEngine.Random.value - halflength;
                Vector3 randomOffset = new Vector3(randomX, 0f, randomZ);
                transform.position = spawnArea.position + randomOffset;

                float randomAngle = 90f * UnityEngine.Random.value - 45f;
                Quaternion randomQuaternion = Quaternion.Euler(0f, randomAngle, 0f);
                transform.rotation = spawnArea.rotation * randomQuaternion;
            }
        }

        void Update()
        {
            // handle cannonCoolingDown, doesn't seem to be working with coroutine
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownTime)
            {
                cooldownTimer = 0f;
                cannonCoolingDown = false;
            }
        }

        public void SetSpawnArea(Transform spawnArea)
        {
            this.spawnArea = spawnArea;
        }

        public override void CollectObservations()
        {
            if (cannonCoolingDown)
            {
                // mask all firing actions
                SetActionMask(2, 1);
                SetActionMask(2, 2);
                SetActionMask(2, 3);
                SetActionMask(2, 4);
                SetActionMask(2, 5);
            }

            // observations
            if (!target)
            {
                AddVectorObs(0.0f);  // distance to enemy (normalized based loosely on gun range)
                AddVectorObs(0.0f);  // bearing to enemy (normalized relative to agent, where 0 is forward, -1 is left 180, +1 is right 180)
                AddVectorObs(0.0f);  // our bearing from enemy point of view (relative to enemy, where 0 means enemy pointing directly at us, -1 is left 180, +1 is right 180)
                AddVectorObs(0.0f);  // line of sight to enemy (0/1)
            }
            else
            {
                float distance = Vector3.Distance(target.transform.position, transform.position);
                float distanceObs = Mathf.Clamp(distance / 50f, 0f, 1.0f);
                AddVectorObs(distanceObs);

                float relativeAngle = Vector3.SignedAngle(transform.forward, target.transform.position - transform.position, Vector3.up);
                float relativeAngleObs = Mathf.Clamp(relativeAngle / 180f, -1f, 1f);
                AddVectorObs(relativeAngleObs);

                // figure out which way the target is facing
                float targetRelativeAngle = Vector3.SignedAngle(target.transform.forward, transform.position - target.transform.position, Vector3.up);
                float targetRelativeAngleObs = Mathf.Clamp(targetRelativeAngle / 180f, -1f, 1f);
                AddVectorObs(targetRelativeAngle);

                RaycastHit hit;
                bool lineOfSight = false;
                if (Physics.SphereCast(transform.position, 0.75f, (target.transform.position - transform.position), out hit, distance + 10f))
                {
                    if (hit.transform.Equals(target.transform))
                    {
                        lineOfSight = true;
                    }
                }
                AddVectorObs(lineOfSight);
            }
        }

        public override void AgentAction(float[] vectorAction, string textAction)
        {
            int forwardAction = (int)vectorAction[0];
            int turnAction = (int)vectorAction[1];
            int cannonAction = (int)vectorAction[2];

            Vector3 forwardMove = Vector3.zero;
            float turn = 0;
            float cannonStrength = 0f;

            // NOTE: Not sure if we should be using Time.fixedDeltaTime
            // not sure if agent code is called in sync with physics

            switch (forwardAction)
            {
                case 0 : break;
                case 1 : forwardMove = transform.forward * tankSpeed * Time.deltaTime; break;
                case 2 : forwardMove = -transform.forward * tankSpeed * Time.deltaTime; break;
            }

            switch (turnAction)
            {
                case 0 : break;
                case 1 : turn = -turnSpeed * Time.deltaTime; break;
                case 2 : turn = turnSpeed * Time.deltaTime; break;
            }

            switch (cannonAction)
            {
                case 0 : break;
                case 1 : cannonStrength = 15.00f; break;
                case 2 : cannonStrength = 18.75f; break;
                case 3 : cannonStrength = 22.50f; break;
                case 4 : cannonStrength = 26.25f; break;
                case 5 : cannonStrength = 30.00f; break;
            }

            // move
            rb.MovePosition(rb.position + forwardMove);

            // turn
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);

            // fire cannon - assume action masking takes care of cannon reload cooldown
            if (cannonAction != 0)
            {
                FireCannon(cannonStrength);
            }

            AgentProcessRewards();
        }

        private void FireCannon(float strength)
        {
            if (cannonCoolingDown || IsDone()) return;

            cannonCoolingDown = true;
            cooldownTimer = 0f;
            // StartCoroutine(CannonCooldown());
            Rigidbody shell = Instantiate(shellPrefab, shellSpawnPoint.position, shellSpawnPoint.rotation) as Rigidbody;
            shell.velocity = strength * shellSpawnPoint.forward;

            SmartShell smartShell = shell.GetComponent<SmartShell>();
            if (smartShell)
            {
                smartShell.owner = id;
                smartShell.OnExplode += ReceiveDamageReport;
            }
        }

        // private IEnumerator CannonCooldown()
        // {
        //     yield return new WaitForSeconds(cooldownTime);
        //     cannonCoolingDown = false;
        // }

        private void AgentProcessRewards()
        {
            const float timePenalty = -0.0005f;
            AddReward(timePenalty);
        }

        private void ReceiveDamageReport(SmartShell shell, Dictionary<int, float> damageReport)
        {
            const float rewardPerDamage = 0.005f;  // max damage is 100
            const float penaltySelfHit = -1.5f;

            if (!IsDone())
            {
                foreach (var entry in damageReport)
                {
                    if (entry.Key == this.id)  // we hit ourself
                    {
                        AddReward(entry.Value * rewardPerDamage * penaltySelfHit);
                        // Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " hit themself for " + entry.Value.ToString());
                    }
                    else  // we hit someone else
                    {
                        AddReward(entry.Value * rewardPerDamage);
                        // Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " hit " + entry.Key.ToString() + " for " + entry.Value.ToString());
                    }
                }
            }

            shell.OnExplode -= ReceiveDamageReport;

            // after calculating rewards, check to see if we won the game
            if (damageReport.Count > 0 && !IsDone())
            {
                gameManager.CheckGameWinConditions();
            }
        }

        public void TakeDamage(float damage)
        {
            if (health <= 0f || IsDone()) return;  // don't keep taking damage if dead already

            health -= damage;
            const float penaltyPerDamage = -0.005f;   // max damage is 100
            AddReward(damage * penaltyPerDamage);
            // Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " takes " + damage.ToString() + " damage, remaining health: " + health.ToString());
        }

        public void WinGame()
        {
            if (IsDone()) return;
            AddReward(1.0f);
            Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " WinGame() with " + GetCumulativeReward().ToString() + " rewards, remaining health: " + health.ToString());
            rb.isKinematic = true;
            Done();
        }

        public void LoseGame()
        {
            if (IsDone()) return;
            AddReward(-1.0f);
            Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " LoseGame() with " + GetCumulativeReward().ToString() + " rewards, with health: " + health.ToString());
            rb.isKinematic = true;
            Done();
        }

        public void DrawGame()
        {
            if (IsDone()) return;
            Debug.unityLogger.Log(LOGTAG, "Player " + id.ToString() + " DrawGame() with" + GetCumulativeReward().ToString() + " rewards");
            rb.isKinematic = true;
            Done();
        }
    }
}
