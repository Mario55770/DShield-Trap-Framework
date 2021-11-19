using Verse;
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

        //Method that would handle chance of destroyed trap
        /**	
		public override void Spring(Pawn p)
		{
			bool spawned = base.Spawned;
			Map map = base.Map;
			SpringSub(p);
			if (def.building.trapDestroyOnSpring)
			{
				if (!base.Destroyed)
				{
					Destroy();
				}
				if (spawned)
				{
					CheckAutoRebuild(map);
				}
			}
		}**/
        protected override void SpringSub(Pawn p)
        {
            //if fuel is not empty or null, lets class run.
            if (this.GetComp<CompRefuelable>() == null || this.GetComp<CompRefuelable>().Fuel > 0)
            {
                //handles fuel.
                if (this.GetComp<CompRefuelable>() != null)
                {

                    this.GetComp<CompRefuelable>().ConsumeFuel(this.GetComp<CompRefuelable>().Props.FuelMultiplierCurrentDifficulty);
                }
                float DamageCount = def.GetModExtension<TrapDef>().applyCount;
                DamageDef damageType = def.GetModExtension<TrapDef>().damageType;
                SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(base.Position, base.Map));
                if (p == null)
                {
                    return;
                }
                
                float num = this.GetStatValue(StatDefOf.TrapMeleeDamage) * DamageRandomFactorRange.RandomInRange / DamageCount;
                float armorPenetration = num * 0.015f;

                //null check, should stop this chunk from running if its not defined.
                //Will need to be moved into loop once I can apply per struck limb.
                HediffDef h = def.GetModExtension<TrapDef>().appliedHediff;
                //For some reason this needs to be outside the if statement.
                FloatRange hediffFactor=new FloatRange(def.GetModExtension<TrapDef>().hediffMinChance, def.GetModExtension<TrapDef>().hediffMaxChance); ;
                bool applyHedifftoWholebody = def.GetModExtension<TrapDef>().applyHediffToWholeBody;
                if (h != null)
                {

                    //gives a random severity of hediff between stated range. Defaults to 0 and 1 respectively, which is the default hediff range.
                    
                    if (applyHedifftoWholebody)
                    {
                        HealthUtility.AdjustSeverity(p, h, hediffFactor.RandomInRange);
                    }
                }
                for (int i = 0; (float)i < DamageCount; i++)
                {

                    DamageInfo dinfo;
                    //This commented out line returns a random element tied to pawn moving
                    //(from x in p.health.hediffSet.GetNotMissingParts() where x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) select x).RandomElement(); The code uses an iteration that uses any from moving limb core, digits, or segments
                    if (def.GetModExtension<TrapDef>().targetLegs)
                    {

                        //swap to damage info that targets moving parts
                        BodyPartRecord targetPart = ((BodyPartRecord)(from x in p.health.hediffSet.GetNotMissingParts() where x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) || x.def.tags.Contains(BodyPartTagDefOf.MovingLimbDigit) || x.def.tags.Contains(BodyPartTagDefOf.MovingLimbSegment) select x).RandomElement());
                        dinfo = new DamageInfo(damageType, num, armorPenetration, -1f, this, targetPart);
                     
                       
                    }
                    //Damage worker when not targeting legs.
                    else
                    {
                        dinfo = new DamageInfo(damageType, num, armorPenetration, -1f, this);
                    }


                    DamageWorker.DamageResult damageResult = p.TakeDamage(dinfo);
                    if (h != null && !applyHedifftoWholebody)
                    {
                        hediffApplicationComparisons(p, h, hediffFactor, damageResult.LastHitPart);
                    }
                    damageResult = p.TakeDamage(dinfo);
                    if (i == 0)
                    {
                        
                        BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(p, RulePackDefOf.DamageEvent_TrapSpike);
                        Find.BattleLog.Add(battleLogEntry_DamageTaken);
                        damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
                    }
                }
            }

        }

        private static void hediffApplicationComparisons(Pawn p, HediffDef h, FloatRange hediffFactor, BodyPartRecord targetPart)
        {
            //check if part has a hediff
            //Designed to confirm pawn is alive and part is nonmissing
            if (!p.health.hediffSet.PartIsMissing(targetPart)&&!p.health.Dead)
            {
                if (p.health.hediffSet.HasHediff(h, targetPart))
                {

                    //if so, then search for the hediff in that limb(Feels roundabout but it works)
                    foreach (Hediff findPart in p.health.hediffSet.hediffs)
                    {
                        //then confirm that, increase severity, and break(to save some minor performance
                        if (targetPart == findPart.Part&& findPart.def==h)
                        {
                           findPart.Severity += hediffFactor.RandomInRange;
                       // Log.Message(findPart.ToString()); 
                        break;

                        }
                    }
                }    
                else //if there is no hediff there yet, then apply one.
                {
                    h.initialSeverity = hediffFactor.RandomInRange;
                    p.health.AddHediff(h, targetPart);
                //Log.Message("Hediff applied newly");
            }

            }
        }
    }
}
