using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Rendering
{
    public class NameGenerator
    {
        private readonly string[] botNames = new string[] {
            "Albert",
            "Allen",
            "Bert",
            "Bob",
            "Cecil",
            "Clarence",
            "Elliot",
            "Elmer",
            "Ernie",
            "Eugene",
            "Fergus",
            "Ferris",
            "Frank",
            "Frasier",
            "Fred",
            "George",
            "Graham",
            "Harvey",
            "Irwin",
            "Larry",
            "Lester",
            "Marvin",
            "Neil",
            "Niles",
            "Oliver",
            "Opie",
            "Ryan",
            "Toby",
            "Ulric",
            "Ulysses",
            "Uri",
            "Waldo",
            "Wally",
            "Walt",
            "Wesley",
            "Yanni",
            "Yogi",
            "Yuri",
            "Alfred",
            "Bill",
            "Brandon",
            "Calvin",
            "Dean",
            "Dustin",
            "Ethan",
            "Harold",
            "Henry",
            "Irving",
            "Jason",
            "Jenssen",
            "Josh",
            "Martin",
            "Nick",
            "Norm",
            "Orin",
            "Pat",
            "Perry",
            "Ron",
            "Shawn",
            "Tim",
            "Will",
            "Wyatt",
            "Adam",
            "Andy",
            "Chris",
            "Colin",
            "Dennis",
            "Doug",
            "Duffy",
            "Gary",
            "Grant",
            "Greg",
            "Ian",
            "Jerry",
            "Jon",
            "Keith",
            "Mark",
            "Matt",
            "Mike",
            "Nate",
            "Paul",
            "Scott",
            "Steve",
            "Tom",
            "Yahn",
            "Adrian",
            "Bank",
            "Brad",
            "Connor",
            "Dave",
            "Dan",
            "Derek",
            "Don",
            "Eric",
            "Erik",
            "Finn",
            "Jeff",
            "Kevin",
            "Reed",
            "Rick",
            "Ted",
            "Troy",
            "Wade",
            "Wayne",
            "Xander",
            "Xavier",
            "Brian",
            "Chad",
            "Chet",
            "Gabe",
            "Hank",
            "Ivan",
            "Jim",
            "Joe",
            "John",
            "Tony",
            "Tyler",
            "Victor",
            "Vladimir",
            "Zane",
            "Zim",
            "Cory",
            "Quinn",
            "Seth",
            "Vinny",
            "Arnold",
            "Brett",
            "Kurt",
            "Kyle",
            "Moe",
            "Quade",
            "Quintin",
            "Ringo",
            "Rip",
            "Zach",
            "Cliffe",
            "Crusher",
            "Gunner",
            "Minh",
            "Garret",
            "Pheonix",
            "Ridgway",
            "Rock",
            "Shark",
            "Steel",
            "Stone",
            "Wolf",
            "Vitaliy",
            "Zed"
        };
        private readonly string[] zsunamyNames = new string[]
        {
            "B1",
            "OfficialLahusa",
            "jonny",
            "Lumex",
            "Marcoooo",
            "Revilum1",
            "BAPFEL",
            "fabifox25"
        };
        private List<string> weaponIDs;

        private Random random;
        private IconRegistry iconRegistry;

        public NameGenerator()
        {
            random = new Random();
            iconRegistry = new IconRegistry();

            weaponIDs = iconRegistry.GetIconNames("res/icons/weapons");
        }

        public string GenerateName()
        {
            if(random.Next(2) == 1)
            {
                return GenerateBotName();
            } else
            {
                return GenerateZsunamyName();
            }
        }

        public string GenerateBotName()
        {
            return "BOT " + botNames[random.Next(botNames.Length)];
        }

        public string GenerateZsunamyName()
        {
            return "Zsunamy " + zsunamyNames[random.Next(zsunamyNames.Length)];
        }

        public string GetRandomWeapon()
        {
            return weaponIDs[random.Next(weaponIDs.Count)];
        }
    }
}
