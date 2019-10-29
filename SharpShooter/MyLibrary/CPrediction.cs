#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 CPrediction.cs is part of SFXChallenger.
 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SharpShooter.MyLibrary
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SharpDX;

    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.Prediction;
    using EnsoulSharp.SDK.Utility;

    #endregion

    public static class CPrediction
    {
        public static float BoundingRadiusMultiplicator { get; set; } = 0.85f;

        public static bool GetLineAoeCanHit(float spellrange, float spellwidth, AIBaseClient target, HitChance hitChance, Vector3 endRange,
            bool boundingRadius = true)
        {
            if (target == null || target.IsDashing())
            {
                return false;
            }

            var targetPosition = target.PreviousPosition.ToVector2();
            var fromPosition = ObjectManager.Player.PreviousPosition;
            var width = spellwidth + (boundingRadius ? target.BoundingRadius * BoundingRadiusMultiplicator : 0);
            var boundradius = (boundingRadius
                ? target.BoundingRadius * BoundingRadiusMultiplicator
                : target.BoundingRadius);

            var rect = new Geometry.Rectangle(fromPosition, endRange, width);
            var circ = new Geometry.Circle(targetPosition, boundradius);

            return circ.Points.Select(point => rect.IsInside(point)).FirstOrDefault();
        }

        public static Result GetCircleAoePrediction(this Spell spell, AIHeroClient target, HitChance hitChance,
            bool boundingRadius = true, bool extended = true, Vector3? sourcePosition = null)
        {
            try
            {
                if (spell == null || target == null)
                {
                    return new Result(Vector3.Zero, new List<AIHeroClient>());
                }
                var fromPosition = sourcePosition ?? ObjectManager.Player.PreviousPosition;
                var hits = new List<AIHeroClient>();
                var center = Vector3.Zero;
                var radius = float.MaxValue;
                var range = spell.Range + (extended ? spell.Width * 0.85f : 0) +
                            (boundingRadius ? target.BoundingRadius * BoundingRadiusMultiplicator : 0);
                var positions = (from t in GameObjects.EnemyHeroes
                                 where t.IsValidTarget(range * 1.5f, true, fromPosition)
                                 let prediction = spell.GetPrediction(t)
                                 where prediction.Hitchance >= hitChance
                                 select new Position(t, prediction.UnitPosition)).ToList();
                var spellWidth = spell.Width;
                if (positions.Any())
                {
                    var mainTarget = positions.FirstOrDefault(p => p.Hero.NetworkId == target.NetworkId);
                    var possibilities =
                        ProduceEnumeration(
                            positions.Where(
                                p => p.UnitPosition.Distance(mainTarget.UnitPosition) <= spell.Width * 0.85f).ToList())
                            .Where(p => p.Count > 0 && p.Any(t => t.Hero.NetworkId == mainTarget.Hero.NetworkId))
                            .ToList();
                    foreach (var possibility in possibilities)
                    {
                        var mec = MEC.GetMec(possibility.Select(p => p.UnitPosition.ToVector2()).ToList());
                        var distance = spell.From.Distance(mec.Center.ToVector3());
                        if (mec.Radius < spellWidth && distance < range)
                        {
                            var lHits = new List<AIHeroClient>();
                            var circle =
                                new Geometry.Circle(
                                    spell.From.Extend(
                                        mec.Center.ToVector3(), spell.Range > distance ? distance : spell.Range), spell.Width);

                            if (boundingRadius)
                            {
                                lHits.AddRange(
                                    from position in positions
                                    where
                                        new Geometry.Circle(
                                            position.UnitPosition,
                                            position.Hero.BoundingRadius * BoundingRadiusMultiplicator).Points.Any(
                                                p => circle.IsInside(p))
                                    select position.Hero);
                            }
                            else
                            {
                                lHits.AddRange(
                                    from position in positions
                                    where circle.IsInside(position.UnitPosition)
                                    select position.Hero);
                            }

                            if ((lHits.Count > hits.Count || lHits.Count == hits.Count && mec.Radius < radius ||
                                 lHits.Count == hits.Count &&
                                 spell.From.Distance(circle.Center.ToVector3()) < spell.From.Distance(center)) &&
                                lHits.Any(p => p.NetworkId == target.NetworkId))
                            {
                                center = ToVector32(circle.Center);
                                radius = mec.Radius;
                                hits.Clear();
                                hits.AddRange(lHits);
                            }
                        }
                    }
                    if (!center.Equals(Vector3.Zero))
                    {
                        return new Result(center, hits);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new Result(Vector3.Zero, new List<AIHeroClient>());
        }

        public static Result GetLineAoePrediction(this Spell spell, AIHeroClient target, HitChance hitChance,
            bool boundingRadius = true, bool maxRange = true, Vector3? sourcePosition = null)
        {
            try
            {
                if (spell == null || target == null)
                {
                    return new Result(Vector3.Zero, new List<AIHeroClient>());
                }
                var fromPosition = sourcePosition ?? ObjectManager.Player.PreviousPosition;
                var range = (spell.IsChargedSpell && maxRange ? spell.ChargedMaxRange : spell.Range) +
                            spell.Width * 0.9f +
                            (boundingRadius ? target.BoundingRadius * BoundingRadiusMultiplicator : 0);
                var positions = (from t in GameObjects.EnemyHeroes
                                 where t.IsValidTarget(range, true, fromPosition)
                                 let prediction = spell.GetPrediction(t)
                                 where prediction.Hitchance >= hitChance
                                 select new Position(t, prediction.UnitPosition)).ToList();
                if (positions.Any())
                {
                    var hits = new List<AIHeroClient>();
                    var pred = spell.GetPrediction(target);
                    if (pred.Hitchance >= hitChance)
                    {
                        hits.Add(target);
                        var rect = new Geometry.Rectangle(
                            spell.From, spell.From.Extend(pred.CastPosition, range), spell.Width);
                        if (boundingRadius)
                        {
                            hits.AddRange(
                                from point in positions.Where(p => p.Hero.NetworkId != target.NetworkId)
                                let circle =
                                    new Geometry.Circle(
                                        point.UnitPosition, point.Hero.BoundingRadius * BoundingRadiusMultiplicator)
                                where circle.Points.Any(p => rect.IsInside(p))
                                select point.Hero);
                        }
                        else
                        {
                            hits.AddRange(
                                from position in positions
                                where rect.IsInside(position.UnitPosition)
                                select position.Hero);
                        }
                        return new Result(pred.CastPosition, hits);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new Result(Vector3.Zero, new List<AIHeroClient>());
        }

        public static Vector3 ToVector32(Vector2 v)
        {
            return new Vector3(v.X, v.Y, NavMesh.GetHeightForPosition(v.X, v.Y));
        }

        private static IEnumerable<int> ConstructSetFromBits(int i)
        {
            for (var n = 0; i != 0; i /= 2, n++)
            {
                if ((i & 1) != 0)
                {
                    yield return n;
                }
            }
        }

        private static IEnumerable<List<T>> ProduceEnumeration<T>(List<T> list)
        {
            for (var i = 0; i < 1 << list.Count; i++)
            {
                yield return ConstructSetFromBits(i).Select(n => list[n]).ToList();
            }
        }

        public struct Position
        {
            public readonly AIHeroClient Hero;
            public readonly AIBaseClient Base;
            public readonly Vector3 UnitPosition;

            public Position(AIHeroClient hero, Vector3 unitPosition)
            {
                Hero = hero;
                Base = null;
                UnitPosition = unitPosition;
            }

            public Position(AIBaseClient unit, Vector3 unitPosition)
            {
                Base = unit;
                Hero = null;
                UnitPosition = unitPosition;
            }
        }

        public struct BasePosition
        {
            public readonly AIBaseClient Unit;
            public readonly Vector3 UnitPosition;

            public BasePosition(AIBaseClient unit, Vector3 unitPosition)
            {
                Unit = unit;
                UnitPosition = unitPosition;
            }
        }

        public struct Result
        {
            public readonly Vector3 CastPosition;
            public readonly List<AIHeroClient> Hits;
            public readonly int TotalHits;

            public Result(Vector3 castPosition, List<AIHeroClient> hits)
            {
                CastPosition = castPosition;
                Hits = hits;
                TotalHits = hits.Count;
            }
        }
    }
}