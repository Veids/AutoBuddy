using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace AutoBuddy.Utilities
{
    //Ported from Screeder (https://github.com/Screeder)
    
    internal class LanePower
    {
        private bool active;

        private readonly Dictionary<string, double> minionPower = new Dictionary<string, double> {
            { "SRU_ChaosMinionMelee", 0.5 },
            { "SRU_ChaosMinionRanged", 0.35 },
            { "SRU_ChaosMinionSiege", 1.5 },
            { "SRU_ChaosMinionSuper", 4.0 },
            { "SRU_OrderMinionMelee", 0.5 },
            { "SRU_OrderMinionRanged", 0.35 },
            { "SRU_OrderMinionSiege", 1.5 },
            { "SRU_OrderMinionSuper", 4.0 },
            };

        private readonly Dictionary<string, Lane> turretBonus = new Dictionary<string, Lane> {
            { "Turret_T2_R_01_A", Lane.Bot },
            { "Turret_T2_R_02_A", Lane.Bot },
            { "Turret_T2_R_03_A", Lane.Bot },
            { "Turret_T2_C_03_A", Lane.Mid },
            { "Turret_T2_C_04_A", Lane.Mid },
            { "Turret_T2_C_05_A", Lane.Mid },
            { "Turret_T2_L_01_A", Lane.Top },
            { "Turret_T2_L_02_A", Lane.Top },
            { "Turret_T2_L_03_A", Lane.Top },
            { "Turret_T1_C_07_A", Lane.Bot },
            { "Turret_T1_R_02_A", Lane.Bot },
            { "Turret_T1_R_03_A", Lane.Bot },
            { "Turret_T1_C_03_A", Lane.Mid },
            { "Turret_T1_C_04_A", Lane.Mid },
            { "Turret_T1_C_05_A", Lane.Mid },
            { "Turret_T1_C_06_A", Lane.Top },
            { "Turret_T1_L_02_A", Lane.Top },
            { "Turret_T1_L_03_A", Lane.Top },
            };
        
        private Dictionary<Obj_AI_Minion, MinionStruct> minions = new Dictionary<Obj_AI_Minion, MinionStruct>();
        private Dictionary<Obj_AI_Turret, Lane> turrets = new Dictionary<Obj_AI_Turret, Lane>();

        private List<AIHeroClient> heroes = new List<AIHeroClient>();

        private struct MinionStruct
        { 
            public Lane Lane;

            public double Power;
        }

        private struct PowerDiff
        {
            public enum Orientation
            {
                None,
                Ally,
                Enemy
            }

            public double Ally;

            public double Enemy;

            public Orientation Direction;
        }

        public LanePower()
        {
            active = true;

            if (Game.MapId != GameMapId.SummonersRift)
                return;

            foreach (AIHeroClient hero in ObjectManager.Get<AIHeroClient>())
            {
                heroes.Add(hero);
            }

            foreach (Obj_AI_Minion minion in ObjectManager.Get<Obj_AI_Minion>())
            {
                Obj_AI_Minion_OnCreate(minion, null);
            }

            foreach (Obj_AI_Turret turret in ObjectManager.Get<Obj_AI_Turret>())
            {
                if (turretBonus.ContainsKey(turret.Name))
                {
                    turrets.Add(turret, turretBonus[turret.Name]);
                }
            }

            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += Obj_AI_Minion_OnCreate;
            GameObject.OnDelete += Obj_AI_Minion_OnDelete;

            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;
        }

        ~LanePower()
        {
            active = false;
            Game.OnUpdate -= Game_OnUpdate;
            GameObject.OnCreate -= Obj_AI_Minion_OnCreate;
            GameObject.OnDelete -= Obj_AI_Minion_OnDelete;

            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw -= Drawing_OnDraw;
        }

        public bool IsActive()
        {
            return active;
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions.ToArray())
                if (!minion.Key.IsValid)
                    minions.Remove(minion.Key);
        }

        private void Obj_AI_Minion_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;

            Obj_AI_Minion minion = sender as Obj_AI_Minion;
            if (minion != null)
                if (minionPower.ContainsKey(minion.BaseSkinName))
                    minions.Add(minion, new MinionStruct() { Lane = minion.GetLane(), Power = minionPower[minion.BaseSkinName] });
        }

        private void Obj_AI_Minion_OnDelete(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;

            foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions.ToArray())
                if (minion.Key.IsValid && minion.Key.NetworkId == sender.NetworkId)
                    minions.Remove(minion.Key);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            PowerDiff toplane = GetPowerDifference(Lane.Top);
            PowerDiff midlane = GetPowerDifference(Lane.Mid);
            PowerDiff botlane = GetPowerDifference(Lane.Bot);

            double top = toplane.Ally * 100 / (toplane.Ally + toplane.Enemy);
            double mid = midlane.Ally * 100 / (midlane.Ally + midlane.Enemy);
            double bot = botlane.Ally * 100 / (botlane.Ally + botlane.Enemy);

            Drawing.DrawText(250, 100, System.Drawing.Color.Gold, "Top: " + (int)top + " mid: " + (int)mid + " bot: " + (int)bot);          
        }

        private PowerDiff GetPowerDifference(Lane lane)
        {
            PowerDiff powerDiff = new PowerDiff();
            powerDiff.Direction = PowerDiff.Orientation.None;

            int allyCount = 0;
            int enemyCount = 0;
            
            foreach (KeyValuePair<Obj_AI_Minion, MinionStruct> minion in minions)
            {
                if (minion.Key != null && minion.Key.IsValid && minion.Value.Lane == lane)
                {
                    if (ObjectManager.Player.Team == minion.Key.Team)
                    {  
                        allyCount++;
                        powerDiff.Ally += minion.Value.Power + (minion.Value.Power * GetTurretBonus(minion));
                    }
                    else
                    {
                        enemyCount++;
                        powerDiff.Enemy += minion.Value.Power + (minion.Value.Power * GetTurretBonus(minion));
                    }
                }
            }

            if (allyCount > 0 && enemyCount > 0)
            {
                int teamDiff = allyCount - enemyCount;
                if (teamDiff > 4)
                {
                    powerDiff.Ally += 2;
                    powerDiff.Direction = PowerDiff.Orientation.Ally;
                }
                else if (teamDiff < -4)
                {
                    powerDiff.Enemy += 2;
                    powerDiff.Direction = PowerDiff.Orientation.Enemy;
                }
            }
            else if (enemyCount == 0 && allyCount > 7)
            {
                powerDiff.Ally += 2;
                powerDiff.Direction = PowerDiff.Orientation.Ally;
            }
            else if (allyCount == 0 && enemyCount > 7)
            {
                powerDiff.Enemy += 2;
                powerDiff.Direction = PowerDiff.Orientation.Enemy;
            }

            return powerDiff;
        }

        private double GetHeroLevelDiff(GameObjectTeam team)
        {
            int maxAllies = 0;
            int maxEnemies = 0;
            int sumLevelAllies = 0;
            int sumLevelEnemies = 0;

            foreach (AIHeroClient hero in heroes)
            {
                if (team == hero.Team)
                {
                    maxAllies++;
                    sumLevelAllies += hero.Level;
                }
                else
                {
                    maxEnemies++;
                    sumLevelEnemies += hero.Level;
                }
            }

            double leveldiff = (maxAllies > 0 ? sumLevelAllies / maxAllies : 0) - (maxEnemies > 0 ? sumLevelEnemies / maxEnemies : 0);
            return Math.Min(Math.Max(leveldiff, -3), 3);
        }

        private double GetTurretDiff(Lane lane, GameObjectTeam team)
        {
            int turretDiff = 0;

            foreach (KeyValuePair<Obj_AI_Turret, Lane> turret in turrets)
            {
                if (turret.Key.IsValid && lane == turret.Value)
                {
                    if (team == turret.Key.Team)
                    {
                        turretDiff++;
                    }
                    else
                    {
                        turretDiff--;
                    }
                }
            }

            return turretDiff;
        }

        private double GetTurretBonus(KeyValuePair<Obj_AI_Minion, MinionStruct> minion)
        {
            double bonus = 0.0;
            double level = GetHeroLevelDiff(minion.Key.Team);
            if (level == 0) return bonus;
            if (level > 0)
            {
                Lane lane = minion.Value.Lane;
                if (lane == Lane.Unknown) return bonus;

                double turretDiff = GetTurretDiff(lane, minion.Key.Team);
                bonus = 0.05 + (0.05 * Math.Max(0, turretDiff));
            }
            else if (minion.Key.IsTargetable != false)
            {
//                     Lane lane = minion.Value.Lane;
//                     if (lane != Lane.Unknown)
//                     {
//                         double turretDiff = GetTurretDiff(lane, minion.Key.Target);
//                         bonus = -0.05 - (0.05 * Math.Max(0, turretDiff));
//                     }
            }
            return bonus;
        }
    }
}
