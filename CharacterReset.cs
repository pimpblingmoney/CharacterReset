﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using TShockAPI.Hooks;
using Terraria;
using TerrariaApi.Server;

namespace CharacterReset
{
    [ApiVersion(1, 16)]
    public class CharacterReset : TerrariaPlugin
    {
        #region Plugin Info
            public override Version Version
            {
                get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
            }
            public override string Name
            {
                get { return "CharacterReset"; }
            }
            public override string Author
            {
                get { return "Bippity"; }
            }
            public override string Description
            {
                get { return "Reset your character back to default."; }
            }
            public CharacterReset(Main game)
                : base(game)
            {
                Order = 4;
            }
            #endregion

        #region Initialize/Dispose
            public List<string> PlayerList = new List<string>();
            public List<NetItem> StarterItems = new List<NetItem>();
            public int startHealth, startMana;

            public override void Initialize()
            {
                ServerApi.Hooks.NetGetData.Register(this, OnGetData);

                Commands.ChatCommands.Add(new Command(new List<string>() { "characterreset.stats", "characterreset.inventory" }, ResetCharacter, "resetcharacter"));

                if (Main.ServerSideCharacter)
                {
                    StarterItems = TShock.ServerSideCharacterConfig.StartingInventory;

                    if (TShock.ServerSideCharacterConfig.StartingHealth > 500)
                        startHealth = 500;
                    else if (TShock.ServerSideCharacterConfig.StartingHealth < 100)
                        startHealth = 100;
                    else
                        startHealth = TShock.ServerSideCharacterConfig.StartingHealth;

                    if (TShock.ServerSideCharacterConfig.StartingMana > 200)
                        startMana = 200;
                    else if (TShock.ServerSideCharacterConfig.StartingMana < 20)
                        startMana = 20;
                    else
                        startMana = TShock.ServerSideCharacterConfig.StartingMana;
                }
            }


            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                }
                base.Dispose(disposing);
            }
            #endregion

        #region CharacterReset
            private void OnGetData(GetDataEventArgs args)
            {
                if (args.MsgID == PacketTypes.TileGetSection)
                {
                    if (Netplay.serverSock[args.Msg.whoAmI].state == 2)
                    {
                        CleanInventory(args.Msg.whoAmI);
                    }
                }
            }

            private void CleanInventory(int who) //original method from ClearInvSSC to prevent exploits
            {
                if (!Main.ServerSideCharacter)
                {
                    Log.ConsoleError("[CharacterReset] This plugin will not work properly with ServerSidedCharacters disabled.");
                }

                if (Main.ServerSideCharacter && !TShock.Players[who].IsLoggedIn)
                {
                    var player = TShock.Players[who];
                    player.TPlayer.SpawnX = -1;
                    player.TPlayer.SpawnY = -1;
                    player.sX = -1;
                    player.sY = -1;

                    ClearInventory(player);
                }
            }

            private void ClearInventory(TSPlayer player) //The inventory clearing method from ClearInvSSC
            {
                for (int i = 0; i < NetItem.maxNetInventory; i++)
                {
                    if (i < NetItem.maxNetInventory - (NetItem.armorSlots + NetItem.dyeSlots)) //main inventory excluding the special slots
                    {
                        player.TPlayer.inventory[i].netDefaults(0);
                    }
                    else if (i < NetItem.maxNetInventory - NetItem.dyeSlots)
                    {
                        var index = i - (NetItem.maxNetInventory - (NetItem.armorSlots + NetItem.dyeSlots));
                        player.TPlayer.armor[index].netDefaults(0);
                    }
                    else
                    {
                        var index = i - (NetItem.maxNetInventory - NetItem.dyeSlots);
                        player.TPlayer.dye[index].netDefaults(0);
                    }
                }

                for (int k = 0; k < NetItem.maxNetInventory; k++)
                {
                    NetMessage.SendData(5, -1, -1, "", player.Index, (float)k, 0f, 0f, 0);
                }

                for (int k = 0; k < Player.maxBuffs; k++)
                {
                    player.TPlayer.buffType[k] = 0;
                }

                NetMessage.SendData(4, -1, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(42, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(16, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(50, -1, -1, "", player.Index, 0f, 0f, 0f, 0);

                for (int k = 0; k < NetItem.maxNetInventory; k++)
                {
                    NetMessage.SendData(5, player.Index, -1, "", player.Index, (float)k, 0f, 0f, 0);
                }

                for (int k = 0; k < Player.maxBuffs; k++)
                {
                    player.TPlayer.buffType[k] = 0;
                }

                NetMessage.SendData(4, player.Index, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(42, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(16, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                NetMessage.SendData(50, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
            }

            private void ResetCharacter(CommandArgs args)
            {
                TSPlayer player = args.Player;
                if (player != null)
                {
                    if (Main.ServerSideCharacter)
                    {
                        if (player.Group.HasPermission("characterreset.*") || player.Group.HasPermission("characterreset.stats")) //resets player's stats
                        {
                            player.TPlayer.statLife = startHealth;
                            player.TPlayer.statLifeMax = startHealth;
                            player.TPlayer.statMana = startMana;
                            player.TPlayer.statManaMax = startMana;

                            NetMessage.SendData(4, -1, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(42, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(16, -1, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(50, -1, -1, "", player.Index, 0f, 0f, 0f, 0);

                            NetMessage.SendData(4, player.Index, -1, player.Name, player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(42, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(16, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                            NetMessage.SendData(50, player.Index, -1, "", player.Index, 0f, 0f, 0f, 0);
                        }

                        if (player.Group.HasPermission("characterreset.*") || player.Group.HasPermission("characterreset.inventory"))
                        {
                            ClearInventory(player);

                            if (Main.ServerSideCharacter)
                            {
                                int slot = 0;
                                Item give;
                                foreach (NetItem item in StarterItems)
                                {
                                    give = TShock.Utils.GetItemById(item.netID);
                                    give.stack = item.stack;
                                    give.prefix = (byte)item.prefix; //does this work...?

                                    if (player.InventorySlotAvailable)
                                    {
                                        player.TPlayer.inventory[slot] = give;
                                        NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, string.Empty, player.Index, slot);
                                        slot++;
                                    }
                                }
                            }
                        }
                        player.SendSuccessMessage("Character reset to default!");
                    }
                }
            }
        #endregion
    }
}
