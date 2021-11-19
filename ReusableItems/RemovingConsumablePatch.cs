// -----------------------------------------------------------------------
// <copyright file="RemovingConsumablePatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

using System;
using Exiled.API.Features;
using InventorySystem.Items.Usables;

namespace Mistaken.ReusableItems
{
    [HarmonyLib.HarmonyPatch(typeof(Consumable), nameof(Consumable.OnRemoved))]
    internal static class RemovingConsumablePatch
    {
        internal static void Postfix(Consumable __instance)
        {
            ReusableItemsHandler.ReusableItems.Remove(__instance.ItemSerial);
        }
    }
}
