namespace SharpShooter.MyCommon
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SharpDX;

    using EnsoulSharp;
    using EnsoulSharp.SDK;

    #endregion

    public static class MyExtraManager
    {
        public static double GetComboDamage(this AIBaseClient target, bool q, bool w, bool e, bool r, bool attack)
        {
            if (target == null || target.IsDead || !target.IsValidTarget())
            {
                return 0;
            }

            if (!q && !w && !e && !r && !attack)
            {
                return 0;
            }

            if (target.HasBuff("KindredRNoDeathBuff"))
            {
                return 0;
            }

            if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3f)
            {
                return 0;
            }

            if (target.HasBuff("JudicatorIntervention"))
            {
                return 0;
            }

            if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3f)
            {
                return 0;
            }

            if (target.HasBuff("FioraW"))
            {
                return 0;
            }

            if (target.HasBuff("ShroudofDarkness"))
            {
                return 0;
            }

            if (target.HasBuff("SivirShield"))
            {
                return 0;
            }

            var damage = 0d;

            if (q && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).State == SpellState.Ready)
            {
                if (ObjectManager.Player.CharacterName == "Jayce")
                {
                    damage += MyPlugin.Jayce.GetQDamage(target);
                }
                else
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
                }
            }

            if (w && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).State == SpellState.Ready)
            {
                if (ObjectManager.Player.CharacterName == "Jayce")
                {
                    damage += MyPlugin.Jayce.GetWDamage(target);
                }
                else if (ObjectManager.Player.CharacterName == "Vayne")
                {
                    damage += MyPlugin.Vayne.GetWDamage(target);
                }
                else if (ObjectManager.Player.CharacterName == "Urgot")
                {
                    damage += MyPlugin.Urgot.GetWDamage(target);
                }
                else
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
                }
            }

            if (e && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).State == SpellState.Ready)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);

                if (ObjectManager.Player.CharacterName == "Jayce")
                {
                    damage += MyPlugin.Jayce.GetEDamage(target);
                }
                else if (ObjectManager.Player.CharacterName == "Kalista")
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) +
                              ObjectManager.Player
                                  .GetSpellDamage(target, SpellSlot.E, DamageStage.Buff);
                }
                else if (ObjectManager.Player.CharacterName == "Twitch")
                {
                    damage += MyPlugin.Twitch.GetEDMGTwitch(target);
                }
                else if (ObjectManager.Player.CharacterName == "Xayah")
                {
                    if (target.Type == GameObjectType.AIMinionClient)
                    {
                        damage += MyPlugin.Xayah.GetEDamageForMinion(target);
                    }
                    else
                    {
                        if (MyPlugin.Xayah.HitECount(target) > 0)
                        {
                            damage += MyPlugin.Xayah.GetEDMG(target, MyPlugin.Xayah.HitECount(target));
                        }
                    }
                }
            }

            if (r && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).State == SpellState.Ready)
            {
                if (ObjectManager.Player.CharacterName == "Urgot" && target.Type == GameObjectType.AIHeroClient)
                {
                    damage += MyPlugin.Urgot.GetRDamage(target as AIHeroClient, true);
                }
                else
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
                }
            }

            if (attack)
            {
                damage += ObjectManager.Player.GetAutoAttackDamage(target);
            }

            if (target.CharacterName == "Morderkaiser")
            {
                damage -= target.Mana;
            }

            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
            {
                damage = damage * 0.6f;
            }

            if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
            {
                damage -= target.Mana / 2f;
            }

            if (target.HasBuff("GarenW"))
            {
                damage = damage * 0.7f;
            }

            if (target.HasBuff("ferocioushowl"))
            {
                damage = damage * 0.7f;
            }

            return damage;
        }

        public static bool isBigMob(this AIMinionClient mob)
        {
            return mob != null &&
                   (mob.GetJungleType() == JungleType.Large || mob.GetJungleType() == JungleType.Legendary);
        }

        public static SpellSlot GetSpellSlotFromName(this AIHeroClient source, string name)
        {
            foreach (var spell in source.Spellbook.Spells.Where(spell => string.Equals(spell.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            {
                return spell.Slot;
            }

            return SpellSlot.Unknown;
        }

        public static bool IsGrass(this Vector3 Position)
        {
            var CF = NavMesh.GetCollisionFlags(Position);
            return CF.HasFlag(CollisionFlags.Grass);
        }

        public static bool HaveShiledBuff(this AIBaseClient target)
        {
            if (target == null || target.IsDead || target.Health <= 0 || !target.IsValidTarget())
            {
                return false;
            }

            if (target.HasBuff("BlackShield"))
            {
                return true;
            }

            if (target.HasBuff("bansheesveil"))
            {
                return true;
            }

            if (target.HasBuff("SivirE"))
            {
                return true;
            }

            if (target.HasBuff("NocturneShroudofDarkness"))
            {
                return true;
            }

            if (target.HasBuff("itemmagekillerveil"))
            {
                return true;
            }

            if (target.HasBuffOfType(BuffType.SpellShield))
            {
                return true;
            }

            return false;
        }

        public static int GetHitCounts(this Spell spell, IEnumerable<AIMinionClient> minionList, Vector3 endPosition)
        {
            var points = minionList.ToList();
            return points.Count(point => spell.WillHit(point, endPosition));
        }

        public static bool CanMoveMent(this AIBaseClient target)
        {
            return !(target.MoveSpeed < 50) && !target.HasBuffOfType(BuffType.Stun) &&
                   !target.HasBuffOfType(BuffType.Fear) && !target.HasBuffOfType(BuffType.Snare) &&
                   !target.HasBuffOfType(BuffType.Knockup) && !target.HasBuff("recall") &&
                   !target.HasBuffOfType(BuffType.Knockback)
                   && !target.HasBuffOfType(BuffType.Charm) && !target.HasBuffOfType(BuffType.Taunt) &&
                   !target.HasBuffOfType(BuffType.Suppression) &&
                   !target.HasBuff("zhonyasringshield") && !target.HasBuff("bardrstasis");
        }

        public static SpellDataInst GetBasicSpell(this Spell spell)
        {
            return ObjectManager.Player.Spellbook.GetSpell(spell.Slot);
        }

        public static SpellData GetSpellData(this Spell spell)
        {
            return ObjectManager.Player.Spellbook.GetSpell(spell.Slot).SData;
        }

        public static bool IsUnKillable(this AIBaseClient target)
        {
            if (target == null || target.IsDead || target.Health <= 0)
            {
                return true;
            }

            if (target.HasBuff("KindredRNoDeathBuff"))
            {
                return true;
            }

            if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3 &&
                target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            if (target.HasBuff("JudicatorIntervention"))
            {
                return true;
            }

            if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3 &&
                target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            if (target.HasBuff("VladimirSanguinePool"))
            {
                return true;
            }

            if (target.HasBuff("ShroudofDarkness"))
            {
                return true;
            }

            if (target.HasBuff("SivirShield"))
            {
                return true;
            }

            if (target.HasBuff("itemmagekillerveil"))
            {
                return true;
            }

            return target.HasBuff("FioraW");
        }

        public static double GetRealDamage(double Damage, AIBaseClient target, bool havetoler = false, float tolerDMG = 0)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += Damage + (havetoler ? tolerDMG : 0) - target.HPRegenRate;

                if (target.CharacterName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (ObjectManager.Player.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        public static double GetKalistaRealDamage(this Spell spell, AIBaseClient target, bool havetoler = false, float tolerDMG = 0, bool getrealDMG = false)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += spell.IsReady()
                    ? ObjectManager.Player.GetSpellDamage(target, spell.Slot) +
                      ObjectManager.Player
                          .GetSpellDamage(target, spell.Slot, DamageStage.Buff) //Kalista E
                    : 0d + (havetoler ? tolerDMG : 0) - target.HPRegenRate;

                if (target.CharacterName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (ObjectManager.Player.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        public static IEnumerable<Vector3> GetCirclePoints(float range)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point =
                    new Vector3(ObjectManager.Player.PreviousPosition.X + range * (float) Math.Cos(angle),
                        ObjectManager.Player.PreviousPosition.Y + range * (float) Math.Sin(angle),
                        ObjectManager.Player.PreviousPosition.Z);

                points.Add(point);
            }

            return points;
        }

        public static IEnumerable<Vector3> GetCirclePoints(Vector3 position, float range)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point =
                    new Vector3(position.X + range * (float)Math.Cos(angle),
                        position.Y + range * (float)Math.Sin(angle),
                        position.Z);

                points.Add(point);
            }

            return points;
        }

        public static IEnumerable<Vector3> GetCirclePoints(Vector2 position, float range)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point = new Vector2(position.X + range * (float)Math.Cos(angle), position.Y + range * (float)Math.Sin(angle));

                points.Add(point.ToVector3());
            }

            return points;
        }
    }
}
