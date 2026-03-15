using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Psychology
{
    public class Thought_MemoryDynamic : Thought_Memory
    {
        private string topic;
        private string label;
        private string description;
        private float duration;
        private float baseMoodEffect;
        private float stackedEffectMult = 1f;
        private int stackLim = 999;

        public Thought_MemoryDynamic()
        {
        }

        public override void ExposeData()
        {
            // On save: temporarily set defName to "Dynamic" for serialization
            // (base.ExposeData saves the def by defName; "Dynamic" is a sentinel
            // that won't resolve on load, triggering def reconstruction below)
            if (Scribe.mode == LoadSaveMode.Saving && this.def != null)
            {
                this.def.defName = "Dynamic";
            }
            base.ExposeData();
            Scribe_Values.Look(ref this.topic, "topic", "Dynamic");
            Scribe_Values.Look(ref this.label, "label", "dynamic thought");
            Scribe_Values.Look(ref this.description, "description", "a dynamic thought.");
            Scribe_Values.Look(ref this.duration, "duration", 5f);
            Scribe_Values.Look(ref this.baseMoodEffect, "realMoodEffect", 5f);
            Scribe_Values.Look(ref this.stackedEffectMult, "stackedEffectMult", 1f);
            Scribe_Values.Look(ref this.stackLim, "stackLim", 999);

            // Only recreate the def on load (when base.ExposeData couldn't resolve "Dynamic").
            // On save, the existing def is still valid — no need to allocate a new one.
            if (this.def == null)
            {
                ThoughtDef def = new ThoughtDef();
                def.defName = this.topic;
                def.label = "dynamic thought";
                def.description = this.description;
                def.durationDays = this.duration;
                def.thoughtClass = typeof(Thought_MemoryDynamic);
                def.stackedEffectMultiplier = this.stackedEffectMult;
                def.stackLimit = this.stackLim;
                ThoughtStage stage = new ThoughtStage();
                stage.label = this.label;
                stage.baseMoodEffect = this.baseMoodEffect;
                def.stages.Add(stage);
                this.def = def;
            }
        }

        public override void Init()
        {
            this.topic = def.defName;
            this.duration = def.durationDays;
            this.label = def.stages[0].label;
            this.description = def.stages[0].description;
            this.baseMoodEffect = def.stages[0].baseMoodEffect;
            this.stackedEffectMult = def.stackedEffectMultiplier;
            this.stackLim = def.stackLimit;
            base.Init();
        }

        public override bool GroupsWith(Thought other)
        {
            Thought_MemoryDynamic thought_MemoryDynamic = other as Thought_MemoryDynamic;
            return thought_MemoryDynamic != null && this.LabelCap == thought_MemoryDynamic.LabelCap;
        }


    }
}
