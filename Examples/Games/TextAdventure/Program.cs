using System.Collections.Generic;
using System.Drawing;
using RetroSharp;

// This application is a simple implementation of a classic text adventure style game.
//
// The application introduces:
// * Text input and output
// * State machine
// * Simple file access

namespace TextAdventure
{
    [Characters(80, 30, System.Drawing.KnownColor.White, System.Drawing.KnownColor.Black)]
    [ScreenBorder(30, 20, System.Drawing.KnownColor.Gray)]
    class Program : RetroApplication
    {
        public enum LocationId
        {
            CLOSET_1,
            CLOSET_2,
            DORMITORY,
            STAIRWAY,
            DORMITORY_PARENTS,
            LIVINGROOM,
            KITCHEN,
            HALLWAY,
            STREET,
            BUTCHER,
            VILLAGE_SQUARE,
            SMITHY,
            PATH,
            FOREST_1,
            FOREST_2,
            FOREST_3,
            FOREST_4,
            RIVER_BANK_1,
            RIVER_BANK_2,
            CLEARING,
            CAVE_ENTRANCE,
            CAVE,
            STAIRCASE,
            TUNNEL_1,
            TUNNEL_2,
            TUNNEL_3,
            TUNNEL_4,
            LOCATION_1,
            LOCATION_2,
            CAVERN_1,
            MINING_AREA,
            UNDERGROUND_RIVER,
            CAVERN_2,
            WATERFALL,
            TUNNEL_ENTRANCE,
            TUNNEL_5,
            TUNNEL_6,
            TUNNEL_7,
            CELL_1,
            PRISON_CORRIDOR_1,
            PRISON_CORRIDOR_2,
            CELL_2
        }

