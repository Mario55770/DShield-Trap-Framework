using Verse;
using Verse.Sound;
using RimWorld;
using System.Linq;

namespace DShield_Framework
{
	public class DShield_TrapDamager : Building_Trap
	{
		
		private static readonly FloatRange DamageRandomFactorRange = new FloatRange(0.8f, 1.2f);

		private static readonly float DamageCount = 5f;

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
			DamageDef damageType = def.GetModExtension<TrapDef>().damageType;
			SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(base.Position, base.Map));
			if (p == null)
			{
				return;
			}
			float num = this.GetStatValue(StatDefOf.TrapMeleeDamage) * DamageRandomFactorRange.RandomInRange / DamageCount;
			float armorPenetration = num * 0.015f;
			for (int i = 0; (float)i < DamageCount; i++)	
			{

				DamageInfo dinfo;
				//This commented out line returns a random element tied to pawn moving
				//(from x in p.health.hediffSet.GetNotMissingParts() where x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) select x).RandomElement(); The code uses an iteration that uses any from moving limb core, digits, or segments
				if (def.GetModExtension<TrapDef>().targetLegs)
                {
					
					//swap to damage info that targets moving parts
					dinfo = new DamageInfo(damageType, num, armorPenetration, -1f, this, (from x in p.health.hediffSet.GetNotMissingParts() where x.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) || x.def.tags.Contains(BodyPartTagDefOf.MovingLimbDigit) || x.def.tags.Contains(BodyPartTagDefOf.MovingLimbSegment) select x).RandomElement());
				}else
                {
					dinfo = new DamageInfo(damageType, num, armorPenetration, -1f, this);
				}
				
				DamageWorker.DamageResult damageResult = p.TakeDamage(dinfo);
				if (i == 0)
				{
					BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(p, RulePackDefOf.DamageEvent_TrapSpike);
					Find.BattleLog.Add(battleLogEntry_DamageTaken);
					damageResult.AssociateWithLog(battleLogEntry_DamageTaken);
				}
			}
		}
	}
}
