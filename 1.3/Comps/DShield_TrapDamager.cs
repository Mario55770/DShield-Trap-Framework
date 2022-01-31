﻿using Verse;
using Verse.Sound;
using RimWorld;
using System.Linq;

namespace DShield_Framework
{
    public class DShield_TrapDamager : Building_Trap
    {

        private static readonly FloatRange DamageRandomFactorRange = new FloatRange(0.8f, 1.2f);





        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                SoundDefOf.TrapArm.PlayOneShot(new TargetInfo(base.Position, map));
            }
        }


        protected override void SpringSub(Pawn pawn)
        {
            //if fuel is not empty or null, lets class run.
            CompRefuelable compRefuelable = this.GetComp<CompRefuelable>();
            if (pawn == null || !(compRefuelable == null || compRefuelable.Fuel > 0))
            {
                return;
            }

            //handles fuel.
            if (compRefuelable != null)
            {

                compRefuelable.ConsumeFuel(compRefuelable.Props.FuelMultiplierCurrentDifficulty);
            }
            float DamageCount = def.GetModExtension<TrapDef>().applyCount;
            DamageDef damageType = def.GetModExtension<TrapDef>().damageType;
            SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(base.Position, base.Map));


            float num = this.GetStatValue(StatDefOf.TrapMeleeDamage) * DamageRandomFactorRange.RandomInRange / DamageCount;
            float armorPenetration = num * 0.015f;

            HediffDef h = def.GetModExtension<TrapDef>().appliedHediff;
            //For some reason this needs to be outside the if statement.
            FloatRange hediffFactor = new FloatRange(def.GetModExtension<TrapDef>().hediffMinChance, def.GetModExtension<TrapDef>().hediffMaxChance); 
            bool applyHedifftoWholebody = def.GetModExtension<TrapDef>().applyHediffToWholeBody;
            //Boolean to prevent a null and boolean check every iteration of a loop. Instead it only checks a single boolean. Should be slightly faster.
            bool hediffExistsAndNotAppliedToWholeBody = false;
            //null check, should stop this chunk from running if its not defined.
            //Applies the hediff assuming its applied to whole body instead of on a hit by hit basis(like anesthetic should be)
            if (h != null && applyHedifftoWholebody)
            {
                //gives a random severity of hediff between stated range. Defaults to 0 and 1 respectively, which is the default hediff range.
                HealthUtility.AdjustSeverity(pawn, h, hediffFactor.RandomInRange);
            }
            //checks to boolean only if this doesnt run and sets it true here so it doesn't have to be repeadely checked in the loop
            else
            {
                if (h != null && !applyHedifftoWholebody)
                {
                    hediffExistsAndNotAppliedToWholeBody = true;
                }
            }
            //gets variable outside the for loop instead of reading from xml DamageCount times
            bool targetLegs = def.GetModExtension<TrapDef>().targetLegs;
            for (int i = 0; (float)i < DamageCount; i++)
            {

                DamageInfo dinfo;

                if (targetLegs)
                {

                    //swap to damage info that targets moving parts
                    //gets random part from moving cores , limbs, and segments.
                    BodyPartRecord targetPart = ((BodyPartRecord)(from x in pawn.health.hediffSet.GetNotMissingParts() where x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) || x.def.tags.Contains(BodyPartTagDefOf.MovingLimbDigit) || x.def.tags.Contains(BodyPartTagDefOf.MovingLimbSegment) select x).RandomElement());
                    dinfo = new DamageInfo(damageType, num, armorPenetration, -1f, this, targetPart);


                }
                //Damage worker when not targeting legs.
                else
                {
                    dinfo = new DamageInfo(damageType, num, armorPenetration, -1f, this);
                }


                DamageWorker.DamageResult damageResult = pawn.TakeDamage(dinfo);

                //if hediff is on a limb by limb basis and exists, apply it.
                if (hediffExistsAndNotAppliedToWholeBody)
                {
                    hediffApplicationComparisons(pawn, h, hediffFactor, damageResult.LastHitPart);
                }
                //apply damage. Then write to log on the last entry. Copy directly from the original class.
                damageResult = pawn.TakeDamage(dinfo);
                if (i == 0)
                {

                    BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_TrapSpike);
                    Find.BattleLog.Add(battleLogEntry_DamageTaken);
                    damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
                }
            }
        }




        private static void hediffApplicationComparisons(Pawn p, HediffDef h, FloatRange hediffFactor, BodyPartRecord targetPart)
        {
            if (p.health.Dead || p.health.hediffSet.PartIsMissing(targetPart)) //If pawn dead or part missing..
                return; //Abort.
            bool found = false;
            foreach (Hediff hediff in p.health.hediffSet.hediffs)
            {
                if (hediff.def != h || hediff.Part != targetPart)
                    continue;
                found = true;
                hediff.Severity += hediffFactor.RandomInRange;
            }
            if (!found)
            {
                h.initialSeverity = hediffFactor.RandomInRange;
                p.health.AddHediff(h, targetPart);
            }
        }
    }
}
