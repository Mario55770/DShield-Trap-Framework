using Verse;
using Verse.Sound;
using RimWorld;
using System.Linq;

namespace DShield_Framework
{
    public class DShield_TrapDamager : Building_Trap
    {

        private static readonly FloatRange DamageRandomFactorRange = new FloatRange(0.8f, 1.2f);
        private static HediffDef appliedHediff;
        private static float DamageCount;
        private static DamageDef damageType;
        private static FloatRange hediffFactor;
        private static bool applyHedifftoWholebody;
        private static TrapDef trapDef;
        private static bool targetLegs;
        private static CompRefuelable compRefuelable;
        private static bool hasBeenCached = false;
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
            //Ends if pawn is null. 
            if (pawn == null)
            {
                return;
            }
            //caches stuff. Should make this whole system run slightly faster for traps that are run multiple times
            //improvements are likely negligible to loses in single use per map load.
            //Only runs efforts to cache if it runs as traps may not always be used and so it is pointless to cache it if its not used.
            if (!hasBeenCached)
            {
                trapDef = def.GetModExtension<TrapDef>();
                DamageCount = trapDef.applyCount;
                damageType = trapDef.damageType;
                appliedHediff = trapDef.appliedHediff;
                //if appliedHediff Is null I can ignore all this stuff.
                if (appliedHediff != null)
                {
                    hediffFactor = new FloatRange(trapDef.hediffMinChance, trapDef.hediffMaxChance);
                    applyHedifftoWholebody = trapDef.applyHediffToWholeBody;

                }
                targetLegs = trapDef.targetLegs;
                compRefuelable = this.GetComp<CompRefuelable>();
                hasBeenCached = true;
            }
           

            //Checks if fuel is not null and not zero. If it is then end. 
            if (compRefuelable ==null || compRefuelable.Fuel > 0)
            {
                compRefuelable.ConsumeFuel(compRefuelable.Props.FuelMultiplierCurrentDifficulty);
            }
            
                //if it didn't end, double check if fuel needs to be consumed.
            {
                
            }

            SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(base.Position, base.Map));

            //this changes on each run ergo it has been kept here. 
            float num = this.GetStatValue(StatDefOf.TrapMeleeDamage) * DamageRandomFactorRange.RandomInRange / DamageCount;
            float armorPenetration = num * 0.015f;


            //Applies hediff to whole body assuming prior tests are passed(in the event hediff is not null and it should be applied to whole body)
            if (appliedHediff != null && applyHedifftoWholebody)
            {
                //gives a random severity of hediff between stated range. Defaults to 0 and 1 respectively, which is the default hediff range.
                HealthUtility.AdjustSeverity(pawn, appliedHediff, hediffFactor.RandomInRange);
            }

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
                if (applyHedifftoWholebody)
                {
                    hediffApplicationComparisons(pawn, appliedHediff, hediffFactor, damageResult.LastHitPart);
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


