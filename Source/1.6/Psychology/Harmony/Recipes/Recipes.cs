using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Psychology.Harmony
{
    [StaticConstructorOnStartup]
    public static class PsychologyDefInjector
    {
        static PsychologyDefInjector()
        {
            RecipeDef[] recipesToAdd = new RecipeDef[]
            {
                RecipeDefOfPsychology.TreatChemicalInterest,
                RecipeDefOfPsychology.TreatChemicalFascination,
                RecipeDefOfPsychology.TreatDepression,
                RecipeDefOfPsychology.TreatInsomnia,
                RecipeDefOfPsychology.CureAnxiety,
                RecipeDefOfPsychology.TreatPyromania,
            };

            CompProperties_PartnerList partnerListProps = new CompProperties_PartnerList();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.race == null || !def.race.Humanlike)
                    continue;

                // Inject Psychology treatment recipes
                List<RecipeDef> recipes = def.AllRecipes;
                foreach (RecipeDef recipe in recipesToAdd)
                {
                    if (recipe != null && !recipes.Contains(recipe))
                    {
                        def.recipes ??= new List<RecipeDef>();
                        def.recipes.Add(recipe);
                    }
                }

                // Inject Comp_PartnerList for hookup/date partner tracking
                def.comps ??= new List<CompProperties>();
                if (!def.comps.Any(c => c is CompProperties_PartnerList))
                {
                    def.comps.Add(partnerListProps);
                }
            }
        }
    }
}
