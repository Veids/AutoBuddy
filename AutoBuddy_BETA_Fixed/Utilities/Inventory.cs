using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace AutoBuddy.Utilities
{
    internal static class Inventory
    {
        public static bool SameItems(int id1, int id2)
        {
            if (id1 == id2) return true;
            if ((id1 == 3003 && id2 == 3040) || (id2 == 3003 && id1 == 3040)) return true;//seraphs
            if ((id1 == 3004 && id2 == 3043) || (id2 == 3004 && id1 == 3043)) return true;//muramana

            return false;
        }

        public static int GetItemSlot(int id)
        {
            for (int i = 0; i < ObjectManager.Player.InventoryItems.Length; i++)
            {
                if ((int)ObjectManager.Player.InventoryItems[i].Id == id)
                    return i;
            }
            return -1;
        }

        public static InventorySlot GetItemSlot(Item item)
        {
            return Player.Instance.InventoryItems.FirstOrDefault(slot => slot.Id == item.Id);
        }

        public static int GetHealtlyConsumableSlot()
        {
            for (int i = 0; i < ObjectManager.Player.InventoryItems.Length; ++i)
            {
                if (ObjectManager.Player.InventoryItems[i].Id.IsHealthlyConsumable())
                    return i;
            }
            return -1;
        }

        public static int GetHPotionSlot()
        {
            for (int i = 0; i < ObjectManager.Player.InventoryItems.Length; ++i)
            {
                if (ObjectManager.Player.InventoryItems[i].Id.IsHPotion())
                    return i;
            }
            return -1;
        }
    }
}