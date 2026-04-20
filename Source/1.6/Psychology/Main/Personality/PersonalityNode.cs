using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Diagnostics;

namespace Psychology;

public class PersonalityNode : IExposable
{
    public Pawn pawn;
    public PersonalityNodeDef def;
    public float rawRating;
    private float cachedRating = -1f;

    public bool HasConvoTopics => this.def.conversationTopics.NullOrEmpty() != true;
    public bool HasPlatformIssue => this.def.platformIssueHigh != null && this.def.platformIssueLow != null;
    public string PlatformIssue => this.AdjustedRating < 0.5f ? this.def.platformIssueLow : this.def.platformIssueHigh;
    public float AdjustedRating
    {
        get
        {
            return cachedRating;
        }
        set
        {
            cachedRating = value;
        }
    }

    public PersonalityNode()
    {
    }

    public PersonalityNode(Pawn pawn)
    {
        this.pawn = pawn;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref this.def, "def");
        Scribe_Values.Look(ref this.rawRating, "rawRating", -1f, false);
    }

    public override int GetHashCode()
    {
        return this.pawn.GetHashCode() + GenText.StableStringHash(this.def.defName);
    }
}
