using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;
using UnityEngine;

namespace Psychology
{
    class LordJob_VisitMayor : LordJob
    {
        private IntVec3 spot;
        private Trigger_TicksPassed timeoutTrigger;
        public Pawn constituent;
        public Pawn mayor;
        private bool complaint;
        public int ticksInSameRoom;

        public LordJob_VisitMayor()
        { }

        public LordJob_VisitMayor(IntVec3 spot)
        {
            this.spot = spot;
        }

        public LordJob_VisitMayor(IntVec3 spot, Pawn constituent, Pawn mayor, bool complaint)
        {
            this.spot = spot;
            this.constituent = constituent;
            this.mayor = mayor;
            this.complaint = complaint;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Meeting lordToil_Meeting = new LordToil_Meeting(this.spot);
            stateGraph.AddToil(lordToil_Meeting);
            LordToil_End lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);
            Transition transition = new Transition(lordToil_Meeting, lordToil_End);
            transition.AddTrigger(new Trigger_TickCondition(() => this.ShouldBeCalledOff()));
            transition.AddTrigger(new Trigger_TickCondition(() => this.mayor.health.summaryHealth.SummaryHealthPercent < 1f || this.constituent.health.summaryHealth.SummaryHealthPercent < 1f));
            transition.AddTrigger(new Trigger_TickCondition(() => this.mayor.Drafted || this.constituent.Drafted));
            transition.AddTrigger(new Trigger_PawnLost());
            stateGraph.AddTransition(transition);
            //Time of meeting is affected by the constituents' mood; meetings to complain can take longer than meetings to commend. Capped at 2 hours.
            this.timeoutTrigger = new Trigger_TicksPassed(Rand.RangeInclusive(GenDate.TicksPerHour, Mathf.Min(2 * GenDate.TicksPerHour, Mathf.RoundToInt(GenDate.TicksPerHour / Mathf.Lerp(0.2f, 1f, constituent.needs.mood.CurLevel)))));
            Transition transition2 = new Transition(lordToil_Meeting, lordToil_End);
            transition2.AddTrigger(this.timeoutTrigger);
            transition2.AddPreAction(new TransitionAction_Custom((Action)delegate
            {
                this.Finished();
            }));
            stateGraph.AddTransition(transition2);
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.spot, "spot", default(IntVec3));
            Scribe_References.Look(ref this.constituent, "constituent");
            Scribe_References.Look(ref this.mayor, "mayor");
            Scribe_Values.Look(ref this.complaint, "complaining");
            Scribe_Values.Look(ref this.ticksInSameRoom, "ticksInSameRoom", 0);
        }

        
        private void Finished()
        {
            if (!PsycheHelper.PsychologyEnabled(mayor) || !PsycheHelper.PsychologyEnabled(constituent) || this.ticksInSameRoom == 0)
            {
                return;
            }
            if (this.complaint)
            {
                ThoughtDef complaintDef = new ThoughtDef();
                complaintDef.label = "MayorComplaint";
                complaintDef.durationDays = 1f + 4f * this.mayor.GetStatValue(StatDefOf.SocialImpact);
                //Constituent thought duration affected by mayor's Social stat
                complaintDef.thoughtClass = typeof(Thought_MemoryDynamic);
                complaintDef.stackedEffectMultiplier = 0.5f;
                complaintDef.stackLimit = 5;
                ThoughtStage complaintStage = new ThoughtStage();
                //Venting is always positive for the constituent; empathetic mayors make it even better
                float empathy = PsycheHelper.Comp(mayor).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Empathetic);
                float complaintMood = 4f + 10f * empathy;
                //Scale by meeting length (fraction of an hour)
                complaintMood *= Mathf.Min((float)this.ticksInSameRoom / (float)GenDate.TicksPerHour, 1f);
                //Beauty of the room has a small positive effect
                complaintMood += 0.1f * BeautyUtility.AverageBeautyPerceptible(this.constituent.Position, this.constituent.Map);
                //Constituent's Judgmental trait changes how much the thought affects them
                complaintMood *= 0.5f + PsycheHelper.Comp(constituent).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Judgmental);
                complaintStage.label = "ComplaintLabel".Translate();
                complaintStage.description = "ComplaintDesc".Translate();
                complaintStage.baseMoodEffect = Mathf.RoundToInt(complaintMood);
                complaintDef.defName = this.constituent.GetHashCode() + "MayorComplaint" + complaintStage.baseMoodEffect;
                complaintDef.stages.Add(complaintStage);
                if (complaintStage.baseMoodEffect != 0)
                {
                    this.constituent.needs.mood.thoughts.memories.TryGainMemory(complaintDef, this.mayor);
                }
            }
            ThoughtDef visitDef = new ThoughtDef();
            visitDef.label = "MayorVisited";
            visitDef.durationDays = 0.75f + 2f * (1f - PsycheHelper.Comp(mayor).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Independent));
            //Mayor thought duration affected by mayor's Independent trait

            visitDef.thoughtClass = typeof(Thought_MemoryDynamic);
            visitDef.stackedEffectMultiplier = 0.75f; //1
            visitDef.stackLimit = 10; //999
            ThoughtStage stage = new ThoughtStage();
            float mood = 7f * (complaint ? -0.5f - (1f - this.constituent.needs.mood.CurLevel) : 0.1f + (this.constituent.needs.mood.CurLevel * 0.65f));
            //Base visit mood determined by the mood level of the constituent
            mood *= (float)this.ticksInSameRoom / (float)GenDate.TicksPerHour;
            //Length of meeting also affects mood
            mood *= (mood < 0f ? Mathf.Lerp(1.25f, 0.75f, PsycheHelper.Comp(constituent).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Polite)) : 1f);
            //Negative meeting thoughts (unhappy constituents) mitigated by constituent's politeness
            mood *= 0.5f + (1f - PsycheHelper.Comp(this.mayor).Psyche.GetPersonalityRating(PersonalityNodeDefOf.LaidBack));
            //Mayor's Laid-Back trait strongly impacts how much the thought affects them
            stage.label = "VisitLabel".Translate();
            stage.description = "VisitDesc".Translate();
            stage.baseMoodEffect = Mathf.RoundToInt(mood);
            visitDef.defName = this.mayor.GetHashCode() + "MayorVisited" + stage.baseMoodEffect;
            visitDef.stages.Add(stage);
            if (stage.baseMoodEffect != 0)
            {
                this.mayor.needs.mood.thoughts.memories.TryGainMemory(visitDef, this.constituent);
            }
            InteractionDef endConversation = new InteractionDef();
            endConversation.defName = "EndConversation";
            FieldInfo RuleStrings = typeof(RulePack).GetField("rulesStrings", BindingFlags.Instance | BindingFlags.NonPublic);
            RulePack goodbyeTextInit = new RulePack();
            List<string> text = new List<string>(1);
            if (complaint)
            {
                text.Add("r_logentry->" + "Complained".Translate(constituent, mayor));
            }
            else
            {
                text.Add("r_logentry->" + "Supported".Translate(constituent, mayor));
            }
            RuleStrings.SetValue(goodbyeTextInit, text);
            endConversation.logRulesInitiator = goodbyeTextInit;
            FieldInfo Symbol = typeof(InteractionDef).GetField("symbol", BindingFlags.Instance | BindingFlags.NonPublic);
            Symbol.SetValue(endConversation, Symbol.GetValue(InteractionDefOfPsychology.HangOut));
            PlayLogEntry_InteractionConversation log = new PlayLogEntry_InteractionConversation(endConversation, this.constituent, this.mayor, new List<RulePackDef>());
            Find.PlayLog.Add(log);
            MoteMaker.MakeInteractionBubble(this.mayor, this.constituent, InteractionDefOf.Chitchat.interactionMote, InteractionDefOf.Chitchat.GetSymbol()); // 1.3
        }

        public override string GetReport(Pawn pawn)
        {
            return "LordReportVisitingMayor".Translate();
        }

        private bool ShouldBeCalledOff()
        {
            if (this.ticksInSameRoom > 2 * GenDate.TicksPerHour
                || this.constituent.needs.food?.Starving == true || this.mayor.needs.food?.Starving == true
                || this.constituent.needs.rest?.CurLevel < 0.1f || this.mayor.needs.rest?.CurLevel < 0.1f
                || this.constituent.InMentalState || this.mayor.InMentalState)
            {
                return true;
            }
            return !GatheringsUtility.AcceptableGameConditionsToContinueGathering(base.Map) || this.constituent.GetTimeAssignment() == TimeAssignmentDefOf.Work || this.mayor.GetTimeAssignment() == TimeAssignmentDefOf.Work || (!this.spot.Roofed(base.Map) && !JoyUtility.EnjoyableOutsideNow(base.Map, null));
        }

        
    }
}
