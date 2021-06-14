using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using RetroSharp;

// This application is based on the text adventure in TextAdventure. But instead of programming all details,
// a more general approach has been used, placing all data and game logic in a separare XML file.
//
// The application introduces:
// * XML
// * Schema validation

namespace TextAdventure2
{
    [Characters(80, 30, System.Drawing.KnownColor.White, System.Drawing.KnownColor.Black)]
    [ScreenBorder(30, 20, System.Drawing.KnownColor.Gray)]
    class Program : RetroApplication
    {
        static XmlSchema Schema;
        static XmlDocument Doc;
        static XmlNamespaceManager Mgr;
        static readonly Dictionary<string, KeyValuePair<string, string>> VisibleObjects = new Dictionary<string, KeyValuePair<string, string>>();
        static readonly Dictionary<string, bool> Visited = new Dictionary<string, bool>();
        static readonly Dictionary<string, bool> Flags = new Dictionary<string, bool>();
        static readonly Dictionary<string, bool> Inventory = new Dictionary<string, bool>();
        static readonly Dictionary<string, string> ObjectLocation = new Dictionary<string, string>();
        static readonly Dictionary<string, List<string>> ObjectsPerLocation = new Dictionary<string, List<string>>();
        static readonly Dictionary<string, string> DynamicExits = new Dictionary<string, string>();
        static string CurrentLocation;
        static List<string> StringList;
        static KeyValuePair<string, string> Pair;
        static XmlElement CurrentLocationElement;
        static XmlElement E;
        static string s2;
        static bool Done = false;
        static bool GameOver = false;
        static bool Found;
        static string Verb;
        static string Noun;
        static int i, j;
        static string s;

