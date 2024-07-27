using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Drawing;

namespace GMSCacheManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: \n-runLast: Runs last created GMS cache data.\n-clearLightweightData: Clear OGGs from a lightweight build (Will *not* support main build).");
                if (args[0] == "-clearLightweightData")
                {
                    Console.WriteLine("Specify lightweight path.");
                    args.Append(Console.ReadLine());
                }
            }
            if (args[0] == "-runLast")
            {
                string runnerDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\gamemaker_studio\\Runner.exe";
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\Local";
                List<string> extensions = new List<string>();


                string[] folders = Directory.GetDirectories(appData, "gm_ttt_*");
                foreach (string folder in folders)
                {
                    Console.WriteLine(folder);
                }
                extensions.Add(".win");
                DateTime latestTime = DateTime.Now.AddDays(1000);//Leniency.
                string latestGame = "";
                foreach (string dir in folders)
                {
                    string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                            .Where(f => extensions.IndexOf(Path.GetExtension(f)) >= 0).ToArray();
                    foreach (string file in files)
                    {
                        var date = File.GetLastWriteTime(file);
                        if (latestGame == "" || DateTime.Compare(date, latestTime) > 0)
                        {
                            latestGame = file;
                            latestTime = date;
                        }
                        Console.WriteLine(file);
                    }
                }
                Console.WriteLine("Latest one is " + latestGame);
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = runnerDirectory;//"cmd.exe";
                startInfo.Arguments = "-game \"" + latestGame + "\"";//"/C copy /b Image1.jpg + Archive.rar Image2.jpg";
                process.StartInfo = startInfo;
                process.Start();
                Console.ReadLine();
                //Console.WriteLine("Hello World!");
            }
            else if (args[0] == "-clearLightweightData")
            {
                var i = 1;
                if (args[1] == "-doExternalRoomClear")
                {
                    doRoomClear = true;
                    i++;
                }
                if (!args[i].Contains("lw_"))
                {
                    Console.WriteLine("Will not compile projects without lw_ at the beginning!!! This is a safety measure.");
                    return;
                }
                Console.WriteLine("Beginning to clear data.");
                XmlDocument doc = new XmlDocument();
                doc.Load(args[i]);
                ReadXML_ClearLightweight(doc.SelectSingleNode("*"));//TraverseNodes(doc.LastChild.ChildNodes);
                foreach (XmlNode node in myDataNodes)
                {


                    node.ParentNode.ParentNode.RemoveChild(node.ParentNode);

                }
                doc.Save(args[i].Replace("lw_", "lw_"));
                Console.WriteLine("Done! Press enter to exit.");
                Console.ReadLine();
            }
            else if (args[0] == "-optimizeTexturePages")
            {
                var doDev = false;
                var index = 1;
                if (args[1] == "-developmentVersion")
                {
                    doDev = true;
                    index++;
                }
                else
                {
                    Console.WriteLine("Do development version? This will split devkit-groups into groups of 8. It is recommended to say no for a public release. y/n");
                    var answer = Console.ReadLine();
                    if (answer == "y" || answer == "Y")
                    {
                        doDev = true;
                    }
                }
                    //                A test to determine the total average size for all sprites (The answer was about 48x44, though this still has *heavy* bias on the side of Hodenkis stuff still being in the files).
                    /*int i = 0;
                    int widthTotal = 0;
                    int heightTotal = 0;
                    Console.WriteLine("Sprites");
                    foreach (string file in Directory.GetFiles(@"C:\Users\Gannio\repos\magmml3-judge-repo\sprites\images"))
                    {
                        if (!file.Contains("8_BIT_FAN"))//Hodenki literally causes bias I'm going to scream.
                        {
                            using (Bitmap myMap = new Bitmap(file))
                            {
                                widthTotal += myMap.Width;
                                heightTotal += myMap.Height;
                            }
                            i++;
                        }
                    }
                    Console.WriteLine("Backgrounds");
                    foreach (string file in Directory.GetFiles(@"C:\Users\Gannio\repos\magmml3-judge-repo\background\images"))
                    {
                        using (Bitmap myMap = new Bitmap(file))
                        {
                            widthTotal += myMap.Width;
                            heightTotal += myMap.Height;
                        }
                        i++;
                    }
                    Console.WriteLine(widthTotal/i);
                    Console.WriteLine(heightTotal / i);
                    int b = 0;
                    */


                    XmlDocument doc = new XmlDocument();
                doc.Load(args[index]);
                ReadXML(doc.SelectSingleNode("*"));
                CompileTextureGroupData(doDev);
                int d = 0;
            }
        }
        static int optimize_spriteLevel = -1;
        /*private static int TraverseNodes(XmlNodeList nodes)
        {
            foreach (XmlNode node in nodes)
            {
                Console.WriteLine(node.Name);
                if (node.Name == "name" && node.Value.Contains(".ogg"))
                {
                    return -1;
                }
                else
                {
                    // Do something with the node.
                    var output = TraverseNodes(node.ChildNodes);
                    if (output == -1)
                    {
                        
                    }
                    return output;
                }
            }
            return 1;
        }*/
        private static Dictionary<string, List<string>> groupDictionary = new Dictionary<string, List<string>>();


        static readonly int minTexturePageRequirement = 2048 * 2048;//1024;//128;//35;//15;//35;
        /*
         * Assume that the average sprite is 64x64. This means you can store about 1024 images per 2048x2048 texture page. Half by 512 to be safe.
         */

        private static void CompileTextureGroupData(bool doDev)
        {
            string[] addOns = { "/Collision", "/Items", "/Effects", "", "/Editor GFX", "/NPC" };

            foreach (var i1 in addOns)
            {
                groupDictionary["/Enemies"].AddRange(groupDictionary[i1]);
                groupDictionary.Remove(i1);
            }
            //Should be misc but because of quirks with fusion words, it isn't.
            //Combine items and effects into enemies, as they're all commonly used together.
            addOns = new string[] { "/Minibosses", "/Menu", "/Borders/Shaders" };//, "/Effects", "", "/EditorGFX", "/NPC" };

            foreach (var i2 in addOns)
            {
                groupDictionary["/Bosses"].AddRange(groupDictionary[i2]);
                groupDictionary.Remove(i2);
            }
            for (var k = 0; k < groupDictionary.Count; k++)
            {
                var entry = groupDictionary.ElementAt(k);

                if (entry.Key.Contains("/Entries"))
                {
                    for (var j = 0; j < groupDictionary.Count; j++)
                    {
                        var entry2 = groupDictionary.ElementAt(j);
                        {
                            if (entry2.Key.Contains(entry.Key) && entry2.Key != entry.Key)
                            {
                                if ((entry.Key.Contains("Neo Pit of Pits") ^ entry2.Key.Contains("Neo Pit of Pits")) == false)//Todo: Do this for Tier X assets.
                                {
                                    Console.WriteLine("Entry fusion: " + entry.Key + "<-" + entry2.Key);
                                    entry.Value.AddRange(entry2.Value);
                                    groupDictionary.Remove(entry2.Key);
                                    break;
                                }
                            }
                        }
                    }
                }

            }
            /*foreach (var entry in groupDictionary)
            {
                if (entry.Key.Contains("/Entries"))
                {
                    foreach (var entry2 in groupDictionary)
                    {
                        if (entry2.Key.Contains(entry.Key) && entry2.Key != entry.Key)
                        {
                            if ((entry.Key.Contains("Neo Pit of Pits") ^ entry2.Key.Contains("Neo Pit of Pits")) == false)//Todo: Do this for Tier X assets.
                            {
                                Console.WriteLine("Entry fusion: " + entry.Key + "<-" + entry2.Key);
                                entry.Value.AddRange(entry2.Value);
                                groupDictionary.Remove(entry2.Key);
                                break;
                            }
                        }
                    }
                }
            }*/
            

            groupDictionary["/Player"].AddRange(groupDictionary["/Gimmicks"]);
            groupDictionary.Remove("/Gimmicks");
            //groupDictionary["/Player"].AddRange(groupDictionary["/Gimmicks"]);
            //groupDictionary.Remove("/Gimmicks");
            groupDictionary["/Player"].AddRange(groupDictionary["/Sections"]);
            groupDictionary.Remove("/Sections");
            groupDictionary["/Player"].AddRange(groupDictionary["/Title"]);
            groupDictionary.Remove("/Title");
            //So problem: If sprites are in the 
            for (var a = 0; a < groupDictionary.Count-1; a++)
            {
                var group1 = groupDictionary.ElementAt(a);
                var group2 = groupDictionary.ElementAt(a+1);
                var size1 = 0;
                var size2 = 0;

                if (!group1.Value.Contains("Entries") && !group2.Value.Contains("Entries"))
                {
                    foreach (var entry in group1.Value)
                    {
                        string assetType = "sprite";
                        XmlDocument doc = new XmlDocument();

                        if (entry.Contains("background\\"))
                        {
                            assetType = "background";
                            doc.Load(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");

                            size1 += (Convert.ToInt32(doc.GetElementsByTagName("width")[0].InnerText) * Convert.ToInt32(doc.GetElementsByTagName("height")[0].InnerText));//group1.Value.Count;
                        }
                        else
                        {
                            doc.Load(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");
                            size1 += (doc.GetElementsByTagName("frames")[0].ChildNodes.Count) *
                                (Convert.ToInt32(doc.GetElementsByTagName("width")[0].InnerText) *
                                Convert.ToInt32(doc.GetElementsByTagName("height")[0].InnerText));
                        }
                        //Convert.ToInt32(doc.GetElementsByTagName("Width")[0].InnerText) * Convert.ToInt32(doc.GetElementsByTagName("Height")[0].InnerText)
                    }
                    foreach (var entry in group2.Value)
                    {
                        string assetType = "sprite";
                        XmlDocument doc = new XmlDocument();

                        if (entry.Contains("background\\"))
                        {
                            assetType = "background";
                            doc.Load(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");

                            size2 += (Convert.ToInt32(doc.GetElementsByTagName("width")[0].InnerText) * Convert.ToInt32(doc.GetElementsByTagName("height")[0].InnerText));//group1.Value.Count;
                        }
                        else
                        {
                            doc.Load(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");
                            size2 += (doc.GetElementsByTagName("frames")[0].ChildNodes.Count) *
                                (Convert.ToInt32(doc.GetElementsByTagName("width")[0].InnerText) *
                                Convert.ToInt32(doc.GetElementsByTagName("height")[0].InnerText));
                        }
                        //Convert.ToInt32(doc.GetElementsByTagName("Width")[0].InnerText) * Convert.ToInt32(doc.GetElementsByTagName("Height")[0].InnerText)
                    }
                    if (size1 + size2 < minTexturePageRequirement)//group1.Value.Count + group2.Value.Count < minTexturePageRequirement)//Fusion!
                    {
                        Console.WriteLine("Fusing " + group1.Key + " and " + group2.Key);
                        group1.Value.AddRange(group2.Value);
                        groupDictionary[group1.Key] = group1.Value;
                        groupDictionary.Remove(group2.Key);
                        a--;//Go backwards so we can try again with the next value in line.
                    }
                }
                
            }
            /*for (var a = 0; a < groupDictionary.Count - 1; a++)
            {
                var group1 = groupDictionary.ElementAt(a);
                var group2 = groupDictionary.ElementAt(a + 1);

//                var container1
                
                foreach (var entry in group1.Value)
                {
                    XmlDocument doc = new XmlDocument();

                    string assetType = "sprite";
                    if (entry.Contains("background\\"))
                    {
                        assetType = "background";
                    }
                    doc.Load(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");

                    doc.GetElementsByTagName("Width")[0].InnerText = i.ToString();
                }


                if (group1.Value.Count + group2.Value.Count < 35)//Fusion!
                {
                    Console.WriteLine("Fusing " + group1.Key + " and " + group2.Key);
                    group1.Value.Concat(group2.Value);
                    groupDictionary[group1.Key] = group1.Value;
                    groupDictionary.Remove(group2.Key);
                    a--;//Go backwards so we can try again with the next value in line.
                }


            }*/
            Console.WriteLine("Splitting skins/borders into their own group each.");
            for (var i = 0; i < groupDictionary.Count; i++)//Split each entry in Skins/Borders to their own page.
            {
                var entry = groupDictionary.ElementAt(i);
                var key = entry.Key;
                bool isDigitPresent = key.Any(c => char.IsDigit(c));
                if ((key.Contains("/Skins") || key.Contains("/Borders")) && !isDigitPresent)
                {
                    
                    var val = entry.Value;
                    groupDictionary.Remove(key);
                    var a = 0;
                    foreach (string value in val)
                    {
                        List<string> singleList = new List<string>();
                        singleList.Add(value);
                        groupDictionary.Add(key + a, singleList);
                        a++;
                    }
                }
            }

            //For development: Split up the enemies one in half.
            if (doDev)
            {
                Console.WriteLine("Splitting up devkit groups for ease of development.");
                for (var i = 0; i < groupDictionary.Count; i++)
                {
                    
                    if (/*!groupDictionary.ElementAt(i).Key.Contains("Entries") && */!groupDictionary.ElementAt(i).Key.Contains("Skins"))
                    {
                        var amount = 8;
                        if (groupDictionary.ElementAt(i).Key.Contains("Entries"))
                        {
                            amount = 4;
                        }
                        var key = groupDictionary.ElementAt(i).Key;
                        var value = groupDictionary[key];

                        bool isDigitPresent = key.Any(c => char.IsDigit(c));
                        if (!isDigitPresent)
                        {
                            groupDictionary.Remove(key);
                            var split = SplitList(value, value.Count() / amount);
                            var j = 0;
                            foreach (var entry in split)
                            {
                                groupDictionary.Add(key + j.ToString(), entry);
                                j++;
                            }

                        }

                    }
                }
            }
            List<string> paragraph_groupSettings = new List<string>();//Need a list as we need to sort them into dumb numbering later.
            List<string> paragraph_groupNames = new List<string>();

            paragraph_groupSettings.Add(@"<option_textureGroup0_border>2</option_textureGroup0_border>
    <option_textureGroup0_nocropping>0</option_textureGroup0_nocropping>
    <option_textureGroup0_parent>&lt;none&gt;</option_textureGroup0_parent>
    <option_textureGroup0_scaled>0</option_textureGroup0_scaled>
    <option_textureGroup0_targets>9223372036854775807</option_textureGroup0_targets>");
            paragraph_groupNames.Add("<option_textureGroups0>Default</option_textureGroups0>");
            var groupNum = 1;//Leave one froom for Default.
            foreach (var group in groupDictionary)
            {
                
                if (group.Value.Count <= 0)
                {
                    continue;
                }
                paragraph_groupSettings.Add("<option_textureGroup" + groupNum.ToString() + "_border>2</option_textureGroup" + groupNum.ToString() + "_border>\n" +
                    "<option_textureGroup" + groupNum.ToString() + "_nocropping>0</option_textureGroup" + groupNum.ToString() + "_nocropping>\n" +
                    "<option_textureGroup" + groupNum.ToString() + "_parent>&lt;none&gt;</option_textureGroup" + groupNum.ToString() + "_parent>\n" +
                    "<option_textureGroup" + groupNum.ToString() + "_scaled>0</option_textureGroup" + groupNum.ToString() + "_scaled>\n" +
                    "<option_textureGroup" + groupNum.ToString() + "_targets>9223372036854775807</option_textureGroup" + groupNum.ToString() + "_targets>");//This last one is for what platforms to build for, some weird giant integer flag.
                paragraph_groupNames.Add("<option_textureGroups" + groupNum.ToString() + ">TexPage" + groupNum.ToString() + "</option_textureGroups" + groupNum.ToString() + ">");

                Console.WriteLine(group.Key + "->TextureGroup" + groupNum);
                var entries = group.Value;
                foreach (var entry in entries)
                {
                    XmlDocument doc = new XmlDocument();

                    string assetType = "sprite";
                    if (entry.Contains("background\\"))
                    {
                        assetType = "background";
                    }
                    doc.Load(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");

                    doc.GetElementsByTagName("TextureGroup0")[0].InnerText = groupNum.ToString();
                    doc.Save(@"C:\Users\Gannio\repos\magmml3-judge-repo\" + entry + "." + assetType + ".gmx");
                }

                groupNum++;
            }
            paragraph_groupSettings.Add("<option_textureGroup_count>" + (paragraph_groupNames.Count()).ToString() + "</option_textureGroup_count>");
            paragraph_groupSettings.Sort((Comparison<String>)(
            (String left, String right) => {
                return String.CompareOrdinal(left, right);
            }
            ));
            paragraph_groupNames.Sort((Comparison<String>)(
            (String left, String right) => {
                return String.CompareOrdinal(left, right);
            }
            ));
            using (StreamWriter sw = new StreamWriter("ConfigOut.txt"))
            {
                foreach (string value in paragraph_groupSettings)
                {
                    sw.WriteLine(value);
                }
                foreach (string value in paragraph_groupNames)
                {
                    sw.WriteLine(value);
                }
            }
        }
        static string[] FusedPhrases = {"Alter Weapons", "Rare Weapons", "Rush","Misc","MaGMML1","MaGMML2", "MaG24HMML","MaG48HMML","MegaEngine","AlterWeapons","Misc.","Projectiles","Other","Icons", "Projectile", "Sakugarne"};//A number of phrases that, when detected, should be ignored in terms of a unique texture page, and just go with the previously made one.
        static string[] AbsoluteGroups = {"Player","Collison","Enemies","Minibosses","Bosses","Gimmicks","Sections","Effects","Items","Menu","Main Game","Skins","Misc"};
        private static void ReadXML(XmlNode root, string pathName = "")
        {
            var curPathName = pathName;
            
            if (optimize_spriteLevel < -1)
            {
                Console.WriteLine("End of sprites found. Exiting.");
            }
            else if (optimize_spriteLevel >= 0)
            {
                optimize_spriteLevel++;
            }
            
            if (root is XmlElement)
            {
                if (root.Name == "sprites")
                {
                    if (root.Attributes["name"].Value == "sprites")
                    {
                        if (optimize_spriteLevel != -1)
                        {
                            while (true)
                            {
                                Console.WriteLine("ERROR");//Hang, if this actually happens someone's dumb in the project file. Never name a folder sprites.
                            }
                        }
                        optimize_spriteLevel = 0;
                        Console.WriteLine("Sprites found. Searching...");
                    }
                    else
                    {
                        if (Array.IndexOf(FusedPhrases, root.Attributes["name"].Value) < 0)
                        {
                            var canPath = true;
                            for (var i = 0; i < AbsoluteGroups.Length; i++)
                            {
                                if (pathName.Contains(AbsoluteGroups[i]))
                                {
                                    canPath = false;
                                    continue;
                                }
                            }
                            if (canPath)
                            {
                                pathName += "/" + root.Attributes["name"].Value;
                            }
                        }
                    }
                    Console.WriteLine(pathName);
                }
                else if (root.Name == "backgrounds")
                {
                    if (root.Attributes["name"].Value == "backgrounds")
                    {
                        if (optimize_spriteLevel != -2)
                        {
                            while (true)
                            {
                                Console.WriteLine("ERROR");//Hang, if this actually happens someone's dumb in the project file. Never name a folder sprites.
                            }
                        }
                        optimize_spriteLevel = 0;
                        Console.WriteLine("Backgrounds found. Searching...");
                    }
                    else
                    {
                        pathName += "/" + root.Attributes["name"].Value;

                    }
                }

                
                if (root.HasChildNodes)
                    ReadXML(root.FirstChild,pathName);
                if (root.NextSibling != null)
                    ReadXML(root.NextSibling, curPathName);
                
                if (root.Name == "sprites" && root.Attributes["name"].Value == "sprites")
                {
                    optimize_spriteLevel = -2;
                }
                if (root.Name == "backgrounds" && root.Attributes["name"].Value == "backgrounds")
                {
                    optimize_spriteLevel = -3;
                }
            }
            else if (root is XmlText)
            {
                if (root.ParentNode.Name == "sprite" || root.ParentNode.Name == "background")
                {
                    List<string> currentEntries = new List<string>();
                    if (!groupDictionary.TryGetValue(curPathName, out currentEntries))
                    {
                        currentEntries = new List<string>();
                        currentEntries.Add(root.InnerText);
                        groupDictionary.Add(curPathName, currentEntries);
                    }
                    else
                    {
                        currentEntries.Add(root.InnerText);
                        groupDictionary[curPathName] = currentEntries;
                    }
                    
                }
            }
            else if (root is XmlComment)
            { }
        }
        //-clearLightweightData -doExternalRoomClear "C:\Users\Gannio\repos\magmml3-judge-repo\lw_MaGMML3.project.gmx"
        private static void ReadXML_ClearLightweight(XmlNode root)
        {
            if (root is XmlElement)
            {
                if (ClearLightweightData(root))
                {
                    if (root.HasChildNodes)
                        ReadXML_ClearLightweight(root.FirstChild);
                    if (root.NextSibling != null)
                        ReadXML_ClearLightweight(root.NextSibling);
                }
            }
            else if (root is XmlText)
            { }
            else if (root is XmlComment)
            { }
        }
        private static List<XmlNode> myDataNodes = new List<XmlNode>();
        private static bool doRoomClear = false;
        private static bool ClearLightweightData(XmlNode root)
        {
            var innerText = root.InnerText.ToLower();
            if (root.Name == "name" && (innerText.Contains(".ogg") || innerText.Contains(".mp3") || innerText.Contains(".nsf") || innerText.Contains(".spc") || innerText.Contains(".gbs") || innerText.Contains(".vgm") || innerText.Contains(".s3m") || innerText.Contains(".vgz") || (doRoomClear && innerText.Contains(".room.gmx"))))
            {
                Console.WriteLine("Removing " + innerText);
                //root.ParentNode.RemoveAll();
                myDataNodes.Add(root);
                //Console.WriteLine(root.Value);
                //root.ParentNode.ParentNode.RemoveChild(root.ParentNode);
                return false;
            }
            return true;
            //Console.WriteLine(root.Name);
        }
        public static List<List<string>> HalfList(List<string> list)
        {
            var newList = new List<List<string>>();
            newList.Add(list.GetRange(0,list.Count/2));
            newList.Add(list.GetRange(list.Count / 2,list.Count));
            return newList;
        
        }
        /*public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }*/
        public static List<List<string>> SplitList(List<string> locations, int nSize)
        {
            List<List<string>> ret = new List<List<string>>();
            if (locations.Count < 10)
            {
                ret.Add(locations);
                return ret;
            }
            else
            { 
            
                for (int i = 0; i < locations.Count; i += nSize)
                {
                    ret.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
                }
                return ret;
            }
        }
    }
}
