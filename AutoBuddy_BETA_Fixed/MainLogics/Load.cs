using System;
using System.Collections.Generic;
using System.Linq;
using AutoBuddy.Humanizers;
using AutoBuddy.Utilities;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace AutoBuddy.MainLogics
{
    internal class Load
    {
        private const float waitTime = 40;
        private readonly LogicSelector currentLogic;
        private readonly float startTime;
        private string status = " ";
        public bool waiting;
        private float lastSliderSwitch;
        private bool waitingSlider;
        private bool hf;
        private bool customlane;

        public Load(LogicSelector c)
        {
            currentLogic = c;
            startTime = Game.Time + waitTime + RandGen.r.NextFloat(-10, 20);
            if (MainMenu.GetMenu("AB").Get<CheckBox>("debuginfo").CurrentValue)
                Drawing.OnDraw += Drawing_OnDraw;

            Chat.OnMessage += Chat_OnMessage;
            MainMenu.GetMenu("AB").Get<CheckBox>("reselectlane").OnValueChange += Checkbox_OnValueChange;
            MainMenu.GetMenu("AB").Get<Slider>("lane").OnValueChange += Slider_OnValueChange;
        }


        private void Slider_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            lastSliderSwitch = Game.Time + 1;
            handleSlider();
        }

        private void handleSlider(bool x = true)
        {
            if (waitingSlider && x) return;
            if (lastSliderSwitch > Game.Time)
            {
                waitingSlider = true;
                Core.DelayAction(() => handleSlider(false), (int)((lastSliderSwitch - Game.Time) * 1000) + 50);
            }
            else
                ReselectLane();
        }

        private void Checkbox_OnValueChange(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            ReselectLane();
        }

        private void ReselectLane()
        {
            SetLane();
            waitingSlider = false;
            Chat.Print("Reselecting lane");
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(250, 70, Color.Gold, "Lane selector status: " + status);
        }

        public void Activate()
        {
        }

        public void SetLane()
        {
            if (MainMenu.GetMenu("AB").Get<Slider>("lane").CurrentValue != 1)
            {
                switch (MainMenu.GetMenu("AB").Get<Slider>("lane").CurrentValue)
                {
                    case 2:
                        ChangeLane(Lane.Top);
                        break;
                    case 3:
                        ChangeLane(Lane.Mid);
                        break;
                    case 4:
                        ChangeLane(Lane.Bot);
                        break;
                }
            }
            else if (ObjectManager.Get<Obj_AI_Turret>().Count() == 24)
            {
                waiting = true;
                Vector3 p = GetAllyTurret("R_03_A").Position;
                Core.DelayAction(() => SafeFunctions.Ping(PingCategory.OnMyWay, p.Randomized()),RandGen.r.Next(1500, 3000));
                AutoWalker.SetMode(Orbwalker.ActiveModes.Combo);
                AutoWalker.WalkTo(p.Extend(AutoWalker.MyNexus, 200 + RandGen.r.NextFloat(0, 100)).To3DWorld().Randomized());

                EarlySelectLane();
            }
            else
                SelectMostPushedLane();
        }

        private void EarlySelectLane()
        {
            status = "looking for free lane " + (int)(startTime - Game.Time);
            if (Game.Time > startTime || GetChampLanes().All(cl => cl.lane != Lane.Unknown))
            {
                waiting = false;
                Core.DelayAction(SelectLane, RandGen.r.Next(500, 5000));
            }
            else
                Core.DelayAction(EarlySelectLane, RandGen.r.Next(500, 5000));
        }

        private void Chat_OnMessage(AIHeroClient sender, ChatMessageEventArgs args)
        {
            if (!hf && (args.Message.Contains("have fun") || args.Message.Contains("hf")))
            {
                Core.DelayAction(() => Chat.Say("gl hf"), RandGen.r.Next(2000, 4000));
                hf = true;
            }

            if (!customlane || !args.Message.Contains(AutoWalker.p.Name, StringComparison.CurrentCultureIgnoreCase)) return;

            if (args.Message.Contains("go top", StringComparison.CurrentCultureIgnoreCase))
            {
                Core.DelayAction(() => ChangeLane(Lane.Top), RandGen.r.Next(1500, 3000));
                customlane = true;
            }

            if (args.Message.Contains("go mid", StringComparison.CurrentCultureIgnoreCase))
            {
                Core.DelayAction(() => ChangeLane(Lane.Mid), RandGen.r.Next(1500, 3000));
                customlane = true;
            }

            if (args.Message.Contains("go bot", StringComparison.CurrentCultureIgnoreCase))
            {
                Core.DelayAction(() => ChangeLane(Lane.Bot), RandGen.r.Next(1500, 3000));
                customlane = true;
            }
        }

        private void SelectMostPushedLane()
        {
            status = "selected most pushed lane";
            Obj_HQ nMyNexus = ObjectManager.Get<Obj_HQ>().First(hq => hq.IsEnemy);

            Obj_AI_Minion andrzej =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(min => min.Name.Contains("Minion") && min.IsAlly && min.Health > 0)
                    .OrderBy(min => min.Distance(nMyNexus))
                    .First();

            Obj_AI_Base ally =
                ObjectManager.Get<Obj_AI_Turret>()
                    .Where(tur => tur.IsAlly && tur.Health > 0 && tur.GetLane() == andrzej.GetLane())
                    .OrderBy(tur => tur.Distance(andrzej))
                    .FirstOrDefault();
            if (ally == null)
            {
                ally =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .Where(tur => tur.Health > 0 && tur.IsAlly
                                      && tur.GetLane() == Lane.HQ)
                        .OrderBy(tur => tur.Distance(andrzej))
                        .FirstOrDefault();
            }
            if (ally == null)
            {
                ally =
                    ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsAlly && tur.GetLane() == Lane.Spawn);
            }

            Obj_AI_Base enemy =
                ObjectManager.Get<Obj_AI_Turret>()
                    .Where(tur => tur.IsEnemy && tur.Health > 0 && tur.GetLane() == andrzej.GetLane())
                    .OrderBy(tur => tur.Distance(andrzej))
                    .FirstOrDefault();
            if (enemy == null)
            {
                enemy =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .Where(tur => tur.Health > 0 && tur.IsEnemy
                                      && tur.GetLane() == Lane.HQ)
                        .OrderBy(tur => tur.Distance(andrzej))
                        .FirstOrDefault();
            }
            if (enemy == null)
            {
                enemy =
                    ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsEnemy && tur.GetLane() == Lane.Spawn);
            }

            currentLogic.pushLogic.Reset(ally, enemy, andrzej.GetLane());
        }

        public void ChangeLane(Lane l)
        {
            status = "selected " + l;
            Obj_AI_Turret ally = null, enemy = null;

            if (l == Lane.Top)
            {
                ally = (GetAllyTurret("L_03_A") ?? GetAllyTurret("L_02_A")) ?? GetAllyTurret("L_01_A");
                enemy = (GetEnemyTurret("L_03_A") ??GetEnemyTurret("L_02_A")) ?? GetEnemyTurret("L_01_A");
            }
            else if (l == Lane.Bot)
            {
                ally = (GetAllyTurret("R_03_A") ?? GetAllyTurret("R_02_A")) ?? GetAllyTurret("R_01_A");
                enemy = (GetEnemyTurret("R_03_A") ?? GetEnemyTurret("R_02_A")) ?? GetEnemyTurret("R_01_A");
            }
            else if (l == Lane.Mid)
            {
                ally = (GetAllyTurret("C_05_A") ?? GetAllyTurret("C_04_A")) ?? GetAllyTurret("C_03_A");
                enemy = (GetEnemyTurret("R_05_A") ?? GetEnemyTurret("R_04_A")) ?? GetEnemyTurret("C_03_A");
            }

            if (ally == null)
                ally = GetAllyHQTurret() ?? GetAllySpawnTurret();
            
            if (enemy == null)
                enemy = GetEnemyHQTurret() ?? GetEnemySpawnTurret();

            currentLogic.pushLogic.Reset(ally, enemy, l);
        }

        private void SelectLane()
        {
            List<ChampLane> list = GetChampLanes();

            if (list.Count(cl => cl.lane == Lane.Bot) < 2)
                currentLogic.pushLogic.Reset( GetAllyTurret("R_03_A"), GetEnemyTurret("R_03_A"), Lane.Bot);
            else if (list.All(cl => cl.lane != Lane.Mid))
                currentLogic.pushLogic.Reset( GetAllyTurret("C_05_A"), GetEnemyTurret("C_05_A"), Lane.Mid);
            else if (list.Count(cl => cl.lane == Lane.Top) < 2)
                currentLogic.pushLogic.Reset( GetAllyTurret("L_03_A"), GetEnemyTurret("L_03_A"), Lane.Top);

            status = "selected free lane";
        }

        private static List<ChampLane> GetChampLanes(float maxDistance = 3000, float maxDistanceFront = 4000)
        {
            Obj_AI_Turret top1 = GetAllyTurret("L_03_A");
            Obj_AI_Turret top2 = GetAllyTurret("L_02_A");
            Obj_AI_Turret mid1 = GetAllyTurret("C_05_A");
            Obj_AI_Turret mid2 = GetAllyTurret("C_04_A");
            Obj_AI_Turret bot1 = GetAllyTurret("R_03_A");
            Obj_AI_Turret bot2 = GetAllyTurret("R_02_A");

            List<ChampLane> ret = new List<ChampLane>();

            foreach (AIHeroClient h in EntityManager.Heroes.Allies.Where(hero => hero.IsAlly && !hero.IsMe))
            {
                Lane lane = Lane.Unknown;
                if (h.Distance(top1) < maxDistanceFront || h.Distance(top2) < maxDistance) lane = Lane.Top;
                else if (h.Distance(mid1) < maxDistanceFront || h.Distance(mid2) < maxDistance) lane = Lane.Mid;
                else if (h.Distance(bot1) < maxDistanceFront || h.Distance(bot2) < maxDistance) lane = Lane.Bot;
                else if (h.FindSummonerSpellSlotFromName("SummonerSmite") != SpellSlot.Unknown) lane = Lane.Jungle;
                ret.Add(new ChampLane(h, lane));
            }
            return ret;
        }

        private static Obj_AI_Turret GetAllyTurret(string name)
        {
            return ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsAlly && tur.Name.EndsWith(name));
        }

        private static Obj_AI_Turret GetEnemyTurret(string name)
        {
            return ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsEnemy && tur.Name.EndsWith(name));
        }

        private static Obj_AI_Turret GetAllyHQTurret()
        {
            return ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsAlly && tur.GetLane() == Lane.HQ);
        }

        private static Obj_AI_Turret GetEnemyHQTurret()
        {
            return ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsEnemy && tur.GetLane() == Lane.HQ);
        }

        private static Obj_AI_Turret GetAllySpawnTurret()
        {
            return ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsAlly && tur.GetLane() == Lane.Spawn);
        }

        private static Obj_AI_Turret GetEnemySpawnTurret()
        {
            return ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tur => tur.IsEnemy && tur.GetLane() == Lane.Spawn);
        }
    }
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}