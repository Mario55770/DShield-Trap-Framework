using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace DShield_Framework
{
    class TrapDef : DefModExtension
    {
        //targeting behaviors. When not told elsewise, assume vanilla
        public bool targetLegs = false;
        //damage adjusters. Defaults to stab to simulate spike traps.
        public DamageDef damageType;
            //=DamageDefOf.Stab;
        public float applyCount = 5f;
        //hediff section
        public HediffDef appliedHediff;
        public float hediffMinChance = 0f;
        public float hediffMaxChance = 1f;
        public bool applyHediffToWholeBody = true;


    }
}