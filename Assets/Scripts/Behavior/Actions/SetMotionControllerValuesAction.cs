using Kabir.CharacterComponents;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Set MotionController values", story: "Sets [MotionController] values", category: "Action", id: "19387a5493d9f1c9935f6c6c8b0480fe")]
public partial class SetMotionControllerValuesAction : Action
{
    [SerializeReference] public BlackboardVariable<MotionController> MotionController;
    [SerializeReference] public BlackboardVariable<bool> FailOnMissing = new(false);
    [SerializeReference] public BlackboardVariable<float> GravityMultiplier = new(1f);
    [SerializeReference] public BlackboardVariable<bool> FollowTerrainGradient = new(true);
    [SerializeReference] public BlackboardVariable<float> FollowSlopeLimit = new(80f);
    [SerializeReference] public BlackboardVariable<bool> AutoEvaluate = new(true);

    protected override Status OnStart()
    {
        if(MotionController.Value == null)
        {
            if (FailOnMissing.Value)
            {
                LogFailure("Missing MotionController");
                return Status.Failure;
            }

            return Status.Success;
        }

        MotionController.Value.GravityMultiplier = GravityMultiplier.Value;
        MotionController.Value.FollowTerrainGradient = FollowTerrainGradient.Value;
        MotionController.Value.FollowSlopeLimit = FollowSlopeLimit.Value;
        MotionController.Value.AutoEvaluate = AutoEvaluate.Value;

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

