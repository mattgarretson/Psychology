using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine.UIElements;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(InteractionWorker_Breakup), nameof(InteractionWorker_Breakup.RandomSelectionWeight), new[] { typeof(Pawn), typeof(Pawn) })]
public static class InteractionWorker_RandomSelectionWeight_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(InteractionWorker_Breakup __instance, ref float __result, Pawn initiator, Pawn recipient)
    {
        if (initiator.story.traits.HasTrait(TraitDefOfPsychology.Codependent))
        {
            __result = 0f;
            return false;
        }
        return true;
    }

    [HarmonyPrefix] // Why was this commented out?
    public static bool NewSelectionWeight(InteractionWorker_Breakup __instance, ref float __result, Pawn initiator, Pawn recipient)
    {
        /* Also this one. */
        if (!LovePartnerRelationUtility.LovePartnerRelationExists(initiator, recipient))
        {
            __result = 0f;
            return false;
        }
        else if (initiator.story.traits.HasTrait(TraitDefOfPsychology.Codependent))
        {
            __result = 0f;
            return false;
        }
        float chance = 0.02f;
        float romanticFactor = 1f;
        if (PsycheHelper.PsychologyEnabled(initiator))
        {
            chance = 0.05f;
            romanticFactor = Mathf.InverseLerp(1.05f, 0f, PsycheHelper.Comp(initiator).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Romantic));
        }
        float opinionFactor = Mathf.InverseLerp(100f, -100f, (float)initiator.relations.OpinionOf(recipient));
        float spouseFactor = 1f;
        if (initiator.relations.DirectRelationExists(PawnRelationDefOf.Spouse, recipient))
        {
            spouseFactor = 0.4f;
        }
        __result = chance * romanticFactor * opinionFactor * spouseFactor;
        return false;
    }

}

[HarmonyPatch(typeof(InteractionWorker_Breakup), nameof(InteractionWorker_Breakup.Interacted))]
public static class InteractionWorker_Breakup_Interacted_Patch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
    {
        return RomanceHelperMethods.InterdictTryGainAndRemoveMemories(codes);
    }
}

