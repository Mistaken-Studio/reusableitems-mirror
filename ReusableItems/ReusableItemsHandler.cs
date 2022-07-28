// -----------------------------------------------------------------------
// <copyright file="ReusableItemsHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using MEC;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;

namespace Mistaken.ReusableItems
{
    internal class ReusableItemsHandler : Module
    {
        public ReusableItemsHandler(PluginHandler p)
            : base(p)
        {
        }

        public override string Name => "ReusableItems";

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Player.UsingItem += this.Player_UsingItem;
            Exiled.Events.Handlers.Player.ChangingItem += this.Player_ChangingItem;
            Events.Handlers.CustomEvents.RequestPickItem += this.CustomEvents_RequestPickItem;
            Exiled.Events.Handlers.Player.PickingUpItem += this.Player_PickingUpItem;
            Exiled.Events.Handlers.Player.ChangingRole += this.Player_ChangingRole;
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Player.UsingItem -= this.Player_UsingItem;
            Exiled.Events.Handlers.Player.ChangingItem -= this.Player_ChangingItem;
            Events.Handlers.CustomEvents.RequestPickItem -= this.CustomEvents_RequestPickItem;
            Exiled.Events.Handlers.Player.PickingUpItem -= this.Player_PickingUpItem;
            Exiled.Events.Handlers.Player.ChangingRole -= this.Player_ChangingRole;
        }

        internal static readonly Dictionary<ushort, ReusableItemData> ReusableItems = new Dictionary<ushort, ReusableItemData>();
        internal static readonly Dictionary<ItemType, ReusableItem> DefaultReusableItems = new Dictionary<ItemType, ReusableItem>()
        {
            {
                ItemType.Medkit,
                new ReusableItem
                {
                    Type = ItemType.Medkit,
                    StartUses = 1,
                    Uses = 2,
                    MaxItems = 2,
                    Cooldown = 10,
                }
            },
            {
                ItemType.Painkillers,
                new ReusableItem
                {
                    Type = ItemType.Painkillers,
                    StartUses = 3,
                    Uses = 3,
                    MaxItems = 1,
                    Cooldown = 8,
                }
            },
            {
                ItemType.SCP500,
                new ReusableItem
                {
                    Type = ItemType.SCP500,
                    StartUses = 1,
                    Uses = 1,
                    MaxItems = 2,
                }
            },
            {
                ItemType.SCP207,
                new ReusableItem
                {
                    Type = ItemType.SCP207,
                    StartUses = 1,
                    Uses = 2,
                    MaxItems = 1,
                    Cooldown = 30,
                }
            },
        };

        internal struct ReusableItem
        {
            public ItemType Type;
            public byte Uses;
            public byte StartUses;
            public byte MaxItems;
            public float Cooldown;
        }

        internal class ReusableItemData
        {
            public byte UsesLeft { get; set; }

            public Usable Item { get; internal set; }

            public float Cooldown { get; private set; }

            public byte MaxItems { get; private set; }

            internal ReusableItemData(Usable item, byte usesLeft, float cooldown, byte maxItems)
            {
                this.Item = item;
                this.UsesLeft = usesLeft;
                this.Cooldown = cooldown;
                this.MaxItems = maxItems;
            }
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            foreach (var defaultReusableItem in DefaultReusableItems)
            {
                foreach (var item in ev.Player.Items.Where(x => x.Type == defaultReusableItem.Key).ToArray())
                {
                    if (!ReusableItems.ContainsKey(item.Serial))
                        ReusableItems[item.Serial] = new ReusableItemData(item as Usable, defaultReusableItem.Value.StartUses, defaultReusableItem.Value.Cooldown, defaultReusableItem.Value.MaxItems);
                }
            }
        }

        private void Player_PickingUpItem(Exiled.Events.EventArgs.PickingUpItemEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (!DefaultReusableItems.TryGetValue(ev.Pickup.Type, out var defaultValue))
                return;

            if (!ReusableItems.TryGetValue(ev.Pickup.Serial, out var reusable))
            {
                reusable = new ReusableItemData(null, defaultValue.StartUses, defaultValue.Cooldown, defaultValue.MaxItems);
                ReusableItems[ev.Pickup.Serial] = reusable;
            }

            foreach (var item in ev.Player.Items.Where(x => x.Type == ev.Pickup.Type).ToArray())
            {
                if (!ReusableItems.TryGetValue(item.Serial, out var reusable2))
                {
                    reusable2 = new ReusableItemData(item as Usable, defaultValue.StartUses, defaultValue.Cooldown, defaultValue.MaxItems);
                    ReusableItems[item.Serial] = reusable2;
                }

                var diff = defaultValue.Uses - reusable2.UsesLeft;
                if (diff <= 0)
                    continue;

                var toAdd = (byte)Math.Min(diff, reusable.UsesLeft);
                reusable2.UsesLeft += toAdd;
                reusable.UsesLeft -= toAdd;
                if (reusable.UsesLeft == 0)
                {
                    ev.Pickup.Destroy();
                    ev.IsAllowed = false;
                    return;
                }
            }

            var count = ev.Player.CountItem(ev.Pickup.Type);
            if (count >= reusable.MaxItems)
            {
                ev.Player.SetGUI("reusablePickup", PseudoGUIPosition.BOTTOM, $"<b>Already</b> reached the limit of <color=yellow>{ev.Pickup.Type}</color> (<color=yellow>{defaultValue.MaxItems} {ev.Pickup.Type}</color>)", 2);
                ev.IsAllowed = false;
                return;
            }

            ev.Player.SetGUI("reusablePickup", PseudoGUIPosition.BOTTOM, $"<color=yellow>{reusable.UsesLeft}</color>/<color=yellow>{defaultValue.Uses}</color> uses left", 2);
        }

