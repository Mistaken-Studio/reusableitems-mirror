// -----------------------------------------------------------------------
// <copyright file="UsingConsumablePatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

using Exiled.API.Features;
using InventorySystem.Items.Usables;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;

namespace Mistaken.ReusableItems
{
    [HarmonyLib.HarmonyPatch(typeof(Consumable), nameof(Consumable.ServerOnUsingCompleted))]
    internal static class UsingConsumablePatch
    {
        internal static bool Prefix(Consumable __instance)
        {
            var ev = new Exiled.Events.EventArgs.UsedItemEventArgs(Player.Get(__instance.Owner), __instance);
            Exiled.Events.Handlers.Player.OnUsedItem(ev);
            ev.Player.SetGUI("reusable", PseudoGUIPosition.BOTTOM, null);
            if (ReusableItemsHandler.ReusableItems.TryGetValue(__instance.ItemSerial, out var data))
            {
                if (data.UsesLeft > 1)
                {
                    bool alreadyActivated = __instance._alreadyActivated;
                    __instance._alreadyActivated = false;
                    data.UsesLeft -= 1;
                    if (data.Cooldown != 0)
                        data.Item.Base.ServerSetGlobalItemCooldown(data.Cooldown);

                    __instance.OwnerInventory.NetworkCurItem = InventorySystem.Items.ItemIdentifier.None;
                    __instance.OwnerInventory.CurInstance = null;

                    UsableItemsController.GetHandler(__instance.Owner).CurrentUsable = CurrentlyUsedItem.None;
                    if (!alreadyActivated)
                        __instance.ActivateEffects();

                    __instance._alreadyActivated = false;
                    __instance.IsUsing = false;
                    __instance._useStopwatch.Stop();
                    return false;
                }

                ReusableItemsHandler.ReusableItems.Remove(__instance.ItemSerial);
            }

            if (__instance.Owner.characterClassManager.CurRole.team == Team.CHI || __instance.Owner.characterClassManager.CurClass == RoleType.ClassD)
                Respawning.GameplayTickets.Singleton.HandleItemTickets(__instance.OwnerInventory.CurInstance);

            __instance.OwnerInventory.NetworkCurItem = InventorySystem.Items.ItemIdentifier.None;
            __instance.OwnerInventory.CurInstance = null;

            if (!__instance._alreadyActivated)
                __instance.ActivateEffects();

            __instance.ServerRemoveSelf();
            return false;
        }
    }
}
