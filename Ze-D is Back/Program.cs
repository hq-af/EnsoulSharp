#region
/*
 * Original author : jackisback
 * Ported by : hq!af 
 * Credits to:
 * Trees (Damage indicator)
 * Kurisu (ult on dangerous)
 * xQx assasin target selector
 */
using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;

#endregion


namespace zedisback
{
    class Program
    {
        private const string ChampionName = "Zed";
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Orbwalker _orbwalker;
        public static Menu _config, combo, harass, farm, lanefarm, lasthit, jungle, misc, dangerous, drawings;
        public static Menu TargetSelectorMenu;
        private static AIHeroClient _player;
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis, _youmuu;
        private static Vector3 linepos;
        private static Vector3 castpos;
        private static int clockon;
        private static int countults;
        private static int countdanger;
        private static int ticktock;
        private static Vector3 rpos;
        private static int shadowdelay = 0;
        private static int delayw = 500;

        private static string liveVersion = "https://raw.githubusercontent.com/hq-af/EnsoulSharp/master/Ze-D%20is%20Back/Properties/AssemblyInfo.cs?t=" + DateTimeOffset.Now.ToUnixTimeSeconds();
        public static AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();

        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += Game_OnGameLoad;
        }

        static async void CheckVersion()
        {
            if (liveVersion.Equals(""))
            {
                Game.Print("live version uri not setup");
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                try
                {
                    string resp = await client.GetStringAsync(liveVersion);
                    Match match = Regex.Match(resp, @"^\[assembly: AssemblyVersion\(""([0-9\.]+)""\)\]", RegexOptions.Multiline);

                    string current = assembly.Version.ToString();
                    string live = match.Groups[1].ToString();

                    Game.Print(match.Success ? "yes" : "no");

                    if (!current.Equals(live))
                    {
                        Game.Print($"<font color='#c63737'>Ze-D is Back - new version available : <b>v{live}</b> !! UPDATE NEEDED !!</font>");
                    }

                }
                catch (Exception e) { Game.Print($"version check failed : {e.Message}"); }
            }
        }