        public static void Main(string[] _)
        {
            Initialize();

            try
            {
                Schema = LoadXmlSchemaFile("Adventure.xsd");
                Doc = LoadXmlFile("Adventure.xml", Schema);

                Mgr = new XmlNamespaceManager(Doc.NameTable);
                Mgr.AddNamespace("adv", "http://tempuri.org/Adventure.xsd");

                while (!Done)
                {
                    s = Doc.SelectSingleNode("/adv:Game/adv:Introduction", Mgr).InnerText;

                    foreach (string Row in MultilineString(s))
                        WriteLineWordWrap(Row);

                    CurrentLocation = Doc.SelectSingleNode("/adv:Game/@startLocation", Mgr).Value;

                    while (!Done && !GameOver)
                    {
                        WriteLine();

                        CurrentLocationElement = (XmlElement)Doc.SelectSingleNode("/adv:Game/adv:Locations/adv:Location[@id='" + CurrentLocation + "']", Mgr);

                        if (Visited.ContainsKey(CurrentLocation))
                            E = (XmlElement)CurrentLocationElement.SelectSingleNode("adv:ShortDescription", Mgr);
                        else
                        {
                            E = (XmlElement)CurrentLocationElement.SelectSingleNode("adv:LongDescription", Mgr);
                            Visited[CurrentLocation] = true;
                        }

                        ForegroundColor = Color.White;
                        WriteLineWordWrap(JoinMultilineString(MultilineString(E.InnerText)));

                        VisibleObjects.Clear();

                        foreach (XmlElement Object in CurrentLocationElement.SelectNodes("adv:Object", Mgr))
                        {
                            s = Object.GetAttribute("ref");

                            if (ObjectLocation.TryGetValue(s, out s2) && s2 != CurrentLocation)
                                continue;

                            E = (XmlElement)Doc.SelectSingleNode("/adv:Game/adv:Objects/adv:Object[@id='" + s + "']", Mgr);
                            s2 = E.GetAttribute("noun");
                            VisibleObjects[s2] = new KeyValuePair<string, string>(s, E.SelectSingleNode("adv:LongDescription", Mgr).InnerText);
                        }

                        if (ObjectsPerLocation.TryGetValue(CurrentLocation, out StringList))
                        {
                            foreach (string ID in StringList)
                            {
                                E = (XmlElement)Doc.SelectSingleNode("/adv:Game/adv:Objects/adv:Object[@id='" + ID + "']", Mgr);
                                s = E.GetAttribute("noun");

                                VisibleObjects[s] = new KeyValuePair<string, string>(ID, E.SelectSingleNode("adv:LongDescription", Mgr).InnerText);
                            }
                        }

                        if (VisibleObjects.Count > 0)
                        {
                            ForegroundColor = Color.Yellow;

                            WriteLine();
                            WriteLine("You also see:");
                            WriteLine();

                            foreach (KeyValuePair<string, string> P in VisibleObjects.Values)
                                WriteLineWordWrap(JoinMultilineString(MultilineString(P.Value)));
                        }

                        ForegroundColor = Color.White;
                        WriteLine();
                        Write("> ");

                        ForegroundColor = Color.Cyan;
                        s = ReadLine();
                        WriteLine();

                        i = s.IndexOf(' ');
                        if (i < 0)
                        {
                            Verb = s.ToUpper();
                            Noun = string.Empty;
                        }
                        else
                        {
                            Verb = s.Substring(0, i).ToUpper();
                            Noun = s.Substring(i + 1).Trim().ToUpper();
                        }

                        if (string.IsNullOrEmpty(Noun))
                        {
                            switch (Verb)
                            {
                                case "NORTH":
                                case "EAST":
                                case "SOUTH":
                                case "WEST":
                                case "DOWN":
                                case "UP":
                                    Noun = Verb;
                                    Verb = "GO";
                                    break;

                                case "N":
                                    Noun = "NORTH";
                                    Verb = "GO";
                                    break;

                                case "E":
                                    Noun = "EAST";
                                    Verb = "GO";
                                    break;

                                case "S":
                                    Noun = "SOUTH";
                                    Verb = "GO";
                                    break;

                                case "W":
                                    Noun = "WEST";
                                    Verb = "GO";
                                    break;

                                case "U":
                                    Noun = "UP";
                                    Verb = "GO";
                                    break;

                                case "D":
                                    Noun = "DOWN";
                                    Verb = "GO";
                                    break;

                                case "EX":
                                case "EXITS":
                                case "I":
                                case "INV":
                                case "INVENTORY":
                                    Noun = Verb;
                                    Verb = "SHOW";
                                    break;
                            }
                        }

                        ForegroundColor = Color.White;

                        if (ProcessActions(CurrentLocationElement.SelectNodes("adv:Action[@verb='" + Verb + "' and (@noun='" + Noun + "' or not(@noun))]", Mgr)))
                            continue;

                        if (ProcessActions(Doc.SelectNodes("/adv:Game/adv:GlobalActions/adv:Action[@verb='" + Verb + "' and (@noun='" + Noun + "' or not(@noun))]", Mgr)))
                            continue;

                        switch (Verb)
                        {
                            case "GO":
                                switch (Noun)
                                {
                                    case "NORTH":
                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|north", out s))
                                        {
                                            if (CurrentLocationElement.HasAttribute("north"))
                                                s = CurrentLocationElement.GetAttribute("north");
                                            else
                                                s = null;
                                        }

                                        if (s is null)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot go north from this location.");
                                        }
                                        else
                                        {
                                            CurrentLocation = s;
                                            ForegroundColor = Color.LightGreen;
                                            WriteLineWordWrap("You go north.");
                                        }
                                        continue;

                                    case "EAST":
                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|east", out s))
                                        {
                                            if (CurrentLocationElement.HasAttribute("east"))
                                                s = CurrentLocationElement.GetAttribute("east");
                                            else
                                                s = null;
                                        }

                                        if (s is null)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot go east from this location.");
                                        }
                                        else
                                        {
                                            CurrentLocation = s;
                                            ForegroundColor = Color.LightGreen;
                                            WriteLineWordWrap("You go east.");
                                        }
                                        continue;

                                    case "SOUTH":
                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|south", out s))
                                        {
                                            if (CurrentLocationElement.HasAttribute("south"))
                                                s = CurrentLocationElement.GetAttribute("south");
                                            else
                                                s = null;
                                        }

                                        if (s is null)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot go south from this location.");
                                        }
                                        else
                                        {
                                            CurrentLocation = s;
                                            ForegroundColor = Color.LightGreen;
                                            WriteLineWordWrap("You go south.");
                                        }
                                        continue;

                                    case "WEST":
                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|west", out s))
                                        {
                                            if (CurrentLocationElement.HasAttribute("west"))
                                                s = CurrentLocationElement.GetAttribute("west");
                                            else
                                                s = null;
                                        }

                                        if (s is null)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot go west from this location.");
                                        }
                                        else
                                        {
                                            CurrentLocation = s;
                                            ForegroundColor = Color.LightGreen;
                                            WriteLineWordWrap("You go west.");
                                        }
                                        continue;

                                    case "UP":
                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|up", out s))
                                        {
                                            if (CurrentLocationElement.HasAttribute("up"))
                                                s = CurrentLocationElement.GetAttribute("up");
                                            else
                                                s = null;
                                        }

                                        if (s is null)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot go up from this location.");
                                        }
                                        else
                                        {
                                            CurrentLocation = s;
                                            ForegroundColor = Color.LightGreen;
                                            WriteLineWordWrap("You go up.");
                                        }
                                        continue;

                                    case "DOWN":
                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|down", out s))
                                        {
                                            if (CurrentLocationElement.HasAttribute("down"))
                                                s = CurrentLocationElement.GetAttribute("down");
                                            else
                                                s = null;
                                        }

                                        if (s is null)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot go down from this location.");
                                        }
                                        else
                                        {
                                            CurrentLocation = s;
                                            ForegroundColor = Color.LightGreen;
                                            WriteLineWordWrap("You go down.");
                                        }
                                        continue;

