using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(SocialInteractionUtility), "CanReceiveRandomInteraction")]
public static class InteractionUtility_CanReceive_Patch
{
    [HarmonyPostfix]
    public static void PsychologyAddonsForCanReceive(ref bool __result, Pawn p)
    {
        if (!__result) return;
        if (p.health.hediffSet.HasHediff(HediffDefOfPsychology.HoldingConversation))
        {
            __result = false;
            return;
        }
        var lord = p.Map.lordManager.lords.Find(l => l.LordJob is LordJob_VisitMayor);
        if (lord != null && lord.ownedPawns.Contains(p))
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(SocialInteractionUtility), "CanInitiateRandomInteraction", new[] { typeof(Pawn) })]
public static class InteractionUtility_CanInitiate_Patch
{
    [HarmonyPostfix]
    public static void PsychologyAddonsForCanInitiate(ref bool __result, Pawn p)
    {
        if (!__result) return;
        if (p.health.hediffSet.HasHediff(HediffDefOfPsychology.HoldingConversation))
        {
            __result = false;
            return;
        }
        var lord = p.Map.lordManager.lords.Find(l => l.LordJob is LordJob_VisitMayor);
        if (lord != null && lord.ownedPawns.Contains(p))
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(SocialInteractionUtility), nameof(SocialInteractionUtility.TryGetRandomVerbForSocialFight))]
public static class InteractionUtility_SocialFightVerb_Patch
{
    [HarmonyPostfix]
    public static void RemoveBiting(ref Verb verb, Pawn p)
    {
        if (verb == null)
        {
            return;
        }
        if (verb.verbProps?.meleeDamageDef?.label != "bite")
        {
            return;
        }
        (from x in p.verbTracker.AllVerbs
         where x.IsMeleeAttack && x.IsStillUsableBy(p) && x.verbProps?.meleeDamageDef?.label != "bite"
         select x).TryRandomElementByWeight((Verb x) => x.verbProps.AdjustedMeleeDamageAmount(x, p), out Verb v);
        if (v != null)
        {
            verb = v;
        }
    }
}

