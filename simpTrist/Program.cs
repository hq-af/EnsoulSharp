using System;
using System.Drawing;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using static EnsoulSharp.ObjectManager;
using static EnsoulSharp.SDK.Render;
using Color = System.Drawing.Color;

namespace simpTrist
{
    class Program
    {
        static void log(object obj)
        {
            Logging.Write(LogLevel.Debug, obj, new object[0]);
        }

        static void Main(string[] args)
        {
#if DEBUG
            //Hacks.Console = true;
#endif

            SetupMenu();
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            if (Player.CharacterName != "Tristana") return;

            Game.Print("simpTrist by hq!af <github.com/hq-af> loaded.");

            GameEvent.OnGameTick += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
        }

        private static void OnBeforeAttack(object sender, BeforeAttackEventArgs e)
        {
            if (e.Target.GetType() != typeof(AIHeroClient) || Orbwalker.ActiveMode != OrbwalkerMode.Combo || !mainMenu.GetValue<MenuBool>("useQCombo").Enabled) return;

            if (Q.IsReady()) Q.Cast();
        }

        private static Menu mainMenu, eSelector, draw;
        private static void SetupMenu()
        {
            mainMenu = new Menu("simpTrist", "simpTrist", true);
            eSelector = new Menu("eSelector", "E Combo/Jump Whitelist");

            foreach(AIHeroClient hero in Get<AIHeroClient>().Where(x => x.Team != Player.Team))
            {
                eSelector.Add(new MenuBool("eSelect" + hero.CharacterName, hero.CharacterName));
            }
            mainMenu.Add(eSelector);

            mainMenu.Add(new MenuBool("useQCombo", "Use Q combo"));
            mainMenu.Add(new MenuKeyBind("manualE", "Semi-Manual E", Keys.E, KeyBindType.Press));
            mainMenu.Add(new MenuKeyBind("manualR", "Semi-Manual R", Keys.R, KeyBindType.Press));
            mainMenu.Add(new MenuKeyBind("pushBack", "Push-back closest", Keys.T, KeyBindType.Press));
            mainMenu.Add(new MenuKeyBind("panicClear", "Panic-clear modifier", Keys.MButton, KeyBindType.Press));

            draw = new Menu("draw", "Drawings");
            draw.Add(new MenuBool("drawW", "W Range"));
            draw.Add(new MenuBool("drawWOE", "^ Only if ready", false));
            draw.Add(new MenuBool("drawPanic", "Panic-clear status"));
            draw.Add(new MenuSliderButton("drawTarget", "Draw target", 1500, 500, 2000, true));

            mainMenu.Add(draw);
            mainMenu.Attach();
        }


        private static Spell Q = new Spell(SpellSlot.Q);
        private static Spell W = new Spell(SpellSlot.W, 900);
        private static Spell E => new Spell(SpellSlot.E, Player.AttackRange + 50);
        private static Spell R => new Spell(SpellSlot.R, Player.AttackRange + 50);

        private static Text status = new Text("Panic-clear", default(Vector2), 14, SharpDX.Color.Red);
        private static void OnDraw(EventArgs args)
        {
            MenuSliderButton drawTarget = mainMenu.GetValue<MenuSliderButton>("drawTarget");
            if (drawTarget.Enabled)
            {
                AIHeroClient target = TargetSelector.GetTarget(drawTarget.Value);
                if (target != null) Circle.DrawCircle(target.Position, target.BoundingRadius + 50, Color.Yellow, 2);
            }

            if (mainMenu.GetValue<MenuBool>("drawW").Enabled && (W.IsReady() || !mainMenu.GetValue<MenuBool>("drawWOE").Enabled))
                Circle.DrawCircle(Player.Position, W.Range, Color.White, 2);

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && mainMenu.GetValue<MenuBool>("drawPanic").Enabled && mainMenu.GetValue<MenuKeyBind>("panicClear").Active)
            {
                Drawing.WorldToScreen(Game.CursorPos, out Vector2 mouse);
                status.X = (int)mouse.X;
                status.Y = (int)mouse.Y - 20;
                status.Draw();
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (mainMenu.GetValue<MenuKeyBind>("pushBack").Active) PushBack();
            else if (mainMenu.GetValue<MenuKeyBind>("manualR").Active) Manual(R);
            else if (mainMenu.GetValue<MenuKeyBind>("manualE").Active) Manual(E);


            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Player.IsDashing()) EWhitelist();
            else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && mainMenu.GetValue<MenuKeyBind>("panicClear").Active) PanicClear();
        }

        private static void PanicClear()
        {
            AIMinionClient minion = Get<AIMinionClient>().Where(x => x.GetMinionType() != MinionTypes.JunglePlant && x.GetMinionType() != MinionTypes.Ward && x.Team != Player.Team && !x.IsDead && x.IsValid && x.Distance(Player) < Player.AttackRange).OrderBy(x => x.Health).FirstOrDefault();
            if (minion == null) return;

            Orbwalker.ForceTarget = minion;
        }

        private static void Manual(Spell spell)
        {
            AIHeroClient target = TargetSelector.GetTarget(spell.Range);
            if (target != null && spell.CanCast(target)) spell.Cast(target);
        }

        private static void EWhitelist()
        {
            Spell spell = E;

            AttackableUnit target = Orbwalker.GetTarget();
            if (target != null && target.GetType() == typeof(AIHeroClient) && eSelector.GetValue<MenuBool>("eSelect"+((AIHeroClient)target).CharacterName).Enabled && spell.CanCast((AIHeroClient)target)) spell.Cast((AIHeroClient)target);
        }

        private static void PushBack()
        {
            Spell spell = R;

            AIHeroClient target = Get<AIHeroClient>().Where(x => x.Team != Player.Team && !x.IsDead && x.IsValid && x.Distance(Player) <= spell.Range).OrderBy(x => x.Distance(Player)).FirstOrDefault();
            if (target != null && spell.CanCast(target)) spell.Cast(target);
        }

    }
}