                                    default:
                                        ForegroundColor = Color.Salmon;
                                        WriteLineWordWrap("Unsure how to go in that direction.");
                                        continue;
                                }

                            case "TAKE":
                                if (!VisibleObjects.TryGetValue(Noun, out Pair))
                                {
                                    ForegroundColor = Color.Salmon;
                                    WriteLineWordWrap("I cannot see that here.");
                                }
                                else if ((E = (XmlElement)Doc.SelectSingleNode("/adv:Game/adv:Objects/adv:Object[@id='" + Pair.Key + "']", Mgr)).GetAttribute("canBeCarried") != "true")
                                {
                                    ForegroundColor = Color.Salmon;
                                    WriteLineWordWrap("You cannot carry the " + E.SelectSingleNode("adv:ShortDescription", Mgr).InnerText.ToLower() + ".");
                                }
                                else
                                {
                                    Inventory[Pair.Key] = true;
                                    ObjectLocation[Pair.Key] = null;    // null = inventory

                                    if (ObjectsPerLocation.TryGetValue(CurrentLocation, out StringList))
                                        StringList.Remove(Pair.Key);

                                    ForegroundColor = Color.LightGreen;
                                    WriteLineWordWrap("You take the " + E.SelectSingleNode("adv:ShortDescription", Mgr).InnerText.ToLower() + ".");
                                }
                                continue;

                            case "DROP":

                                Found = false;

