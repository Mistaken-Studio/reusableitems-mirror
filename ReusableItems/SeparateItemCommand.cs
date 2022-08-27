// -----------------------------------------------------------------------
// <copyright file="SeparateItemCommand.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Mistaken.API.Commands;
using UnityEngine;

namespace Mistaken.ReusableItems
{
    [CommandSystem.CommandHandler(typeof(CommandSystem.ClientCommandHandler))]
    internal class SeparateItemCommand : IBetterCommand
    {
        public override string Command => "separateitem";

        public override string[] Aliases => new string[] { "separate" };

        public override string[] Execute(ICommandSender sender, string[] args, out bool success)
        {
            var player = Player.Get(sender);
            success = false;
            var heldItem = player.CurrentItem;

            ReusableItemsHandler.ReusableItemData data;
            if (heldItem == null)
                return new string[] { "Musisz trzymać przedmiot, który można rodzielić" };
            else if (!ReusableItemsHandler.ReusableItems.TryGetValue(heldItem.Serial, out data))
                return new string[] { "Tego przedmiotu nie można rodzielić" };
            else if (data.UsesLeft < 2)
                return new string[] { "Przedmiot posiada zbyt małą ilość użyć by móc go rozdzielić" };

            ReusableItemsHandler.DefaultReusableItems.TryGetValue(heldItem.Type, out var defaultValue);
            for (int i = data.UsesLeft; i > 1; i--)
            {
                var item = Item.Create(heldItem.Type);
                var pickup = item.Spawn(player.Position + Vector3.up);
                ReusableItemsHandler.ReusableItems.Add(item.Serial, new ReusableItemsHandler.ReusableItemData(item as Usable, 1, defaultValue.Cooldown, defaultValue.MaxItems));
                ReusableItemsHandler.ReusableItems.Add(pickup.Serial, new ReusableItemsHandler.ReusableItemData(null, 1, defaultValue.Cooldown, defaultValue.MaxItems));
                data.UsesLeft--;
            }

            success = true;
            return new string[] { "Done" };
        }
    }
}
