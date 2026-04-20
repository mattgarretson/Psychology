using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(ThoughtUtility), nameof(ThoughtUtility.GiveThoughtsForPawnExecuted))]
public static class ThoughtUtility_ExecutedPatch
{
    [HarmonyPostfix]
    public static void BleedingHeartThoughts(Pawn victim, PawnExecutionKind kind)
    {
        if (!victim.RaceProps.Humanlike)
        {
            return;
        }
        int forcedStage = 1;
        if (victim.guilt.IsGuilty)
        {
            forcedStage = 0;
        }
        else
        {
            switch (kind)
            {
                case PawnExecutionKind.GenericBrutal:
                    forcedStage = 2;
                    break;
                case PawnExecutionKind.GenericHumane:
                    forcedStage = 1;
                    break;
                case PawnExecutionKind.OrganHarvesting:
                    forcedStage = 3;
                    break;
            }
        }
        ThoughtDef def;
        if (victim.IsColonist)
        {
            def = ThoughtDefOfPsychology.KnowColonistExecutedBleedingHeart;
        }
        else
        {
            def = ThoughtDefOfPsychology.KnowGuestExecutedBleedingHeart;
        }
        foreach (Pawn current in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners)
        {
            if (current.needs?.mood != null)
            {
                current.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(def, forcedStage), null);
            }
        }
    }
}

[HarmonyPatch(typeof(ThoughtUtility), nameof(ThoughtUtility.GiveThoughtsForPawnOrganHarvested))]
public static class ThoughtUtility_OrganHarvestedPatch
{
    [HarmonyPostfix]
    public static void BleedingHeartThoughts(Pawn victim)
    {
        if (!victim.RaceProps.Humanlike)
        {
            return;
        }
        ThoughtDef thoughtDef = null;
        if (victim.IsColonist)
        {
            thoughtDef = ThoughtDefOfPsychology.KnowColonistOrganHarvestedBleedingHeart;
        }
        else if (victim.HostFaction == Faction.OfPlayer)
        {
            thoughtDef = ThoughtDefOfPsychology.KnowGuestOrganHarvestedBleedingHeart;
        }
        foreach (Pawn current in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners)
        {
            if (current.needs?.mood == null)
            {
                continue;
            }
            if (current == victim)
            {
                current.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.MyOrganHarvested, null);
            }
            else if (thoughtDef != null)
            {
                current.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, null);
            }
        }
    }
}