                                foreach (string ID in Inventory.Keys)
                                {
                                    s = Doc.SelectSingleNode("/adv:Game/adv:Objects/adv:Object[@id='" + ID + "']/@noun", Mgr).Value;
                                    if (s == Noun)
                                    {
                                        Inventory.Remove(ID);
                                        ObjectLocation[ID] = CurrentLocation;

                                        if (!ObjectsPerLocation.TryGetValue(CurrentLocation, out StringList))
                                        {
                                            StringList = new List<string>();
                                            ObjectsPerLocation[CurrentLocation] = StringList;
                                        }

                                        StringList.Add(ID);

                                        ForegroundColor = Color.LightGreen;
                                        WriteLineWordWrap("You drop the " + Doc.SelectSingleNode("/adv:Game/adv:Objects/adv:Object[@id='" + ID + "']/adv:ShortDescription", Mgr).InnerText.ToLower() + ".");

                                        Found = true;
                                        break;
                                    }
                                }

                                if (!Found)
                                {
                                    ForegroundColor = Color.Salmon;
                                    WriteLineWordWrap("You cannot drop anything you don't have.");
                                }
                                continue;

                            case "SHOW":
                                switch (Noun)
                                {
                                    case "EX":
                                    case "EXITS":
                                        StringList = new List<string>();

                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|north", out s) && CurrentLocationElement.HasAttribute("north"))
                                            s = CurrentLocationElement.GetAttribute("north");

                                        if (!(s is null))
                                            StringList.Add("north");

                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|east", out s) && CurrentLocationElement.HasAttribute("east"))
                                            s = CurrentLocationElement.GetAttribute("east");

                                        if (!(s is null))
                                            StringList.Add("east");

                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|south", out s) && CurrentLocationElement.HasAttribute("south"))
                                            s = CurrentLocationElement.GetAttribute("south");

                                        if (!(s is null))
                                            StringList.Add("south");

                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|west", out s) && CurrentLocationElement.HasAttribute("west"))
                                            s = CurrentLocationElement.GetAttribute("west");

                                        if (!(s is null))
                                            StringList.Add("west");

                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|up", out s) && CurrentLocationElement.HasAttribute("up"))
                                            s = CurrentLocationElement.GetAttribute("up");

                                        if (!(s is null))
                                            StringList.Add("up");

                                        if (!DynamicExits.TryGetValue(CurrentLocation + "|down", out s) && CurrentLocationElement.HasAttribute("down"))
                                            s = CurrentLocationElement.GetAttribute("down");

                                        if (!(s is null))
                                            StringList.Add("down");

                                        if (StringList.Count == 0)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("You cannot see any exits from this location.");
                                        }
                                        else
                                        {
                                            ForegroundColor = Color.Yellow;
                                            WriteWordWrap("You can go ");
                                            WriteList(StringList);
                                        }
                                        continue;

