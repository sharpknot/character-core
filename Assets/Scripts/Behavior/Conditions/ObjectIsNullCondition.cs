using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Object is null", story: "[Object] is null", category: "Conditions", id: "8608a2092db25ae7dfa25667adcc4728")]
public partial class ObjectIsNullCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Object;

    public override bool IsTrue()
    {
        return Object.Value == null;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
