using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreTraded))]
public static class Pawn_PreTradedPatch
{
    [HarmonyPostfix]
    public static void BleedingHeartThought(Pawn __instance, TradeAction action, Pawn playerNegotiator, ITrader trader)
    {
        if (action == TradeAction.PlayerSells)
        {
            if (__instance.RaceProps.Humanlike)
            {
                foreach (Pawn current in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners)
                {
                    if (current.needs?.mood != null)
                    {
                        current.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.KnowPrisonerSoldBleedingHeart, null);
                    }
                }
            }
        }
    }
}
