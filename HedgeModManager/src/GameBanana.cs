﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace HedgeModManager
{
    public static class GameBanana
    {

        public class GameBananaItemSubmittion
        {
            public string Name, Description, UserName, Credits, ThumbURL;

            public static string GetResponseFromGameBanana(string itemType, string ItemID)
            {
                return new WebClient().DownloadString($"https://api.gamebanana.com/Core/Item/Data?itemtype={itemType}&itemid={ItemID}&fields=name,description,text,Owner().name,Credits().aAuthors(),Preview().sStructuredDataFullsizeUrl()&format=xml");
            }

            public static GameBananaItemSubmittion ReadResponse(string s)
            {
                try
                {
                    var item = new GameBananaItemSubmittion();
                    var xml = XDocument.Parse(s);
                    var values = xml.Root.Elements("value").ToList();
                    item.Name = values[0].Value;
                    item.Description = values[1].Value;
                    item.Description += values[2].Value;
                    item.UserName = values[3].Value;
                    item.ThumbURL = values[4].Value;
                    var credits = xml.Root.Elements("valueset").ToList()[0];
                    foreach (var values2 in credits.Elements("valueset"))
                    {
                        item.Credits += "- " + values2.Elements("value").ToList()[0].Value + "\n";
                        item.Credits += "    " + values2.Elements("value").ToList()[1].Value + "\n";
                    }
                    return item;
                }catch (Exception ex)
                {
                    try
                    {
                        var xml = XDocument.Parse(s);
                        MainForm.AddMessage2("A gbAPI error has occured! Please See \"HedgeModManager.log\" for more info," +
                            " Error: " + xml.Root.Elements("value").ToList()[0].Value, ex, s, xml.Root.Elements("value").ToList()[1].Value);
                    }
                    catch
                    {
                        MainForm.AddMessage("Exception thrown while Parsing a gbAPI error.", ex, s);
                    }
                }
                return null;
            }
        }

        public class GameBananaItemMember
        {
            public string Name, AvatarURL;

            public static string GetResponseFromGameBanana(string UserID)
            {
                return new WebClient().DownloadString($"https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid={UserID}&fields=name,Url().sGetAvatarUrl()&format=xml");
            }

            public static GameBananaItemMember ReadResponse(string s)
            {
                try
                { 
                    var item = new GameBananaItemMember();
                    var xml = XDocument.Parse(s);
                    var values = xml.Root.Elements("value").ToList();
                    item.Name = values[0].Value;
                    item.AvatarURL = values[1].Value;
                    return item;
                }
                catch (Exception ex)
                {
                    try
                    {
                        var xml = XDocument.Parse(s);
                        MainForm.AddMessage2("A gbAPI error has occured! Please See \"HedgeModManager.log\" for more info," +
                            " Error: " + xml.Root.Elements("value").ToList()[0].Value, ex, s, xml.Root.Elements("value").ToList()[1].Value);
                    }
                    catch
                    {
                        MainForm.AddMessage("Exception thrown while Parsing a gbAPI error.", ex, s);
                    }
                }
                return null;

            }
        }
    }
}