        public static void Main(string[] args)
        {
            Initialize();

            WriteLine("Welcome to this text adventure game.");
            WriteLine();
            WriteLine("The game is played by typing in commands at the prompt.");
            WriteLine("Commands can be one or two word phrases of the form VERB or VERB NOUN.");
            WriteLine("You walk around in the world by writing the direction you want to go.");
            WriteLine("Example: N, S, E, W, U, D, NORTH, SOUTH, EAST, WEST, UP, DOWN");
            WriteLine("Write INVENTORY, INV or I to list items you are carrying.");
            WriteLine("Write LOOK or L to view the full location description again.");
            WriteLine("Write EXITS or E to view available exists from the current location.");
            WriteLine("Type HELP if you need help.");

            Location[] Locations = new Location[]
            {
                new Location(LocationId.CLOSET_1,
                    "You're sitting in a very tight place.",
                    "It's all dark. You wake up and your entire body hurts. You're crouching in what appears to be a very tight place. Feeling around with your hands you can feel that you lie ontop of what appears to be clothes. You can also feel what appears to be a door on your left. The door is closed.",
                    -1,-1,-1,-1,-1,-1),

                new Location(LocationId.CLOSET_2,
                    "You're sitting in your closet.",
                    "You're crouching on a shelf in the closet. The door to the closet is open and light filters in from your dormitory.",
                    -1,-1,-1,-1,-1,(int)LocationId.DORMITORY),

                new Location(LocationId.DORMITORY,
                    "You're in your dormitory.",
                    "You're standing in your dormitory. You have no idea why you woke up in the closet or how you ended up there. There's no sound coming from the rest of the house. The hallway to the east is quiet.",
                    -1,(int)LocationId.STAIRWAY,-1,-1,(int)LocationId.CLOSET_2,-1),

                new Location(LocationId.STAIRWAY,
                    "You're in the stairway.",
                    "You're standing in the stairway. Your dormitory is to the west and your parents dormitory is to the north. A stairway is leading down to the floor below. The house is awfully quiet.",
                    (int)LocationId.DORMITORY_PARENTS,-1,-1,(int)LocationId.DORMITORY,-1,(int)LocationId.LIVINGROOM),

                new Location(LocationId.DORMITORY_PARENTS,
                    "You're in your parents dormitory.",
                    "You're standing in your parents dormitory. Nobody is there. It seems your parents left in a hurry. Everything is a complete mess.",
                    -1,-1,(int)LocationId.STAIRWAY,-1,-1,-1),

                new Location(LocationId.LIVINGROOM,
                    "You're in the livingroom.",
                    "You're below the stairs in the livingroom. The house seems completely empty. Where are everybody? There kitchen lies to the west and the hallway to the north.",
                    (int)LocationId.HALLWAY,-1,-1,(int)LocationId.KITCHEN,-1,(int)LocationId.STAIRWAY)
            };

            List<string> StringList;
            int CurrentLocationId = (int)LocationId.CLOSET_1;
            Location CurrentLocation;
            bool Done = false;
            string s;
            string Verb;
            string Noun;
            int i;

            while (!Done)
            {
                CurrentLocation = Locations[CurrentLocationId];

                WriteLine();
                if (CurrentLocation.Visited)
                    WriteLineWordWrap(CurrentLocation.ShortDescription);
                else
                {
                    WriteLineWordWrap(CurrentLocation.LongDescription);
                    CurrentLocation.Visited = true;
                }

                WriteLine();
                Write("> ");

                ForegroundColor = Color.Cyan;
                s = ReadLine();
                ForegroundColor = Color.White;
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
                        case "N":
                        case "NORTH":
                        case "E":
                        case "EAST":
                        case "S":
                        case "SOUTH":
                        case "W":
                        case "WEST":
                        case "U":
                        case "UP":
                        case "D":
                        case "DOWN":
                            Noun = Verb;
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

                switch (Verb)
                {
                    case "GO":
                        switch (Noun)
                        {
                            case "N":
                            case "NORTH":
                                if (Locations[CurrentLocationId].North < 0)
                                    WriteLineWordWrap("You cannot go north from this location.");
                                else
                                {
                                    CurrentLocationId = Locations[CurrentLocationId].North;
                                    WriteLineWordWrap("You go north.");
                                }
                                continue;

                            case "E":
                            case "EAST":
                                if (Locations[CurrentLocationId].East < 0)
                                    WriteLineWordWrap("You cannot go east from this location.");
                                else
                                {
                                    CurrentLocationId = Locations[CurrentLocationId].East;
                                    WriteLineWordWrap("You go east.");
                                }
                                continue;

                            case "S":
                            case "SOUTH":
                                if (Locations[CurrentLocationId].South < 0)
                                    WriteLineWordWrap("You cannot go south from this location.");
                                else
                                {
                                    CurrentLocationId = Locations[CurrentLocationId].South;
                                    WriteLineWordWrap("You go south.");
                                }
                                continue;

                            case "W":
                            case "WEST":
                                if (Locations[CurrentLocationId].West < 0)
                                    WriteLineWordWrap("You cannot go west from this location.");
                                else
                                {
                                    CurrentLocationId = Locations[CurrentLocationId].West;
                                    WriteLineWordWrap("You go west.");
                                }
                                continue;

                            case "U":
                            case "UP":
                                if (Locations[CurrentLocationId].Up < 0)
                                    WriteLineWordWrap("You cannot go up from this location.");
                                else
                                {
                                    CurrentLocationId = Locations[CurrentLocationId].Up;
                                    WriteLineWordWrap("You go up.");
                                }
                                continue;

                            case "D":
                            case "DOWN":
                                if (Locations[CurrentLocationId].Down < 0)
                                    WriteLineWordWrap("You cannot go down from this location.");
                                else
                                {
                                    CurrentLocationId = Locations[CurrentLocationId].Down;
                                    WriteLineWordWrap("You go down.");
                                }
                                continue;

                            default:
                                WriteLineWordWrap("Unsure how to go in that direction.");
                                continue;
                        }

                    case "OPEN":
                        switch ((LocationId)CurrentLocationId)
                        {
                            case LocationId.CLOSET_1:
                                if (Noun == "DOOR")
                                {
                                    WriteLineWordWrap("You open the door you felt and light filters in. You now see that you're crouching in the closet in your own room. If you want you can go down from the closet.");
                                    CurrentLocationId = (int)LocationId.CLOSET_2;
                                    continue;
                                }
                                break;

                            case LocationId.CLOSET_2:
                                if (Noun == "DOOR")
                                {
                                    WriteLineWordWrap("The door is already open.");
                                    continue;
                                }
                                break;
                        }
                        break;

                    case "CLOSE":
                        switch ((LocationId)CurrentLocationId)
                        {
                            case LocationId.CLOSET_1:
                                if (Noun == "DOOR")
                                {
                                    WriteLineWordWrap("The door is already closed.");
                                    continue;
                                }
                                break;

                            case LocationId.CLOSET_2:
                                if (Noun == "DOOR")
                                {
                                    WriteLineWordWrap("You close the door to the closet, shutting out all light.");
                                    CurrentLocationId = (int)LocationId.CLOSET_1;
                                    continue;
                                }
                                break;
                        }
                        break;

                    case "SHOW":
                        switch (Noun)
                        {
                            case "EX":
                            case "EXITS":
                                StringList = new List<string>();

                                if (CurrentLocation.North >= 0)
                                    StringList.Add("north");

                                if (CurrentLocation.East >= 0)
                                    StringList.Add("east");

                                if (CurrentLocation.South >= 0)
                                    StringList.Add("south");

                                if (CurrentLocation.West >= 0)
                                    StringList.Add("west");

                                if (CurrentLocation.Up >= 0)
                                    StringList.Add("up");

                                if (CurrentLocation.Down >= 0)
                                    StringList.Add("down");

                                if (StringList.Count == 0)
                                    WriteLineWordWrap("You cannot see any exits from this location.");
                                else
                                {
                                    WriteWordWrap("You can go ");
                                    WriteList(StringList);
                                }
                                continue;

                            case "I":
                            case "INV":
                            case "INVENTORY":
                                // TODO
                                break;
                        }
                        break;

                    case "L":
                    case "LOOK":
                        CurrentLocation.Visited = false;
                        continue;

                    case "SAVE":
                        // TODO
                        break;

                    case "LOAD":
                        // TODO
                        break;

                    case "Q":
                    case "QUIT":
                        Done = true;
                        continue;
                }

                WriteLineWordWrap("Unsure how to do that.");
            }

            WriteLineWordWrap("Goodbye.");

            Terminate();
        }

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

        class Location
        {
            public LocationId Id;
            public string ShortDescription;
            public string LongDescription;
            public int North;
            public int East;
            public int South;
            public int West;
            public int Up;
            public int Down;
            public bool Visited = false;

            public Location(LocationId Id, string ShortDescription, string LongDescription, int North, int East, int South, int West, int Up, int Down)
            {
                this.Id = Id;
                this.ShortDescription = ShortDescription;
                this.LongDescription = LongDescription;
                this.North = North;
                this.East = East;
                this.South = South;
                this.West = West;
                this.Up = Up;
                this.Down = Down;
            }
        }
    }
}