                                    case "I":
                                    case "INV":
                                    case "INVENTORY":
                                        if (Inventory.Count == 0)
                                        {
                                            ForegroundColor = Color.Salmon;
                                            WriteLineWordWrap("Your inventory is empty.");
                                        }
                                        else
                                        {
                                            ForegroundColor = Color.Yellow;
                                            WriteLineWordWrap("Your inventory contains:");
                                            WriteLine();

                                            foreach (string ID in Inventory.Keys)
                                                WriteLineWordWrap(Doc.SelectSingleNode("/adv:Game/adv:Objects/adv:Object[@id='" + ID + "']/adv:LongDescription", Mgr).InnerText.ToLower());
                                        }
                                        continue;
                                }
                                break;

                            case "L":
                            case "LOOK":
                                Visited.Remove(CurrentLocation);
                                continue;

                            case "SAVE":
                                try
                                {
                                    using (FileStream fs = File.Create(Noun + ".sav"))
                                    {
                                        using (BinaryWriter w = new BinaryWriter(fs))
                                        {
                                            w.Write(0);     // Version
                                            w.Write(CurrentLocation);

                                            w.Write(Visited.Count);
                                            foreach (KeyValuePair<string, bool> Rec in Visited)
                                                w.Write(Rec.Key);

                                            w.Write(Flags.Count);
                                            foreach (KeyValuePair<string, bool> Rec in Flags)
                                                w.Write(Rec.Key);

                                            w.Write(Inventory.Count);
                                            foreach (KeyValuePair<string, bool> Rec in Inventory)
                                                w.Write(Rec.Key);

                                            w.Write(ObjectLocation.Count);
                                            foreach (KeyValuePair<string, string> Rec in ObjectLocation)
                                            {
                                                w.Write(Rec.Key);

                                                if (Rec.Value is null)
                                                    w.Write(false);
                                                else
                                                {
                                                    w.Write(true);
                                                    w.Write(Rec.Value);
                                                }
                                            }

                                            w.Write(ObjectsPerLocation.Count);
                                            foreach (KeyValuePair<string, List<string>> Rec in ObjectsPerLocation)
                                            {
                                                w.Write(Rec.Key);
                                                w.Write(Rec.Value.Count);

                                                foreach (string Object in Rec.Value)
                                                    w.Write(Object);
                                            }

                                            w.Write(DynamicExits.Count);
                                            foreach (KeyValuePair<string, string> Rec in DynamicExits)
                                            {
                                                w.Write(Rec.Key);
                                                w.Write(Rec.Value);
                                            }
                                        }
                                    }

                                    ForegroundColor = Color.LightGreen;
                                    WriteLineWordWrap("Game successfully saved to " + Noun + ".sav");
                                }
                                catch (Exception ex)
                                {
                                    ForegroundColor = Color.Salmon;
                                    WriteLineWordWrap("Unable to save the game to the file " + Noun + ".sav");
                                    WriteLine();
                                    WriteLineWordWrap("The following error was reported:");
                                    WriteLine();
                                    WriteLineWordWrap(ex.Message);
                                }
                                continue;

                            case "LOAD":
                                try
                                {
                                    using (FileStream fs = File.OpenRead(Noun + ".sav"))
                                    {
                                        using (BinaryReader r = new BinaryReader(fs))
                                        {
                                            int Version = r.ReadInt32();
                                            if (Version != 0)
                                                throw new Exception("Unsupported file version.");

                                            CurrentLocation = r.ReadString();

                                            Visited.Clear();
                                            Flags.Clear();
                                            Inventory.Clear();
                                            ObjectLocation.Clear();
                                            ObjectsPerLocation.Clear();
                                            DynamicExits.Clear();

                                            i = r.ReadInt32();
                                            while (i-- > 0)
                                                Visited[r.ReadString()] = true;

                                            i = r.ReadInt32();
                                            while (i-- > 0)
                                                Flags[r.ReadString()] = true;

                                            i = r.ReadInt32();
                                            while (i-- > 0)
                                                Inventory[r.ReadString()] = true;

                                            i = r.ReadInt32();
                                            while (i-- > 0)
                                            {
                                                s = r.ReadString();

                                                if (r.ReadBoolean())
                                                    s2 = r.ReadString();
                                                else
                                                    s2 = null;

                                                ObjectLocation[s] = s2;
                                            }

                                            i = r.ReadInt32();
                                            while (i-- > 0)
                                            {
                                                s = r.ReadString();

                                                j = r.ReadInt32();
                                                StringList = new List<string>();
                                                while (j-- > 0)
                                                    StringList.Add(r.ReadString());

                                                ObjectsPerLocation[s] = StringList;
                                            }

                                            i = r.ReadInt32();
                                            while (i-- > 0)
                                            {
                                                s = r.ReadString();
                                                s2 = r.ReadString();

                                                DynamicExits[s] = s2;
                                            }
                                        }
                                    }

                                    ForegroundColor = Color.LightGreen;
                                    WriteLineWordWrap("Game successfully loaded from " + Noun + ".sav");
                                }
                                catch (Exception ex)
                                {
                                    ForegroundColor = Color.Salmon;
                                    WriteLineWordWrap("Unable to load the game from the file " + Noun + ".sav");
                                    WriteLine();
                                    WriteLineWordWrap("The following error was reported:");
                                    WriteLine();
                                    WriteLineWordWrap(ex.Message);
                                }
                                continue;

                            case "HELP":
                                // TODO
                                break;

                            case "Q":
                            case "QUIT":
                                Done = true;
                                continue;
                        }

                        ForegroundColor = Color.Salmon;
                        WriteLineWordWrap("Unsure how to do that.");
                    }

                    if (GameOver)
                    {
                        ForegroundColor = Color.Salmon;
                        WriteLine();
                        WriteLineWordWrap("Game Over!");

                        ForegroundColor = Color.White;
                        WriteLine();
                        WriteLineWordWrap("Press ENTER to try again.");

                        ReadLine();

                        VisibleObjects.Clear();
                        Visited.Clear();
                        Flags.Clear();
                        Inventory.Clear();
                        ObjectLocation.Clear();
                        ObjectsPerLocation.Clear();
                        DynamicExits.Clear();

                        GameOver = false;
                    }
                }

                ForegroundColor = Color.LightBlue;
                WriteLineWordWrap("Goodbye.");
            }
            catch (CtrlCException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                WriteLineWordWrap(ex.Message);
                WriteLine();
                WriteLine(ex.StackTrace);
                WriteLine();
                WriteLine("Press ENTER to close application.");
                ReadLine();
            }

            Terminate();
        }

        static bool ProcessActions(XmlNodeList ActionList)
        {
            LinkedList<XmlElement> Actions = new LinkedList<XmlElement>();
            bool Found = false;

            foreach (XmlElement Action in ActionList)
            {
                foreach (XmlElement Command in Action.SelectNodes("child::*", Mgr))
                    Actions.AddLast(Command);

                while (!(Actions.First is null))
                {
                    E = Actions.First.Value;
                    Actions.RemoveFirst();

                    switch (E.LocalName)
                    {
                        case "IfFlag":
                            s = E.GetAttribute("name");
                            if (!Flags.ContainsKey(s))
                                break;

                            foreach (XmlElement Command in E.SelectNodes("child::*", Mgr))
                                Actions.AddLast(Command);

                            break;

                        case "IfNotFlag":
                            s = E.GetAttribute("name");
                            if (Flags.ContainsKey(s))
                                break;

                            foreach (XmlElement Command in E.SelectNodes("child::*", Mgr))
                                Actions.AddLast(Command);

                            break;

                        case "IfObjectInInventory":
                            s = E.GetAttribute("ref");
                            if (!Inventory.ContainsKey(s))
                                break;

                            foreach (XmlElement Command in E.SelectNodes("child::*", Mgr))
                                Actions.AddLast(Command);

                            break;

                        case "IfObjectNotInInventory":
                            s = E.GetAttribute("ref");
                            if (Inventory.ContainsKey(s))
                                break;

                            foreach (XmlElement Command in E.SelectNodes("child::*", Mgr))
                                Actions.AddLast(Command);

                            break;

                        case "Text":
                            s = E.InnerText;
                            ForegroundColor = Color.LightGreen;
                            WriteLineWordWrap(JoinMultilineString(MultilineString(s)));
                            Found = true;
                            break;

                        case "ChangeLocation":
                            CurrentLocation = E.GetAttribute("newLocation");
                            Found = true;
                            break;

                        case "SetFlag":
                            s = E.GetAttribute("name");
                            Flags[s] = true;
                            Found = true;
                            break;

                        case "ClearFlag":
                            s = E.GetAttribute("name");
                            Flags.Remove(s);
                            Found = true;
                            break;

                        case "AddExit":
                            if (E.HasAttribute("from"))
                                s = E.GetAttribute("from");
                            else
                                s = CurrentLocation;

                            DynamicExits[s + "|" + E.GetAttribute("direction")] = E.GetAttribute("to");
                            Found = true;
                            break;

                        case "RemoveExit":
                            if (E.HasAttribute("from"))
                                s = E.GetAttribute("from");
                            else
                                s = CurrentLocation;

                            DynamicExits[s + "|" + E.GetAttribute("direction")] = null;
                            Found = true;
                            break;

                        case "TakeObject":
                            s = E.GetAttribute("ref");

                            if (VisibleObjects.ContainsKey(s))
                            {
                                Inventory[s] = true;
                                ObjectLocation[s] = null;    // null = inventory

                                if (ObjectsPerLocation.TryGetValue(CurrentLocation, out StringList))
                                    StringList.Remove(s);
                            }

                            Found = true;
                            break;

                        case "DropObject":
                            s = E.GetAttribute("ref");

                            if (Inventory.ContainsKey(s))
                            {
                                Inventory.Remove(s);
                                ObjectLocation[s] = CurrentLocation;

                                if (!ObjectsPerLocation.TryGetValue(CurrentLocation, out StringList))
                                {
                                    StringList = new List<string>();
                                    ObjectsPerLocation[CurrentLocation] = StringList;
                                }

                                StringList.Add(s);
                            }

                            Found = true;
                            break;

                        case "AddObjectToInventory":
                            s = E.GetAttribute("ref");

                            Inventory[s] = true;

                            if (ObjectLocation.TryGetValue(s, out s2) && !(s2 is null) && ObjectsPerLocation.TryGetValue(s2, out StringList))
                                StringList.Remove(s);

                            ObjectLocation[s] = null;    // null = inventory

                            Found = true;
                            break;

                        case "AddObjectToLocation":
                            s = E.GetAttribute("ref");

                            if (ObjectLocation.TryGetValue(s, out s2) && !(s2 is null) && ObjectsPerLocation.TryGetValue(s2, out StringList))
                                StringList.Remove(s);

                            if (E.HasAttribute("location"))
                                s2 = E.GetAttribute("location");
                            else
                                s2 = CurrentLocation;

                            Inventory.Remove(s);
                            ObjectLocation[s] = s2;

                            if (!ObjectsPerLocation.TryGetValue(s2, out StringList))
                            {
                                StringList = new List<string>();
                                ObjectsPerLocation[CurrentLocation] = StringList;
                            }

                            StringList.Add(s);

                            Found = true;
                            break;

                        case "RemoveObjectFromInventory":
                            s = E.GetAttribute("ref");

                            Inventory.Remove(s);

                            Found = true;
                            break;

                        case "RemoveObjectFromLocation":
                            s = E.GetAttribute("ref");

                            if (ObjectLocation.TryGetValue(s, out s2) && !(s2 is null) && ObjectsPerLocation.TryGetValue(s2, out StringList))
                                StringList.Remove(s);

                            ObjectLocation[s] = null;

                            Found = true;
                            break;

                        case "GameOver":
                            GameOver = true;
                            Found = true;
                            break;
                    }
                }
            }

            return Found;
        }

        static string[] MultilineString(string s)
        {
            string[] Result = s.Split(CRLF, StringSplitOptions.None);
            int i, c = Result.Length;

            for (i = 0; i < c; i++)
                Result[i] = Result[i].Trim();

            return Result;
        }

        static string JoinMultilineString(string[] s)
        {
            StringBuilder sb = null;

            foreach (string s2 in s)
            {
                if (sb is null)
                    sb = new StringBuilder();
                else
                    sb.Append(" ");

                sb.Append(s2.Trim());
            }

            if (sb is null)
                return string.Empty;
            else
                return sb.ToString();
        }

        static readonly string[] CRLF = new string[] { "\r\n" };

        static void WriteList(IEnumerable<string> List)
        {
            int c = 0;

            foreach (string s in List)
                c++;

            foreach (string s in List)
            {
                c--;
                WriteWordWrap(s);

                if (c == 1)
                    WriteWordWrap(" and ");
                else if (c > 1)
                    WriteWordWrap(", ");
            }
        }
    }
}