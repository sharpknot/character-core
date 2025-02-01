using Kabir;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Find deposit object", story: "Find [deposit] [object]", category: "Action", id: "e2cf8bb1c90d08f2ccb6d446e9197a60")]
public partial class FindDepositObjectAction : Action
{
    [SerializeReference] public BlackboardVariable<DepositMachine> Deposit;
    [SerializeReference] public BlackboardVariable<GameObject> Object;

    protected override Status OnStart()
    {
        if(Deposit.Value != null)
        {
            Object.Value = Deposit.Value.gameObject;
            return Status.Success;
        }

        Deposit.Value = GameObject.FindFirstObjectByType<DepositMachine>();
        if (Deposit.Value != null)
        {
            Object.Value = Deposit.Value.gameObject;
            return Status.Success;
        }

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

