using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Kabir;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find pickup", story: "Find random [pickup]", category: "Action", id: "556a7a2a26ece61431d53ea326e4ffb1")]
public partial class FindPickupAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Pickup;
    private PickupSpawner _pickupSpawner;
    protected override Status OnStart()
    {
        if(_pickupSpawner == null)
        {
            _pickupSpawner = GameObject.FindFirstObjectByType<PickupSpawner>();
            if(_pickupSpawner == null)
            {
                Pickup.Value = null;
                return Status.Success;
            }
        }

        Pickup.Value = _pickupSpawner.SelectRandomPickup();

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

