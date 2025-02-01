using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Get random nearby position", story: "Gets a random nearby [position] [distance] from [object] [agent]", category: "Action", id: "adcd48a299f59a27f5c83a7a4c6f35b7")]
public partial class GetRandomNearbyPositionAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> Position;
    [SerializeReference] public BlackboardVariable<float> Distance;
    [SerializeReference] public BlackboardVariable<GameObject> Object;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;

    protected override Status OnStart()
    {
        if(Object.Value == null)
        {
            LogFailure("GetRandomNearbyPositionAction: Null object");
            return Status.Failure;
        }

        if(Distance.Value <= 0f)
        {
            Position.Value = Object.Value.transform.position;
            DebugExtension.DebugArrow(Position.Value + Vector3.up, Vector3.down, Color.cyan, 0.5f);
            Debug.Log($"GetRandomNearbyPositionAction: Destination {Position.Value}");
            return Status.Success;
        }

        Vector2 randomFlatOffset = UnityEngine.Random.insideUnitCircle * Distance.Value;
        Vector3 randomOffset = new(randomFlatOffset.x, Object.Value.transform.position.y, randomFlatOffset.y);
        float radius = randomOffset.magnitude;
        Vector3 randomPos = randomOffset + Object.Value.transform.position;

        if(NavMesh.SamplePosition(randomPos, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            Position.Value = hit.position;
            DebugExtension.DebugArrow(Position.Value + Vector3.up, Vector3.down, Color.cyan, 0.5f);

            Debug.Log($"GetRandomNearbyPositionAction: Destination {Position.Value}");
            return Status.Success;
        }

        Position.Value = randomPos;
        DebugExtension.DebugArrow(Position.Value + Vector3.up, Vector3.down, Color.cyan, 0.5f);
        Debug.Log($"GetRandomNearbyPositionAction: Destination {Position.Value}");
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

