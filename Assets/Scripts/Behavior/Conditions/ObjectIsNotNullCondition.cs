using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Object is not null", story: "[Object] is not null", category: "Conditions", id: "b6e18abeafca53a64bf0e01660c3a14a")]
public partial class ObjectIsNotNullCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Object;

    public override bool IsTrue()
    {
        return Object.Value != null;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
