using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoBuddy.Humanizers;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace AutoBuddy.Utilities
{
    internal enum ShopActionType
    {
        Buy = 1,
        Sell = 2,
        StartHpPot = 3,
        StopHpPot = 4
    }

    public static class ShopGlobals
    {
        public static int GoldForNextItem=999999;
        public static string Next;
    }

     internal class AutoShop
     {
        private readonly Menu menu;
        private CheckBox enabled;

        private readonly string buildFile;
        private readonly string internalBuild;
        private readonly List<ItemAction> order = new List<ItemAction>();
        private static int current;
        private static int count;
        private static bool hppots;

        public AutoShop(Menu parentMenu, string build = "")
        {
            menu = parentMenu.AddSubMenu("AutoShop: " + AutoWalker.p.ChampionName, "AB_SHOP_" + AutoWalker.p.ChampionName);
            buildFile = Path.Combine(SandboxConfig.DataDirectory + "AutoBuddy\\Builds\\" +
               AutoWalker.p.ChampionName + "-" + Game.MapId + ".txt");
            internalBuild = build;

            CreateMenu();
            LoadBuild();
            Shopping();
        }

        private void LoadBuild()
        {
            if (!File.Exists(buildFile))
            {
                Chat.Print("Custom build doesn't exist: " + buildFile);
                if (!internalBuild.Equals(string.Empty))
                    LoadInternalBuild();
                return;
            }
            //Loading customBuild
            try
            {
                string s = File.ReadAllText(buildFile);
                if (s.Equals(string.Empty))
                {
                    Chat.Print("AutoBuddy: the build is empty.");
                    LoadInternalBuild();
                    return;
                }
                DeserializeBuild(s);

                Chat.Print("Loaded build from: " + buildFile);
            }
            catch (Exception e)
            {
                Chat.Print("AutoBuddy: couldn't load the build.");

                LoadInternalBuild();
                Console.WriteLine(e.Message);
            }
        }

        private void LoadInternalBuild()
        {
            try
            {
                if (internalBuild.Equals(string.Empty))
                {
                    Chat.Print("AutoBuddy: internal build is empty.");
                    return;
                }
                DeserializeBuild(internalBuild);
            }
            catch (Exception e)
            {
                Chat.Print("AutoBuddy: internal build load failed.");
                Console.WriteLine(e.Message);
                return;
            }
            Chat.Print("AutoBuddy: Internal build loaded.");
        }

        private void DeserializeBuild(string serialized)
        {
            foreach (string s in serialized.Split(','))
            {
                ItemAction ac = new ItemAction();
                bool flag = true;
                foreach (string s2 in s.Split(':'))
                {
                    if (flag)
                    {
                        ac.item = new Item(int.Parse(s2));
                        flag = false;
                    }
                    else ac.action = (ShopActionType) Enum.Parse(typeof (ShopActionType), s2, true);
                }

                if (ac.item.ItemInfo.AvailableForMap && ac.item.ItemInfo.ValidForPlayer)
                    order.Add(ac);
                else
                    Chat.Print("Item: " + ac.item.ItemInfo.Name +
                               " doesn't exist in current map or not valid for player");
            }
            count = order.Count;
        }

        private void Shopping()
        {
            if (!enabled.CurrentValue)
            {
                ShopGlobals.GoldForNextItem = 99999;
                Core.DelayAction(Shopping, 300);
                return;
            }

            if (!order.Any() || !ObjectManager.Player.IsInShopRange() || !ObjectManager.Player.IsDead ||
                current >= count)
            {
                Core.DelayAction(Shopping, 300);
                return;
            }

            ItemAction handle = order[current];

            ShopGlobals.GoldForNextItem = handle.item.GoldRequired();
            ShopGlobals.Next = handle.item.ItemInfo.Name;

            switch (handle.action)
            {
                case ShopActionType.Buy:
                    if (AutoWalker.p.Gold >= ShopGlobals.GoldForNextItem)
                    {
                        if (!handle.item.IsOwned())
                            Shop.BuyItem(handle.item.Id);
                        ++current;
                    }
                    break;

                case ShopActionType.Sell:
                    if (handle.item.IsOwned())
                        Shop.SellItem(Inventory.GetItemSlot(handle.item).Slot);
                    ++current;
                    break;

                case ShopActionType.StartHpPot:
                    hppots = true;
                    ++current;
                    break;

                case ShopActionType.StopHpPot:
                    hppots = false;
                    ++current;
                    break;

                default:
                    Chat.Print("Error: Wrong action, id: " + handle.item.Id + " action: " + handle.action);
                    ++current;
                    break;
            }

            if (current < count)
            {
                ShopGlobals.GoldForNextItem = order[current].item.GoldRequired();
                ShopGlobals.Next = order[current].item.ItemInfo.Name;
            }

            Core.DelayAction(HpPotsController, RandGen.r.Next(150, 300));
            Core.DelayAction(Shopping, RandGen.r.Next(600, 1000));
        }

        private static void HpPotsController()
        {
            if (hppots && !AutoWalker.p.InventoryItems.Any(it => it.Id.IsHealthlyConsumable()))
                Shop.BuyItem(ItemId.Health_Potion);
            else if (!hppots)
            {
                int slot = Inventory.GetHealtlyConsumableSlot();
                if (slot != -1)
                    Shop.SellItem(slot);
            }
        }

        private void CreateMenu()
        {
            Label l = new Label("Shopping list for " + Game.MapId);
            enabled = new CheckBox("AutoShop enabled");
            menu.Add("eeewgrververv", l);
            menu.Add(AutoWalker.p.ChampionName + "enabled", enabled);
        }

        public static bool InOrder { get { return current < count; }}

        private struct ItemAction
        {
            public ShopActionType action;
            public Item item;
        }
    }
}