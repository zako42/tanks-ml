using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;


namespace TankDemoML {
    /// <summary>
    /// component to handle init and reset of environment
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const int numPlayers = 2;
        public GameObject[] tankPrefabs = new GameObject[numPlayers];
        public Transform[] playerSpawnAreas = new Transform[numPlayers];
        public Color[] playerColors = new Color[numPlayers];
        [HideInInspector] public SimpleTankAgent[] tankAgents = new SimpleTankAgent[numPlayers];

        private const string LOGTAG = "GameManager";


        /// <summary>
        /// Gets called by InitializeAcademy()
        /// </summary>
        public void Initialize()
        {
            SpawnTanks();

            // TODO: this is not scalable, need to do differently
            tankAgents[0].target = tankAgents[1].gameObject;
            tankAgents[1].target = tankAgents[0].gameObject;
        }

        private void SpawnTanks()
        {
            for (int i=0; i < numPlayers; i++)
            {
                GameObject tank = Instantiate(tankPrefabs[i], playerSpawnAreas[i].position, playerSpawnAreas[i].rotation);

                // set color
                MeshRenderer[] renderers = tank.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    renderer.material.color = playerColors[i];
                }

                // set spawn area so tank can reset itself
                var agent = tank.GetComponent<SimpleTankAgent>();
                if (agent)
                {
                    tankAgents[i] = agent;
                    agent.SetSpawnArea(playerSpawnAreas[i]);
                    agent.id = i;
                }
            }
        }

        public void CheckGameWinConditions()
        {
            // Debug.unityLogger.Log(LOGTAG, "Checking win conditions...");
            int deads = tankAgents.Count(t => t.health <= 0f);
            if (deads == numPlayers)
            {
                // Debug.unityLogger.Log(LOGTAG, "DRAW GAME");
                DestroyShells();
                // everyone dead, draw game
                foreach (var tank in tankAgents)
                {
                    tank.DrawGame();
                }
            }
            else if (deads == numPlayers - 1)
            {
                DestroyShells();

                // one winner left standing
                var winner = tankAgents.First(t => t.health > 0f);
                // Debug.unityLogger.Log(LOGTAG, "PLAYER " + winner.id.ToString() + " WON");
                winner.WinGame();

                SimpleTankAgent[] losers = tankAgents.Where(t => t.health <= 0f).ToArray();
                foreach (var loser in losers)
                {
                    loser.LoseGame();
                }
            }
            else
            {
                // Debug.unityLogger.Log(LOGTAG, "No win condition");
            }
        }

        public void DestroyShells()
        {
            GameObject[] shells = GameObject.FindGameObjectsWithTag("SmartShell");
            // Debug.unityLogger.Log(LOGTAG, "End game, found " + shells.Length.ToString() + " shells to destroy");

            foreach (var shellobject in shells)
            {
                shellobject.SetActive(false);
                Destroy(shellobject.gameObject);
            }
        }
    }
}
