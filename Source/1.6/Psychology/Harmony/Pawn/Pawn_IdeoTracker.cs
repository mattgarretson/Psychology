using Verse;
using HarmonyLib;
using RimWorld;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.CertaintyChangePerDay), MethodType.Getter)]
public static class Pawn_IdeoTracker_CertaintyChangePerDay_Patch
{
    [HarmonyPostfix]
    public static void CertaintyChangePerDay(ref float __result, Pawn ___pawn)
    {
        if (!PsycheHelper.PsychologyEnabled(___pawn))
        {
            return;
        }
        __result += Current.Game.GetComponent<PsychologyGameComponent>().CertaintyChange(___pawn, true);
    }
}