        static void Game_OnGameLoad()
        {
            try
            {
                _player = ObjectManager.Player;
                if (ObjectManager.Player.CharacterName != ChampionName) return;
                _q = new Spell(SpellSlot.Q, 900f);
                _w = new Spell(SpellSlot.W, 700f);
                _e = new Spell(SpellSlot.E, 270f);
                _r = new Spell(SpellSlot.R, 650f);

                _q.SetSkillshot(0.25f, 50f, 1700f, false, SpellType.Line);

                _bilge = new Items.Item(3144, 475f);
                _blade = new Items.Item(3153, 425f);
                _hydra = new Items.Item(3074, 250f);
                _tiamat = new Items.Item(3077, 250f);
                _rand = new Items.Item(3143, 490f);
                _lotis = new Items.Item(3190, 590f);
                _youmuu = new Items.Item(3142, 10);
                _igniteSlot = _player.GetSpellSlot("SummonerDot");

                var enemy = from hero in ObjectManager.Get<AIHeroClient>()
                            where hero.IsEnemy == true
                            select hero;
                // Just menu things test
                _config = new Menu("Zed Is Back", "Zed Is Back", true);

                TargetSelectorMenu = new Menu("Target Selector", "Assassin Manager");
                

                _orbwalker = new Orbwalker();

                //Combo
                combo = new Menu("Combo", "Combo");

                combo.Add(new MenuBool("UseWC", "Use W (also gap close)", true));
                combo.Add(new MenuBool("UseIgnitecombo", "Use Ignite(rush for it)", true));
                combo.Add(new MenuBool("UseUlt", "Use Ultimate", true));
                combo.Add(new MenuKeyBind("ActiveCombo", "Combo!", Keys.Space, KeyBindType.Press));
                combo.Add(new MenuKeyBind("TheLine", "The Line Combo", Keys.T, KeyBindType.Press));
                _config.Add(combo);

                //Harass
                harass = new Menu("Harass", "Harass");

                harass.Add(new MenuKeyBind("longhar", "Long Poke (toggle)", Keys.U, KeyBindType.Toggle));
                //harass.Add(new MenuBool("UseItemsharass", "Use Tiamat/Hydra", true));
                harass.Add(new MenuBool("UseWH", "Use W", true));
                harass.Add(new MenuKeyBind("ActiveHarass", "Harass!", Keys.C, KeyBindType.Press));
                _config.Add(harass);

                //items
                /*_config.AddSubMenu(new Menu("items", "items"));
                _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Tiamat", "Use Tiamat")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hydra", "Use Hydra")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omen", "Use Randuin Omen"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omenenemys", "Randuin if enemys>").SetValue(new Slider(2, 1, 5)));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotis", "Use Iron Solari"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotisminhp", "Solari if Ally Hp<").SetValue(new Slider(35, 1, 100)));
                */

                //Farm
                farm = new Menu("Farm", "Farm");
                lanefarm = new Menu("LaneFarm", "LaneFarm");
                //lanefarm.Add(new MenuBool("UseItemslane", "Use Hydra/Tiamat", true));
                lanefarm.Add(new MenuBool("UseQL", "Q LaneClear", true));
                lanefarm.Add(new MenuBool("UseEL", "E LaneClear", true));
                lanefarm.Add(new MenuSlider("Energylane", "Energy Lane% >", 45, 1, 100));
                lanefarm.Add(new MenuKeyBind("Activelane", "Lane clear!", Keys.V, KeyBindType.Press));
                farm.Add(lanefarm);

                lasthit = new Menu("LastHit", "LastHit");
                lasthit.Add(new MenuBool("UseQLH", "Q LastHit", true));
                lasthit.Add(new MenuBool("UseELH", "E LastHit", true));
                lasthit.Add(new MenuSlider("Energylast", "Energy lasthit% >", 85, 1, 100));
                lasthit.Add(new MenuKeyBind("ActiveLast", "LastHit!", Keys.X, KeyBindType.Press));
                farm.Add(lasthit);

                jungle = new Menu("Jungle", "Jungle");
                //jungle.Add(new MenuBool("UseItemsjungle", "Use Hydra/Tiamat", true));
                jungle.Add(new MenuBool("UseQJ", "Q Jungle", true));
                jungle.Add(new MenuBool("UseWJ", "W Jungle", true));
                jungle.Add(new MenuBool("UseEJ", "E Jungle", true));
                jungle.Add(new MenuSlider("Energyjungle", "Energy Jungle% >", 85, 1, 100));
                jungle.Add(new MenuKeyBind("Activejungle", "Jungle!", Keys.V, KeyBindType.Press));
                farm.Add(jungle);
                _config.Add(farm);

                //Misc
                misc = new Menu("Misc", "Misc");
                misc.Add(new MenuBool("UseIgnitekill", "Use Ignite KillSteal", true));
                misc.Add(new MenuBool("UseQM", "Use Q KillSteal", true));
                misc.Add(new MenuBool("UseEM", "Use E KillSteal", true));
                misc.Add(new MenuBool("AutoE", "Auto E", true));
                misc.Add(new MenuBool("rdodge", "R Dodge Dangerous", true));

                dangerous = new Menu("DangerousSpells", "Dangerous Spells");
                foreach (var e in enemy)
                {
                    SpellDataInst rdata = e.Spellbook.GetSpell(SpellSlot.R);
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(rdata.SData.Name)))
                        dangerous.Add(new MenuBool("ds" + e.CharacterName, rdata.SData.Name, true));
                }
                misc.Add(dangerous);
                _config.Add(misc);

                //Drawings
                drawings = new Menu("Drawings", "Drawings");
                drawings.Add(new MenuBool("DrawQ", "Draw Q", true));
                drawings.Add(new MenuBool("DrawE", "Draw E", true));
                drawings.Add(new MenuBool("DrawQW", "Draw long harras", true));
                drawings.Add(new MenuBool("DrawR", "Draw R", true));
                //drawings.Add(new MenuBool("DrawHP", "Draw HP bar", true));
                drawings.Add(new MenuBool("shadowd", "Shadow Position", true));
                drawings.Add(new MenuBool("damagetest", "Damage Text", true));
                drawings.Add(new MenuBool("CircleLag", "Lag Free Circles", true));
                //drawings.Add(new MenuSlider("CircleQuality", "Circles Quality", 100, 10, 100));
                //drawings.Add(new MenuSlider("CircleThickness", "Circles Thickness", 1, 1, 10));
                _config.Add(drawings);
                _config.Attach();

