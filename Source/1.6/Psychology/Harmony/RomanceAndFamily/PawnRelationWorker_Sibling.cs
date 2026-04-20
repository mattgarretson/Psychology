using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Linq;
using System.Reflection.Emit;

namespace Psychology.Harmony;

[HarmonyPatch(typeof(PawnRelationWorker_Sibling), "GenerateParentParams")]
public static class PawnRelationWorker_Sibling_GenerateParentParams_Patch
{
    [HarmonyPrefix]
    public static bool GenerateParentParams(ref float minChronologicalAge, ref float maxChronologicalAge, ref float midChronologicalAge, ref float minBioAgeToHaveChildren, Pawn generatedChild, Pawn existingChild)
    {
        float chrAge1 = generatedChild.ageTracker.AgeChronologicalYearsFloat;
        float chrAge2 = existingChild.ageTracker.AgeChronologicalYearsFloat;

        //float num4 = minChronologicalAge;

        SpeciesSettings settings = SpeciesHelper.GetOrMakeSpeciesSettingsFromThingDef(existingChild.def);
        float minLovingAge = settings.minLovinAge;
        if (minLovingAge < 0f)
        {
            minLovingAge = 0f;
        }
        if (minLovingAge == 0f || !settings.enableAgeGap)
        {
            //float maxBioAge = existingChild.RaceProps.lifeExpectancy;
            if (existingChild.RaceProps?.lifeExpectancy is float maxBioAge != true)
            {
                maxBioAge = 80f;
            }
            minChronologicalAge = chrAge1 + minLovingAge;
            midChronologicalAge = chrAge1 + 0.5f * (maxBioAge + minLovingAge);
            maxChronologicalAge = chrAge1 + maxBioAge;
            minBioAgeToHaveChildren = minLovingAge;
            return true;
        }
        float chrAgeMax = Mathf.Max(chrAge1, chrAge2);
        float num = minChronologicalAge - chrAgeMax;
        float num2 = maxChronologicalAge - chrAgeMax;
        float num3 = midChronologicalAge - chrAgeMax;

        // Convert from hardcoded vanilla values
        num = PsycheHelper.LovinBioAgeFromVanilla(num, minLovingAge);
        num2 = PsycheHelper.LovinBioAgeFromVanilla(num2, minLovingAge);
        num3 = PsycheHelper.LovinBioAgeFromVanilla(num3, minLovingAge);

        // Recompute
        minChronologicalAge = chrAgeMax + num;
        maxChronologicalAge = chrAgeMax + num2;
        midChronologicalAge = chrAgeMax + num3;
        minBioAgeToHaveChildren = num;
        return true;

        //SpeciesSettings settings = SpeciesHelper.GetOrMakeSpeciesSettingsFromThingDef(existingChild.def);
        //float minLovingAge = settings.minLovinAge;
        //float childChrAge = Mathf.Max(generatedChild.ageTracker.AgeChronologicalYearsFloat, existingChild.ageTracker.AgeChronologicalYearsFloat);
        //if (minLovingAge <= 0f || !settings.enableAgeGap)
        //{
        //    float maxBioAge = existingChild.RaceProps.lifeExpectancy;
        //    minChronologicalAge = childChrAge + minLovingAge;
        //    midChronologicalAge = childChrAge + 0.5f * (maxBioAge + minLovingAge);
        //    maxChronologicalAge = childChrAge + maxBioAge;
        //    minBioAgeToHaveChildren = minLovingAge;
        //    return true;
        //}
        //float minVanillaAge = minChronologicalAge - childChrAge;
        //float midVanillaAge = midChronologicalAge - childChrAge;
        //float maxVanillaAge = maxChronologicalAge - childChrAge;

        //minBioAgeToHaveChildren = PsycheHelper.LovinAgeFromVanilla(minVanillaAge, minLovingAge);
        //minChronologicalAge = childChrAge + minBioAgeToHaveChildren;
        //midChronologicalAge = childChrAge + PsycheHelper.LovinAgeFromVanilla(midVanillaAge, minLovingAge);
        //maxChronologicalAge = childChrAge + PsycheHelper.LovinAgeFromVanilla(maxVanillaAge, minLovingAge);
        //return true;
    }
}

public static class PawnRelationWorker_Sibling_ManualPatches
{
    public static IEnumerable<CodeInstruction> CreateRelation_Transpiler(IEnumerable<CodeInstruction> codes)
    {
        //Log.Message("CreateRelations_Transpiler, start");
        List<CodeInstruction> clist = codes.ToList();
        int max = clist.Count();
        bool bool1;
        bool bool2;

        //Log.Message("CreateRelations_Transpiler, start while");
        for (int i = 0; i < max; i++)
        {
            yield return clist[i];
            if (i >= max - 4)
            {
                continue;
            }
            bool1 = clist[i + 3].LoadsField(AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Gay)));
            bool2 = clist[i + 4].Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), new Type[] { typeof(TraitDef) }));
            if (!bool1 || !bool2)
            {
                continue;
            }
            //Log.Message("bools satisfied");
            yield return CodeInstruction.Call(typeof(PawnRelationWorker_Sibling_ManualPatches), nameof(DivorceBasedOnSexuality), new Type[] { typeof(Pawn) });
            //Log.Message("call satisfied");
            i += 4;
        }
        //Log.Message("CreateRelations_Transpiler, end");
    }

    public static bool DivorceBasedOnSexuality(Pawn parent)
    {
        bool flag = true;
        if (PsycheHelper.TryGetPawnSeed(parent) != true)
        {
            flag = PsycheHelper.HasTraitDef(parent, TraitDefOf.Gay);
            if (flag)
            {
                Log.Error("CreateRelations.DivorceBasedOnSexuality, TryGetPawnSeed(parent) != true but pawn has TraitDefOf.Gay");
            }
            return flag;
        }
        if (PsycheHelper.PsychologyEnabled(parent) != true)
        {
            flag = PsycheHelper.HasTraitDef(parent, TraitDefOf.Gay);
            if (flag)
            {
                Log.Error("CreateRelations.DivorceBasedOnSexuality, PsychologyEnabled(parent) != true but pawn has TraitDefOf.Gay");
            }
            return flag;
        }
        int kinsey = PsycheHelper.Comp(parent).Sexuality.kinseyRating;
        //flag = kinsey > 4;
        flag = kinsey > 5;
        //Log.Message("DivorceBasedOnSexuality fired, flag = " + flag);
        return flag;
    }
}