        private void CustomEvents_RequestPickItem(Events.EventArgs.PickItemRequestEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;

            if (!DefaultReusableItems.TryGetValue(ev.Pickup.Type, out var defaultValue))
                return;

            if (!ReusableItems.TryGetValue(ev.Pickup.Serial, out var reusable))
                reusable = new ReusableItemData(null, defaultValue.StartUses, defaultValue.Cooldown, defaultValue.MaxItems);

            foreach (var item in ev.Player.Items.Where(x => x.Type == ev.Pickup.Type).ToArray())
            {
                if (!ReusableItems.TryGetValue(item.Serial, out var reusable2))
                {
                    reusable2 = new ReusableItemData(item as Usable, defaultValue.StartUses, defaultValue.Cooldown, defaultValue.MaxItems);
                    ReusableItems[item.Serial] = reusable2;
                }

                var diff = defaultValue.Uses - reusable2.UsesLeft;
                if (diff <= 0)
                    continue;
                ev.Player.SetGUI("reusablePickup", PseudoGUIPosition.BOTTOM, $"<color=yellow>{reusable.UsesLeft}</color>/<color=yellow>{defaultValue.Uses}</color> uses left", 2);
                return;
            }

            var count = ev.Player.CountItem(ev.Pickup.Type);
            if (count >= reusable.MaxItems)
            {
                ev.Player.SetGUI("reusablePickup", PseudoGUIPosition.BOTTOM, $"<b>Already</b> reached the limit of <color=yellow>{ev.Pickup.Type}</color> (<color=yellow>{defaultValue.MaxItems} {ev.Pickup.Type}</color>)", 2);
                ev.IsAllowed = false;
                return;
            }

            ev.Player.SetGUI("reusablePickup", PseudoGUIPosition.BOTTOM, $"<color=yellow>{reusable.UsesLeft}</color>/<color=yellow>{defaultValue.Uses}</color> uses left", 2);
        }

        private void Player_ChangingItem(Exiled.Events.EventArgs.ChangingItemEventArgs ev)
        {
            if (ev.NewItem == null)
                return;

            if (!DefaultReusableItems.ContainsKey(ev.NewItem.Type))
                return;

            this.RunCoroutine(this.InformUsesLeft(ev.Player), "InformUsesLeft");
        }

        private IEnumerator<float> InformUsesLeft(Player p)
        {
            yield return Timing.WaitForSeconds(.1f);

            if (p.CurrentItem == null)
                yield break;

            var itemSerial = p.CurrentItem.Serial;

            if (!DefaultReusableItems.TryGetValue(p.CurrentItem.Type, out var defaultValue))
                yield break;

            if (!ReusableItems.TryGetValue(itemSerial, out var reusable))
            {
                reusable = new ReusableItemData(p.CurrentItem as Usable, defaultValue.StartUses, defaultValue.Cooldown, defaultValue.MaxItems);
                ReusableItems[itemSerial] = reusable;
            }
            else if (reusable.Item == null)
                reusable.Item = p.CurrentItem as Usable;

            p.SetGUI("reusable", PseudoGUIPosition.BOTTOM, $"<color=yellow>{reusable.UsesLeft}</color>/<color=yellow>{defaultValue.Uses}</color> uses left");

            do
                yield return Timing.WaitForSeconds(1f);
            while (p.CurrentItem?.Serial == itemSerial);

            p.SetGUI("reusable", PseudoGUIPosition.BOTTOM, null);
        }

        private void Player_UsingItem(Exiled.Events.EventArgs.UsingItemEventArgs ev)
        {
            if (!ReusableItems.TryGetValue(ev.Item.Serial, out var reusable))
            {
                if (DefaultReusableItems.TryGetValue(ev.Item.Type, out var defaultValue))
                    ReusableItems[ev.Item.Serial] = new ReusableItemData(ev.Item, defaultValue.StartUses, defaultValue.Cooldown, defaultValue.MaxItems);
            }
            else if (reusable.Item == null)
               reusable.Item = ev.Item;
        }
    }
}
