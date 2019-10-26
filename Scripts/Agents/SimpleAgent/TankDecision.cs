using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class TankDecision : Decision
{
    public override float[] Decide(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        float[] actions = new float[3];

        int randomMove = Random.Range(0, 3);
        actions[0] = (float)randomMove;

        int randomTurn = Random.Range(0, 3);
        actions[1] = (float)randomTurn;

        int randomCannon = Random.Range(0, 6);
        actions[2] = (float)randomCannon;

        return actions;
    }

    public override List<float> MakeMemory(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        return default(List<float>);
    }
}
