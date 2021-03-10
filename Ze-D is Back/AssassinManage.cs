using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

namespace zedisback
{
    internal class AssassinManager
    {
        public AssassinManager()
        {
            Load();
        }

        public static Menu assassin, draw, list;

        private static void Load()
        {

            assassin = Program.TargetSelectorMenu;
            assassin.Add(new MenuBool("AssassinActive", "Active", true));
            assassin.Add(new MenuList("AssassinSelectOption", "Set: ", new string[] { "Single Select", "Multi Select" }));
            assassin.Add(new MenuBool("AssassinSetClick", "Add/Remove with Right-Click", true));
            assassin.Add(new MenuKeyBind("AssassinReset", "Reset List", Keys.O, KeyBindType.Press));

            draw = new Menu("Draw:", "Draw");
            draw.Add(new MenuColor("DrawSearch", "Search Range", SharpDX.Color.Gray));
            draw.Add(new MenuColor("DrawActive", "Active Enemy", SharpDX.Color.GreenYellow));
            draw.Add(new MenuColor("DrawNearest", "Nearest Enemy", SharpDX.Color.DarkSeaGreen));
            assassin.Add(draw);

            list = new Menu("AssassinMode", "Assassin List:");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                list.Add(new MenuBool("Assassin" + enemy.CharacterName, enemy.CharacterName, (int)TargetSelector.GetPriority(enemy) > 3));
            }
            assassin.Add(list);
            assassin.Add(new MenuSlider("AssassinSearchRange", "Search Range", 1000, 500, 2000));

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        static void ClearAssassinList()
        {
            foreach (
                var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
            {

                list.GetValue<MenuBool>("Assassin" + enemy.CharacterName).SetValue(false);

            }
        }
        private static void OnUpdate(EventArgs args)
        {
        }

        private static void Game_OnWndProc(GameWndEventArgs args)
        {
            if (assassin.GetValue<MenuKeyBind>("AssassinReset").Active && args.Msg == 257)
            {
                ClearAssassinList();
                Game.Print(
                    "<font color='#FFFFFF'>Reset Assassin List is Complete! Click on the enemy for Add/Remove.</font>");
            }

            if (args.Msg != 0x201)
            {
                return;
            }

            if (assassin.GetValue<MenuBool>("AssassinSetClick").Enabled)
            {
                foreach (var objAiHero in from hero in ObjectManager.Get<AIHeroClient>()
                                          where hero.IsValidTarget()
                                          select hero
                                              into h
                                          orderby h.Distance(Game.CursorPos) descending
                                          select h
                                                  into enemy
                                          where enemy.Distance(Game.CursorPos) < 150f
                                          select enemy)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        var xSelect =
                            assassin.GetValue<MenuList>("AssassinSelectOption").Index;
                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                list.GetValue<MenuBool>("Assassin" + objAiHero.CharacterName).SetValue(true);
                                Game.Print(
                                    string.Format(
                                        "<font color='FFFFFF'>Added to Assassin List</font> <font color='#09F000'>{0} ({1})</font>",
                                        objAiHero.Name, objAiHero.CharacterName));
                                break;
                            case 1:
                                var menuStatus = list.GetValue<MenuBool>("Assassin" + objAiHero.CharacterName).Enabled;
                                list.GetValue<MenuBool>("Assassin" + objAiHero.CharacterName).SetValue(!menuStatus);
                                Game.Print(
                                    string.Format("<font color='{0}'>{1}</font> <font color='#09F000'>{2} ({3})</font>",
                                        !menuStatus ? "#FFFFFF" : "#FF8877",
                                        !menuStatus ? "Added to Assassin List:" : "Removed from Assassin List:",
                                        objAiHero.Name, objAiHero.CharacterName));
                                break;
                        }
                    }
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!assassin.GetValue<MenuBool>("AssassinActive").Enabled)
                return;

            var drawSearch = draw.GetValue<MenuColor>("DrawSearch").Color;
            var drawActive = draw.GetValue<MenuColor>("DrawActive").Color;
            var drawNearest = draw.GetValue<MenuColor>("DrawNearest").Color;

            var drawSearchRange = assassin.GetValue<MenuSlider>("AssassinSearchRange").Value;
            if (!drawSearch.Equals(SharpDX.Color.Transparent))
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, drawSearchRange, drawSearch.ToSystemColor());
            }

            foreach (
                var enemy in
                    ObjectManager.Get<AIHeroClient>()
                        .Where(enemy => enemy.Team != ObjectManager.Player.Team)
                        .Where(
                            enemy =>
                                enemy.IsVisible &&
                                list.GetValue<MenuBool>("Assassin" + enemy.CharacterName) != null &&
                                !enemy.IsDead)
                        .Where(
                            enemy => list.GetValue<MenuBool>("Assassin" + enemy.CharacterName).Enabled))
            {
                if (ObjectManager.Player.Distance(enemy.Position) < drawSearchRange)
                {
                    if (!drawActive.Equals(SharpDX.Color.Transparent))
                        Render.Circle.DrawCircle(enemy.Position, 85f, drawActive.ToSystemColor());
                }
                else if (ObjectManager.Player.Distance(enemy.Position) > drawSearchRange &&
                         ObjectManager.Player.Distance(enemy.Position) < drawSearchRange + 400)
                {
                    if (!drawNearest.Equals(SharpDX.Color.Transparent))
                        Render.Circle.DrawCircle(enemy.Position, 85f, drawNearest.ToSystemColor());
                }
            }
        }
    }
}