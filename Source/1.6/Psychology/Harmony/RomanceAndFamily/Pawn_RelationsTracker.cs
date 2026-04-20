using System;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.Notify_RescuedBy))]
public static class Notify_RescuedBy_BleedingHeartPatch
{

    [HarmonyPostfix]
    public static void AddBleedingHeartThought(Pawn_RelationsTracker __instance, Pawn rescuer, Pawn ___pawn)
    {
        if (rescuer.needs.mood != null && __instance.canGetRescuedThought)
        {
            //rescuer.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.RescuedBleedingHeart, Traverse.Create(__instance).Field("pawn").GetValue<Pawn>());
            rescuer.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.RescuedBleedingHeart, ___pawn);
        }
    }
}

[HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor))]
public static class Pawn_RelationsTracker_LovinChancePatch
{
    [HarmonyPrefix]
    public static bool SecondaryLovinChanceFactor(Pawn_RelationsTracker __instance, Pawn otherPawn, Pawn ___pawn, ref float __result)
    {
        Pawn pawn = ___pawn;
        if (pawn == otherPawn)
        {
            __result = 0f;
            return false;
        }

        // Disable psyche for a species means no personality or sexuality generated
        // However, should we use the vanilla formula or just diable all dating?
        if (!PsycheHelper.PsychologyEnabled(pawn) || !PsycheHelper.PsychologyEnabled(otherPawn))
        {
            //Log.Message("SecondaryLovinChanceFactor, Psychology not enabled for pawn and/or otherPawn");
            __result = 0f; // Disable all dating for pawns with no psyche
            return false;
            //return true // Use the vanilla formula
        }
        

        /* SEXUAL PREFERENCE FACTOR */
        float sexualityFactor = 1f;
        /* Psychology result */
        if (PsychologySettings.enableKinsey)
        {
            float kinseyFactor = PsycheHelper.Comp(pawn).Sexuality.kinseyRating / 3f;
            sexualityFactor = Mathf.Clamp01(pawn.gender == otherPawn.gender ? kinseyFactor : 2f - kinseyFactor);
        }
        // Vanilla Asexual, Bisexual, and Gay traits
        else if (pawn.story != null && pawn.story.traits != null)
        {
            if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
            {
                __result = 0f;
                return false;
            }
            if (!pawn.story.traits.HasTrait(TraitDefOf.Bisexual))
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Gay))
                {
                    if (otherPawn.gender != pawn.gender)
                    {
                        __result = 0f;
                        return false;
                    }
                }
                else if (otherPawn.gender == pawn.gender)
                {
                    __result = 0f;
                    return false;
                }
            }
        }

        /* AGE FACTOR */
        float ageFactor = CalculateAgeFactor(pawn, otherPawn);
        if (ageFactor == 0f)
        {
            //Log.Message("SecondaryLovinChanceFactor, ageFactor = 0");
            __result = 0f;
            return false;
        }

        /* BEAUTY FACTOR */
        float pawnBeauty = pawn.GetStatValue(StatDefOf.PawnBeauty);
        float otherPawnBeauty = otherPawn.GetStatValue(StatDefOf.PawnBeauty);
        float otherPawnCool = PsycheHelper.Comp(otherPawn).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Cool);
        float pawnOpenMinded = pawn.story.traits.HasTrait(TraitDefOfPsychology.OpenMinded) ? 1f : 0f;

        /* Beautiful pawns will have higher beauty standards. Everyone wants to date out of league */
        float physicalFactor = otherPawnBeauty - 0.75f * pawnBeauty;
        /* Open Minded pawns don't care about physical beauty */
        physicalFactor *= 1f - pawnOpenMinded;
        /* Pawns who can't see as well can't determine physical beauty as well. */
        physicalFactor *= 0.1f + 0.9f * pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);

        /* Cool pawns are more attractive */
        float personalityFactor = 2f * otherPawnCool - 1f;

        // Men will care more about physical beauty, women will care more about personality
        physicalFactor *= pawn.gender == Gender.Male ? 1.6f : 0.6f;
        personalityFactor *= pawn.gender == Gender.Female ? 1.6f : 0.6f;

        /* Turn into multiplicative factor. This ranges between 0.27 and 2.85 */
        float beautyFactor = 0.1f + Mathf.Pow(0.5f + 1f / (1f + Mathf.Pow(4f, -physicalFactor - personalityFactor + 0.12f)), 2.5f);

        /* PAWN SEX AND ROMANCE DRIVE FACTORS */
        float pawnSexDrive = PsycheHelper.Comp(pawn).Sexuality.AdjustedSexDrive;
        float pawnRomanceDrive = PsycheHelper.Comp(pawn).Sexuality.AdjustedRomanticDrive;
        float pawnDriveFactor = pawnRomanceDrive + 0.25f * pawnSexDrive;

        /*  MULTIPLY TO GET RESULT */
        __result = sexualityFactor * ageFactor * beautyFactor * pawnDriveFactor;
        //Log.Message("SecondaryLovinChanceFactor between " + pawn.LabelShort + " and " + otherPawn.LabelShort + ", sexualityFactor = " + sexualityFactor + ", ageFactor = " + ageFactor + ", beautyFactor = " + beautyFactor + ", pawnDriveFactor = " + pawnDriveFactor + ", result = " + __result);
        return false;
    }

    public static float CalculateAgeFactor(Pawn pawn, Pawn otherPawn)
    {
        if (!SpeciesHelper.RomanceEnabled(pawn, true) || !SpeciesHelper.RomanceEnabled(otherPawn, true))
        {
            // No romance factor for children, no exceptions
            return 0f;
        }

        float age1 = pawn.ageTracker.AgeBiologicalYearsFloat;
        float age2 = otherPawn.ageTracker.AgeBiologicalYearsFloat;
        SpeciesSettings settings1 = PsychologySettings.speciesDict[pawn.def.defName];
        SpeciesSettings settings2 = PsychologySettings.speciesDict[otherPawn.def.defName];
        float minAge1 = settings1.minDatingAge;
        float minAge2 = PsychologySettings.speciesDict[otherPawn.def.defName].minDatingAge;
        bool pawnLecher = pawn.story.traits.HasTrait(TraitDefOfPsychology.Lecher);
        if (minAge1 < 0f || age1 < minAge1)
        {
            // No underage initiators
            return 0f;
        }
        if (minAge2 == 0f)
        {
            // Attractiveness of ageless pawns does not depend on age
            return 1f;
        }
        if (minAge2 < 0f)
        {
            // Lechers are gross and will hit on aromantic species
            return pawnLecher ? 1f : 0f;
        }
        if (age2 < minAge2 && !pawnLecher)
        {
            // Lechers are gross and will hit on underage pawns
            return 0f;
        }
        float scaledAge2 = PsycheHelper.DatingBioAgeToVanilla(age2, minAge2);
        if (minAge1 == 0f)
        {
            return pawnLecher ? 1f : Mathf.InverseLerp(14f, 18f, scaledAge2);
        }
        float scaledAge1 = PsycheHelper.DatingBioAgeToVanilla(age1, minAge1);
        float ageFactor = pawnLecher ? 1f : Mathf.InverseLerp(14f, Mathf.Clamp(0.5f * scaledAge1 + 7f, 14f, 18f), scaledAge2);
        if (settings1.enableAgeGap && settings2.enableAgeGap)
        {
            float pawnOpenMinded = pawn.story.traits.HasTrait(TraitDefOfPsychology.OpenMinded) ? 1f : 0f;
            float pawnExperimental = PsycheHelper.Comp(pawn).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Experimental);
            float pawnPure = PsycheHelper.Comp(pawn).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Pure);
            //float minY = Mathf.Clamp01(0.2f + 0.8f * Mathf.Pow(pawnExperimental, 2) - 0.4f * pawnPure + 0.5f * pawnOpenMinded);
            float minY = Mathf.Clamp01(Mathf.Pow(0.5f * (pawnExperimental + 1f - pawnPure), 2.3f) + 0.5f * pawnOpenMinded);

            float pawnKinseyFactor = Mathf.InverseLerp(6f, 0f, PsycheHelper.Comp(pawn).Sexuality.kinseyRating);

            // Maybe one day other genders will come to the Rim...
            float pawnGenderFactor = pawn.gender == Gender.Female ? 1f : pawn.gender == Gender.Male ? -1f : 0f;


            float smallShift = 3.5f * pawnKinseyFactor * pawnGenderFactor;
            float largeShift = 10f * pawnKinseyFactor * pawnGenderFactor;

            List<float> offsets = new List<float>() { -20f + largeShift, -6.5f + smallShift, 6.5f + smallShift, 20f + largeShift };
            //Log.Message("Age factor for pawn1 = " + pawn.LabelShort + ", pawn2 = " + otherPawn.LabelShort);
            ageFactor *= AgeGapFactor(scaledAge1, scaledAge2, minY, pawnLecher, offsets);
        }
        return ageFactor;
    }

    public static float AgeGapFactor(float age1, float age2, float minY, bool lecher, List<float> offsets)
    {
        if (lecher)
        {
            if (age1 > age2)
            {
                // Gross
                return 1f;
            }
            minY = 0.5f * (minY + 1f);
        }
        float olderAgeGapFactor = Mathf.Max(1f, age1 / 30f);
        float min = age1 + offsets[0] * olderAgeGapFactor;
        float lower = age1 + offsets[1] * olderAgeGapFactor;
        float upper = age1 + offsets[2] * olderAgeGapFactor;
        float max = age1 + offsets[3] * olderAgeGapFactor;
        float result = GenMath.FlatHill(minY, min, lower, upper, max, minY, age2);
        //Log.Message("age1 = " + age1 + ", age2 = " + age2 + ", min = " + min + ", lower = " + lower + ", upper = " + upper + ", max = " + max + ", minY = " + minY + ", result = " + result);
        return result;
    }

}