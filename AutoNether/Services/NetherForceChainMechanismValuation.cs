#nullable enable

namespace AutoNether.Services;

internal sealed class NetherForceChainMechanismValuation
{
    public NetherMechanismValue Evaluate(NetherForceChainPayoffInput input)
    {
        if (input == null || !input.CompletionTriggerKnown)
            return NetherMechanismValue.Missing("force-chain-completion-trigger-unavailable");
        if (!input.CompletionMessageReachable)
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.None,
                0,
                "force-chain-completion-unreachable"
            );
        if (!input.NumericalEffectKnown
            || input.TargetRow is not (NetherCodeTargetRow.Back or NetherCodeTargetRow.Forward))
        {
            return NetherMechanismValue.ReachableUnquantified(
                "force-chain-payoff-relationship-unavailable"
            );
        }

        return input.TargetRow == NetherCodeTargetRow.Back
            ? NetherMechanismValue.Qualitative(
                NetherMechanismQualitativePriority.BackForceChainHigh,
                "force-chain-completion-message;back-row-no-cadence"
            )
            : NetherMechanismValue.Qualitative(
                NetherMechanismQualitativePriority.FrontForceChainFallback,
                "force-chain-completion-message;front-row-fallback-no-cadence"
            );
    }
}