                new AssassinManager();
                _config.Add(TargetSelectorMenu);
                //new DamageIndicator();
                //DamageIndicator.DamageToUnit = ComboDamage;

                Game.Print("<font color='#881df2'>Zed is Back by jackisback (ported by hq!af)</font> Loaded.");

                Drawing.OnDraw += Drawing_OnDraw;
                Game.OnUpdate += Game_OnUpdate;
                AIBaseClient.OnProcessSpellCast += OnProcessSpell;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.Print("Error something went wrong");
            }



        }

        private static void OnProcessSpell(AIBaseClient unit, AIBaseClientProcessSpellCastEventArgs castedSpell)
        {
            if (unit.Type != GameObjectType.AIHeroClient)
                return;
            if (unit.IsEnemy)
            {
                if (misc.GetValue<MenuBool>("rdodge").Enabled && _r.IsReady() && UltStage == UltCastStage.First &&
                dangerous.GetValue<MenuBool>("ds" + unit.CharacterName).Enabled)
                {
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(castedSpell.SData.Name)) &&
                        (unit.Distance(_player.Position) < 650f || _player.Distance(castedSpell.End) <= 250f))
                    {
                        if (castedSpell.SData.Name == "SyndraR")
                        {
                            clockon = Environment.TickCount + 150;
                            countdanger = countdanger + 1;
                        }
                        else
                        {
                            var target = TargetSelector.GetTarget(640, DamageType.Physical);
                            _r.Cast(target);
                        }
                    }
                }
            }

            if (unit.IsMe && castedSpell.SData.Name == "zedult")
            {
                ticktock = Environment.TickCount + 200;

            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (combo.GetValue<MenuKeyBind>("ActiveCombo").Active)
            {
                Combo(GetEnemy);

            }
            if (combo.GetValue<MenuKeyBind>("TheLine").Active)
            {
                TheLine(GetEnemy);
            }
            if (harass.GetValue<MenuKeyBind>("ActiveHarass").Active)
            {
                Harass(GetEnemy);

            }
            if (lanefarm.GetValue<MenuKeyBind>("Activelane").Active)
            {
                Laneclear();
            }
            if (jungle.GetValue<MenuKeyBind>("Activejungle").Active)
            {
                JungleClear();
            }
            if (lasthit.GetValue<MenuKeyBind>("ActiveLast").Active)
            {
                LastHit();
            }
            if (misc.GetValue<MenuBool>("AutoE").Enabled)
            {
                CastE();
            }

            if (Environment.TickCount >= clockon && countdanger > countults)
            {
                _r.Cast(TargetSelector.GetTarget(640, DamageType.Physical));
                countults = countults + 1;
            }


            if (LastCast.GetLastCastedSpell(_player).Name.StartsWith("ZedR"))
            {
                AIMinionClient shadow;
                shadow = ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");

                rpos = shadow.Position;
            }


            _player = ObjectManager.Player;


            KillSteal();

        }

        private static float ComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            /*if (Items.HasItem(3077) && Items.CanUseItem(3077))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Tiamat);
            if (Items.HasItem(3074) && Items.CanUseItem(3074))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Hydra);
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Botrk);
            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Bilgewater);
            */
            if (_q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q);
            if (_w.IsReady() && _q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q) / 2;
            if (_e.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);
            damage += (_r.Level * 0.15 + 0.05) *
                      (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite));

            return (float)damage;
        }

        private static void Combo(AIHeroClient t)
        {
            var target = t;
            var overkill = _player.GetSpellDamage(target, SpellSlot.Q) + _player.GetSpellDamage(target, SpellSlot.E) + _player.GetAutoAttackDamage(target, true) * 2;
            var doubleu = _player.Spellbook.GetSpell(SpellSlot.W);


            if (combo.GetValue<MenuBool>("UseUlt").Enabled && UltStage == UltCastStage.First && (overkill < target.Health ||
                (!_w.IsReady() && doubleu.Cooldown > 2f && _player.GetSpellDamage(target, SpellSlot.Q) < target.Health && target.Distance(_player.Position) > 400)))
            {
                if ((target.Distance(_player.Position) > 700 && target.MoveSpeed > _player.MoveSpeed) || target.Distance(_player.Position) > 800)
                {
                    CastW(target);
                    _w.Cast();

                }
                _r.Cast(target);
            }

            else
            {
                if (target != null && combo.GetValue<MenuBool>("UseIgnitecombo").Enabled && _igniteSlot != SpellSlot.Unknown &&
                        _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (ComboDamage(target) > target.Health || target.HasBuff("zedulttargetmark"))
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                }
                if (target != null && ShadowStage == ShadowCastStage.First && combo.GetValue<MenuBool>("UseWC").Enabled &&
                        target.Distance(_player.Position) > 400 && target.Distance(_player.Position) < 1300)
                {
                    CastW(target);
                }
                if (target != null && ShadowStage == ShadowCastStage.Second && combo.GetValue<MenuBool>("UseWC").Enabled &&
                    target.Distance(WShadow.Position) < target.Distance(_player.Position))
                {
                    _w.Cast();
                }


                //UseItemes(target);
                CastE();
                CastQ(target);

            }


        }

        private static void TheLine(AIHeroClient t)
        {
            var target = t;

            if (target == null)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if (!_r.IsReady() || target.Distance(_player.Position) >= 640)
            {
                return;
            }
            if (UltStage == UltCastStage.First)
                _r.Cast(target);
            linepos = target.Position.Extend(_player.Position, -500);

            if (target != null && ShadowStage == ShadowCastStage.First && UltStage == UltCastStage.Second)
            {
                //UseItemes(target);

                if (!LastCast.GetLastCastedSpell(_player).SpellData.Name.StartsWith("ZedW"))
                {
                    _w.Cast(linepos);
                    CastE();
                    CastQ(target);


                    if (target != null && combo.GetValue<MenuBool>("UseIgnitecombo").Enabled && _igniteSlot != SpellSlot.Unknown &&
                            _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }

                }
            }

            if (target != null && WShadow != null && UltStage == UltCastStage.Second && target.Distance(_player.Position) > 250 && (target.Distance(WShadow.Position) < target.Distance(_player.Position)))
            {
                _w.Cast();
            }

        }

        private static void _CastQ(AIHeroClient target)
        {
            throw new NotImplementedException();
        }

        private static void Harass(AIHeroClient t)
        {
            var target = t;

            //var useItemsH = _config.Item("UseItemsharass").GetValue<bool>();

            if (target.IsValidTarget() && harass.GetValue<MenuKeyBind>("longhar").Active && _w.IsReady() && _q.IsReady() && ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost && target.Distance(_player.Position) > 850 &&
                target.Distance(_player.Position) < 1400)
            {
                CastW(target);
            }

            if (target.IsValidTarget() && (ShadowStage == ShadowCastStage.Second || ShadowStage == ShadowCastStage.Cooldown || !(harass.GetValue<MenuBool>("UseWH").Enabled))
                            && _q.IsReady() &&
                                (target.Distance(_player.Position) <= 900 || target.Distance(WShadow.Position) <= 900))
            {
                CastQ(target);
            }

            if (target.IsValidTarget() && _w.IsReady() && _q.IsReady() && harass.GetValue<MenuBool>("UseWH").Enabled &&
                ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost)
            {
                if (target.Distance(_player.Position) < 750)

                    CastW(target);
            }

            CastE();

            /*if (useItemsH && _tiamat.IsReady() && target.Distance(_player.Position) < _tiamat.Range)
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && target.Distance(_player.Position) < _hydra.Range)
            {
                _hydra.Cast();
            }*/

        }

        private static void Laneclear()
        {
            var allMinionsQ = GameObjects.GetMinions(ObjectManager.Player.Position, _q.Range);
            var allMinionsE = GameObjects.GetMinions(ObjectManager.Player.Position, _e.Range);
            var mymana = (_player.Mana >= (_player.MaxMana * lanefarm.GetValue<MenuSlider>("Energylane").Value) / 100);

            //var useItemsl = lanefarm.GetValue<MenuBool>("UseItemslane").Enabled;
            var useQl = lanefarm.GetValue<MenuBool>("UseQL").Enabled;
            var useEl = lanefarm.GetValue<MenuBool>("UseEL").Enabled;
            if (_q.IsReady() && useQl && mymana)
            {
                var fl2 = _q.GetLineFarmLocation(allMinionsQ, _q.Width);

                if (fl2.MinionsHit >= 3)
                {
                    _q.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!ObjectManager.Player.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                            _q.Cast(minion);
            }

            if (_e.IsReady() && useEl && mymana)
            {
                if (allMinionsE.Count > 2)
                {
                    _e.Cast();
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!ObjectManager.Player.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E))
                            _e.Cast();
            }

            /*if (useItemsl && _tiamat.IsReady() && allMinionsE.Count > 2)
            {
                _tiamat.Cast();
            }
            if (useItemsl && _hydra.IsReady() && allMinionsE.Count > 2)
            {
                _hydra.Cast();
            }*/
        }

        private static void LastHit()
        {
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, _q.Range, MinionTypes.All);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * lasthit.GetValue<MenuSlider>("Energylast").Value) / 100);
            var useQ = lasthit.GetValue<MenuBool>("UseQLH").Enabled;
            var useE = lasthit.GetValue<MenuBool>("UseELH").Enabled;
            foreach (var minion in allMinions)
            {
                if (mymana && useQ && _q.IsReady() && _player.Distance(minion.Position) < _q.Range &&
                    minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion);
                }

                if (mymana && _e.IsReady() && useE && _player.Distance(minion.Position) < _e.Range &&
                    minion.Health < 0.95 * _player.GetSpellDamage(minion, SpellSlot.E))
                {
                    _e.Cast();
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = GameObjects.GetJungles(_player.Position, _q.Range,
                JungleType.All,
                JungleOrderTypes.MaxHealth);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * jungle.GetValue<MenuSlider>("Energyjungle").Value) / 100);
            //var useItemsJ = jungle.GetValue<MenuBool>("UseItemsjungle").Enabled;
            var useQ = jungle.GetValue<MenuBool>("UseQJ").Enabled;
            var useW = jungle.GetValue<MenuBool>("UseWJ").Enabled;
            var useE = jungle.GetValue<MenuBool>("UseEJ").Enabled;

            if (mobs.Count > 0)
            {
                var mob = mobs.First();
                if (mymana && _w.IsReady() && useW && _player.Distance(mob.Position) < _q.Range)
                {
                    _w.Cast(mob.Position);
                }
                if (mymana && useQ && _q.IsReady() && _player.Distance(mob.Position) < _q.Range)
                {
                    CastQ(mob);
                }
                if (mymana && _e.IsReady() && useE && _player.Distance(mob.Position) < _e.Range)
                {
                    _e.Cast();
                }

                /*if (useItemsJ && _tiamat.IsReady() && _player.Distance(mob.Position) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsJ && _hydra.IsReady() && _player.Distance(mob.ServerPosition) < _hydra.Range)
                {
                    _hydra.Cast();
                }*/
            }

        }
        static AIHeroClient GetEnemy
        {
            get
            {
                var assassinRange = AssassinManager.assassin.GetValue<MenuSlider>("AssassinSearchRange").Value;

                var vEnemy = ObjectManager.Get<AIHeroClient>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            AssassinManager.list.GetValue<MenuBool>("Assassin" + enemy.CharacterName) != null &&
                            AssassinManager.list.GetValue<MenuBool>("Assassin" + enemy.CharacterName).Enabled &&
                            ObjectManager.Player.Distance(enemy.Position) < assassinRange);

                if (AssassinManager.assassin.GetValue<MenuList>("AssassinSelectOption").Index == 1)
                {
                    vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
                }

                AIHeroClient[] objAiHeroes = vEnemy as AIHeroClient[] ?? vEnemy.ToArray();

                AIHeroClient t = !objAiHeroes.Any()
                    ? TargetSelector.GetTarget(1400, DamageType.Magical)
                    : objAiHeroes[0];

                return t;

            }

        }

        /*private static void UseItemes(AIHeroClient target)
        {
            var iBilge = _config.Item("Bilge").GetValue<bool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth * (_config.Item("BilgeEnemyhp").GetValue<Slider>().Value) / 100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth * (_config.Item("Bilgemyhp").GetValue<Slider>().Value) / 100);
            var iBlade = _config.Item("Blade").GetValue<bool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth * (_config.Item("BladeEnemyhp").GetValue<Slider>().Value) / 100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth * (_config.Item("Blademyhp").GetValue<Slider>().Value) / 100);
            var iOmen = _config.Item("Omen").GetValue<bool>();
            var iOmenenemys = ObjectManager.Get<AIHeroClient>().Count(hero => hero.IsValidTarget(450)) >=
                              _config.Item("Omenenemys").GetValue<Slider>().Value;
            var iTiamat = _config.Item("Tiamat").GetValue<bool>();
            var iHydra = _config.Item("Hydra").GetValue<bool>();
            var ilotis = _config.Item("lotis").GetValue<bool>();
            var iYoumuu = _config.Item("Youmuu").GetValue<bool>();
            //var ihp = _config.Item("Hppotion").GetValue<bool>();
            // var ihpuse = _player.Health <= (_player.MaxHealth * (_config.Item("Hppotionuse").GetValue<Slider>().Value) / 100);
            //var imp = _config.Item("Mppotion").GetValue<bool>();
            //var impuse = _player.Health <= (_player.MaxHealth * (_config.Item("Mppotionuse").GetValue<Slider>().Value) / 100);

            if (_player.Distance(target.ServerPosition) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iTiamat && _tiamat.IsReady())
            {
                _tiamat.Cast();

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iHydra && _hydra.IsReady())
            {
                _hydra.Cast();

            }
            if (iOmenenemys && iOmen && _rand.IsReady())
            {
                _rand.Cast();

            }
            if (ilotis)
            {
                foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsAlly || hero.IsMe))
                {
                    if (hero.Health <= (hero.MaxHealth * (_config.Item("lotisminhp").GetValue<Slider>().Value) / 100) &&
                        hero.Distance(_player.ServerPosition) <= _lotis.Range && _lotis.IsReady())
                        _lotis.Cast();
                }
            }
            if (_player.Distance(target.ServerPosition) <= 350 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();

            }
        }*/

        private static AIMinionClient WShadow
        {
            get
            {
                return
                    ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(minion => minion.IsVisible && !minion.IsDead && minion.IsAlly && (minion.Position != rpos) && minion.Name == "Shadow");
            }
        }
        private static AIMinionClient RShadow
        {
            get
            {
                return
                    ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(minion => minion.IsVisible && !minion.IsDead && minion.IsAlly && (minion.Position == rpos) && minion.Name == "Shadow");
            }
        }

        private static UltCastStage UltStage
        {
            get
            {
                if (!_r.IsReady()) return UltCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR"
                //return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "zedult"
                    ? UltCastStage.First
                    : UltCastStage.Second);
            }
        }


        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!_w.IsReady()) return ShadowCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedW"
                //return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second);

            }
        }

        private static void CastW(AIBaseClient target)
        {
            if (delayw >= Environment.TickCount - shadowdelay || ShadowStage != ShadowCastStage.First ||
                (target.HasBuff("zedulttargetmark") && ObjectManager.Player.GetLastCastedSpell().Name.StartsWith("ZedR") && UltStage == UltCastStage.Cooldown))
                return;

            var herew = target.Position.Extend(ObjectManager.Player.Position, -200);

            _w.Cast(herew);
            shadowdelay = Environment.TickCount;

        }

        private static void CastQ(AIBaseClient target)
        {
            if (!_q.IsReady()) return;

            if (WShadow != null && target.Distance(WShadow.Position) <= 900 && target.Distance(_player.Position) > 450)
            {

                var shadowpred = _q.GetPrediction(target);
                _q.UpdateSourcePosition(WShadow.Position, WShadow.Position);
                if (shadowpred.Hitchance >= HitChance.Medium)
                    _q.Cast(target);


            }
            else
            {

                _q.UpdateSourcePosition(_player.Position, _player.Position);
                var normalpred = _q.GetPrediction(target);

                if (normalpred.CastPosition.Distance(_player.Position) < 900 && normalpred.Hitchance >= HitChance.Medium)
                {
                    _q.Cast(target);
                }


            }


        }

        private static void CastE()
        {
            if (!_e.IsReady()) return;
            if (ObjectManager.Get<AIHeroClient>()
                .Count(
                    hero =>
                        hero.IsValidTarget() &&
                        (hero.Distance(ObjectManager.Player.Position) <= _e.Range ||
                         (WShadow != null && hero.Distance(WShadow.Position) <= _e.Range))) > 0)
                _e.Cast();
        }

        internal enum UltCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(2000, DamageType.Physical);
            var igniteDmg = _player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            if (target.IsValidTarget() && misc.GetValue<MenuBool>("UseIgnitekill").Enabled && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (igniteDmg > target.Health && _player.Distance(target.Position) <= 600)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }
            if (target.IsValidTarget() && _q.IsReady() && misc.GetValue<MenuBool>("UseQM").Enabled && _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.Position) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.Position) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.Position, WShadow.Position);
                    _q.Cast(target);
                }
                else if (RShadow != null && RShadow.Distance(target.Position) <= _q.Range)
                {
                    _q.UpdateSourcePosition(RShadow.Position, RShadow.Position);
                    _q.Cast(target);
                }
            }

            if (target.IsValidTarget() && _q.IsReady() && misc.GetValue<MenuBool>("UseQM").Enabled && _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.Position) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.Position) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.Position, WShadow.Position);
                    _q.Cast(target);
                }
            }
            if (_e.IsReady() && misc.GetValue<MenuBool>("UseEM").Enabled)
            {
                var t = TargetSelector.GetTarget(_e.Range, DamageType.Physical);
                if (_e.GetDamage(t) > t.Health && (_player.Distance(t.Position) <= _e.Range || WShadow.Distance(t.Position) <= _e.Range))
                {
                    _e.Cast();
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (RShadow != null)
            {
                Render.Circle.DrawCircle(RShadow.Position, RShadow.BoundingRadius * 2, Color.Blue);
            }



            if (drawings.GetValue<MenuBool>("shadowd").Enabled)
            {
                if (WShadow != null)
                {
                    if (ShadowStage == ShadowCastStage.Cooldown)
                    {
                        Render.Circle.DrawCircle(WShadow.Position, WShadow.BoundingRadius * 1.5f, Color.Red);
                    }
                    else if (WShadow != null && ShadowStage == ShadowCastStage.Second)
                    {
                        Render.Circle.DrawCircle(WShadow.Position, WShadow.BoundingRadius * 1.5f, Color.Yellow);
                    }
                }
            }
            if (drawings.GetValue<MenuBool>("damagetest").Enabled)
            {
                foreach (
                    var enemyVisible in
                        ObjectManager.Get<AIHeroClient>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Rekt");
                    }
                    else if (ComboDamage(enemyVisible) + _player.GetAutoAttackDamage(enemyVisible, true) * 2 >
                             enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange,
                            "Combo + 2 AA = Rekt");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                            "Unkillable with combo + 2AA");
                }
            }

            if (drawings.GetValue<MenuBool>("CircleLag").Enabled)
            {
                if (drawings.GetValue<MenuBool>("DrawQ").Enabled)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Blue);
                }
                if (drawings.GetValue<MenuBool>("DrawE").Enabled)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (drawings.GetValue<MenuBool>("DrawQW").Enabled && harass.GetValue<MenuKeyBind>("longhar").Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.Yellow);
                }
                if (drawings.GetValue<MenuBool>("DrawR").Enabled)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.Blue);
                }
            }
            else
            {
                if (drawings.GetValue<MenuBool>("DrawQ").Enabled)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
                }
                if (drawings.GetValue<MenuBool>("DrawE").Enabled)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (drawings.GetValue<MenuBool>("DrawQW").Enabled && harass.GetValue<MenuKeyBind>("longhar").Active)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.White);
                }
                if (drawings.GetValue<MenuBool>("DrawR").Enabled)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
                }
            }
        }
    }
}