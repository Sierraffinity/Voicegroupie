using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Voicegroupie
{
    struct Metadata
    {
        public int start;
        public int size;
    }
    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Please enter a path.");
                return 1;
            }

            string directory = args[0] + '\\';

            byte[] baserom = File.ReadAllBytes(directory + "baserom.gba");
            var map = new SortedList<int, string>();

            Metadata voiceGroups = ReadDataFile(directory + @"sound\voice_groups.inc", ref map);
            Metadata keysplitTables = ReadDataFile(directory + @"sound\keysplit_tables.inc", ref map);
            Metadata programmableWaveData = ReadDataFile(directory + @"sound\programmable_wave_data.inc", ref map);
            Metadata directSoundData = ReadDataFile(directory + @"sound\direct_sound_data.inc", ref map);

            Directory.CreateDirectory(directory + @"sound\direct_sound_samples");
            Directory.CreateDirectory(directory + @"sound\key_split_tables");
            Directory.CreateDirectory(directory + @"sound\programmable_wave_samples");

            List<string> speciesNames = ReadSpeciesFiles(directory);

            int cry = 0;
            int cry2 = 0;

            for (int offset = voiceGroups.start, size = voiceGroups.size; size > 0; offset += 12, size -= 12)
            {
                int ptr = BitConverter.ToInt32(baserom, offset + 4) - 0x8000000;
                int ptr2 = BitConverter.ToInt32(baserom, offset + 8) - 0x8000000;
                switch (baserom[offset])
                {
                    case 0:
                    case 8:
                    case 16:
                        if (!map.ContainsKey(ptr))
                            map.Add(ptr, "DirectSoundWaveData_8" + ptr.ToString("X"));
                        break;
                    case 3:
                    case 11:
                        if (!map.ContainsKey(ptr))
                            map.Add(ptr, "ProgrammableWaveData_8" + ptr.ToString("X"));
                        break;
                    case 0x40:
                        if (!map.ContainsKey(ptr))
                            map.Add(ptr, "voicegroup_8" + ptr.ToString("X"));
                        if (!map.ContainsKey(ptr2))
                            map.Add(ptr2, "KeySplitTable_8" + ptr2.ToString("X"));
                        break;
                    case 0x80:
                        if (!map.ContainsKey(ptr))
                            map.Add(ptr, "voicegroup_8" + ptr.ToString("X"));
                        break;
                    case 0x20:
                        if (!map.ContainsKey(ptr))
                            map.Add(ptr, "Cry_" + speciesNames[cry++]);
                        break;
                    case 0x30:
                        if (!map.ContainsKey(ptr))
                            map.Add(ptr, "Cry_" + speciesNames[cry2++]);
                        break;
                }
            }

            // Data parsed, now extract

            var vgOut = new List<string>();

            for (int offset = voiceGroups.start, size = voiceGroups.size; size > 0; offset += 12, size -= 12)
            {

                if (map.ContainsKey(offset))
                {
                    string str = String.Format("\t.align 2\n{0}:: @ 8{1}", map[offset], offset.ToString("X"));
                    if (vgOut.Count > 0)
                        vgOut.Add("\n" + str);
                    else
                        vgOut.Add(str);
                }
                
                int ptr = BitConverter.ToInt32(baserom, offset + 4) - 0x8000000;
                int ptr2 = BitConverter.ToInt32(baserom, offset + 8) - 0x8000000;
                string output = "\t";
                switch (baserom[offset])
                {
                    case 0:
                    case 8:
                    case 16:
                        if (baserom[offset] == 0)
                            output += "voice_directsound ";
                        else if (baserom[offset] == 8)
                            output += "voice_directsound_no_resample ";
                        else if (baserom[offset] == 16)
                            output += "voice_directsound_alt ";

                        output += baserom[offset + 1] + ", ";

                        if ((baserom[offset + 3] & 0x80) != 0)
                            output += baserom[offset + 3] - 0x80 + ", ";
                        else
                            output += "0, ";

                        output += map[ptr] + ", ";
                        output += baserom[offset + 8] + ", ";
                        output += baserom[offset + 9] + ", ";
                        output += baserom[offset + 10] + ", ";
                        output += baserom[offset + 11];
                        break;
                    case 1:
                    case 9:
                        if (baserom[offset] == 1)
                            output += "voice_square_1 ";
                        else if (baserom[offset] == 9)
                            output += "voice_square_1_alt ";

                        output += baserom[offset + 3] + ", ";
                        output += baserom[offset + 4] + ", ";

                        output += baserom[offset + 8] + ", ";
                        output += baserom[offset + 9] + ", ";
                        output += baserom[offset + 10] + ", ";
                        output += baserom[offset + 11];
                        break;
                    case 2:
                    case 10:
                        if (baserom[offset] == 2)
                            output += "voice_square_2 ";
                        else if (baserom[offset] == 10)
                            output += "voice_square_2_alt ";

                        output += baserom[offset + 4] + ", ";

                        output += baserom[offset + 8] + ", ";
                        output += baserom[offset + 9] + ", ";
                        output += baserom[offset + 10] + ", ";
                        output += baserom[offset + 11];
                        break;
                    case 3:
                    case 11:
                        if (baserom[offset] == 3)
                            output += "voice_programmable_wave ";
                        else if (baserom[offset] == 11)
                            output += "voice_programmable_wave_alt ";

                        output += map[ptr] + ", ";

                        output += baserom[offset + 8] + ", ";
                        output += baserom[offset + 9] + ", ";
                        output += baserom[offset + 10] + ", ";
                        output += baserom[offset + 11];
                        break;
                    case 4:
                    case 12:
                        if (baserom[offset] == 4)
                            output += "voice_noise ";
                        else if (baserom[offset] == 12)
                            output += "voice_noise_alt ";

                        output += baserom[offset + 4] + ", ";

                        output += baserom[offset + 8] + ", ";
                        output += baserom[offset + 9] + ", ";
                        output += baserom[offset + 10] + ", ";
                        output += baserom[offset + 11];
                        break;
                    case 0x40:
                        output += "voice_keysplit ";
                        output += map[ptr] + ", ";
                        output += map[ptr2];
                        break;
                    case 0x80:
                        output += "voice_keysplit_all ";
                        output += map[ptr];
                        break;
                    case 0x20:
                        output += "cry ";
                        output += map[ptr];
                        break;
                    case 0x30:
                        output += "cry2 ";
                        output += map[ptr];
                        break;
                }
                output += "  @ " + (offset + 0x8000000).ToString("X");
                vgOut.Add(output);
            }

            var ktOut = new List<string>();

            for (int idx = map.IndexOfKey(keysplitTables.start), totalSize = keysplitTables.size; totalSize > 0; idx++)
            {
                int size = totalSize;
                if ((idx + 1) < map.Count && map.Keys[idx + 1] - map.Keys[idx] < size)
                {
                    size = map.Keys[idx + 1] - map.Keys[idx];
                }
                totalSize -= size;

                string str = String.Format("{0}:: @ 8{1}", map[map.Keys[idx]], map.Keys[idx].ToString("X"));
                if (ktOut.Count > 0)
                    ktOut.Add("\n" + str);
                else
                    ktOut.Add(str);

                string fileName = String.Format("sound/key_split_tables/8{0}.bin", map.Keys[idx].ToString("X"));

                ktOut.Add(String.Format("\t.incbin \"{0}\"", fileName));

                byte[] bytes = new byte[size];
                Buffer.BlockCopy(baserom, map.Keys[idx], bytes, 0, size);

                File.WriteAllBytes(directory + fileName, bytes);
            }

            var pwdOut = new List<string>();

            for (int offset = programmableWaveData.start, size = programmableWaveData.size; size > 0; offset += 16, size -= 16)
            {
                bool unused = false;
                if (!map.ContainsKey(offset))
                {
                    unused = true;
                    map.Add(offset, "ProgrammableWaveData_Unused_8" + offset.ToString("X"));
                }

                string str = String.Format("{0}:: @ 8{1}", map[offset], offset.ToString("X"));

                if (pwdOut.Count > 0)
                    pwdOut.Add("\n" + str);
                else
                    pwdOut.Add(str);

                string fileName = String.Format("sound/programmable_wave_samples/{0}8{1}.pcm", unused ? "unused_" : "", offset.ToString("X"));

                pwdOut.Add(String.Format("\t.incbin \"{0}\"", fileName));

                byte[] bytes = new byte[16];
                Buffer.BlockCopy(baserom, offset, bytes, 0, 16);

                File.WriteAllBytes(directory + fileName, bytes);
            }

            var dsdOut = new List<string>();

            for (int offset = directSoundData.start, sizeRemaining = directSoundData.size; sizeRemaining > 0;)
            {
                int size = BitConverter.ToInt32(baserom, offset + 12) + 1;
                bool unused = false;
                if (!map.ContainsKey(offset))
                {
                    unused = true;
                    map.Add(offset, "DirectSoundWaveData_Unused_8" + offset.ToString("X"));
                }

                string symbol = map[offset];

                string str = String.Format("\t.align 2\n{0}:: @ 8{1}", symbol, offset.ToString("X"));

                if (dsdOut.Count > 0)
                    dsdOut.Add("\n" + str);
                else
                    dsdOut.Add(str);

                string fileName = String.Format("sound/direct_sound_samples/{0}8{1}.bin", unused ? "unused_" : "", offset.ToString("X"));

                if (map[offset].StartsWith("Cry"))
                {
                    int idx = symbol.IndexOf("Unused");
                    if (idx > 0)
                        fileName = String.Format("sound/direct_sound_samples/cry_unused_{0}.bin", symbol.Substring(idx + 6));
                    else
                    {
                        string snakeCase = String.Concat(symbol.Replace("_", string.Empty).Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();

                        fileName = String.Format("sound/direct_sound_samples/{0}.bin", snakeCase);
                    }
                }

                dsdOut.Add(String.Format("\t.incbin \"{0}\"", fileName));

                if ((BitConverter.ToInt32(baserom, offset) & 1) == 1)
                {
                    uint i = 0;
                    uint j = 0;
                    while (true)
                    {
                        i++;
                        j++;
                        if (j >= size)
                        {
                            break;
                        }
                        j++;
                        i++;
                        if (j >= size)
                        {
                            break;
                        }
                        for (int k = 0; k < 31; k++)
                        {
                            j++;
                            if (j >= size)
                            {
                                break;
                            }
                            j++;
                            i++;
                            if (j >= size)
                            {
                                break;
                            }
                        }
                        if (j >= size)
                        {
                            break;
                        }
                    }
                    size = (int)i;
                }

                size += 16;

                byte[] bytes = new byte[size];
                Buffer.BlockCopy(baserom, offset, bytes, 0, size);

                File.WriteAllBytes(directory + fileName, bytes);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = directory + @"tools\aif2pcm\aif2pcm";
                startInfo.Arguments = directory + fileName;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                Process.Start(startInfo);

                offset += size;
                sizeRemaining -= size;
                if (offset % 4 != 0)
                {
                    int alignment = 4 - (offset % 4);
                    offset += alignment;
                    sizeRemaining -= alignment;
                }
            }

            using (StreamWriter stream = File.CreateText(directory + @"sound\voice_groups.txt"))
            {
                foreach (string line in vgOut)
                    stream.WriteLine(line);
            }

            using (StreamWriter stream = File.CreateText(directory + @"sound\keysplit_tables.txt"))
            {
                foreach (string line in ktOut)
                    stream.WriteLine(line);
            }

            using (StreamWriter stream = File.CreateText(directory + @"sound\programmable_wave_data.txt"))
            {
                foreach (string line in pwdOut)
                    stream.WriteLine(line);
            }

            using (StreamWriter stream = File.CreateText(directory + @"sound\direct_sound_data.txt"))
            {
                foreach (string line in dsdOut)
                    stream.WriteLine(line);
            }

            return 0;
        }

        public static string GetUntilOrEmpty(this string text, string stopAt = " ")
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return String.Empty;
        }

        public static Metadata ReadDataFile(string fileName, ref SortedList<int, string> map)
        {
            string[] lines = File.ReadAllLines(fileName);
            string name = "";
            Metadata data = new Metadata();

            foreach (string line in lines)
            {
                if (line.Contains(".incbin"))
                {
                    string[] words = line.Replace(",", "").Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);

                    int offset = Convert.ToInt32(words[2], 16);
                    if (data.start == 0)
                        data.start = offset;

                    if (!String.IsNullOrEmpty(name))
                    {
                        map.Add(offset, name);
                        name = "";
                    }
                    data.size += Convert.ToInt32(words[3], 16);
                }
                else if (line.Contains(":"))
                {
                    name = GetUntilOrEmpty(line, ":");
                }
            }
            return data;
        }

        public static List<string> ReadSpeciesFiles(string directory)
        {
            string[] speciesNames = File.ReadAllLines(directory + @"data\text\species_names.inc");
            string[] cryIdTable = File.ReadAllLines(directory + @"data\cry_id_table.inc");

            var names = new List<string>();

            foreach (string line in speciesNames)
            {
                if (line.Contains(".string"))
                {
                    string name = Regex.Match(line, "\"([^\"]*)\"").Groups[1].Value.TrimEnd('$');

                    if (name == "NIDORAN♀")
                        name = "NidoranF";
                    else if (name == "NIDORAN♂")
                        name = "NidoranM";
                    else if (name == "FARFETCH’D")
                        name = "Farfetchd";
                    else if (name == "MR. MIME")
                        name = "MrMime";
                    else if (name == "HO-OH")
                        name = "HoOh";
                    else
                        name = name.First().ToString().ToUpper() + name.ToLower().Substring(1);

                    names.Add(name);
                }
            }

            SortedList<short, short> gSpeciesIdToCryId = new SortedList<short, short>();
            short count = 277;

            foreach(string line in cryIdTable)
            {
                string[] words = line.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                if (words[0] == ".2byte")
                {
                    gSpeciesIdToCryId.Add(Int16.Parse(words[1]), count++);
                }
            }

            var reorderedNames = new List<string>();

            for (int i = 0; i < 388; i++)
            {
                string species = "ERROR";
                if (i < 251)
                    species = names[i + 1];
                else
                {
                    if (gSpeciesIdToCryId.ContainsKey((short)i))
                        species = names[gSpeciesIdToCryId[(short)i]];
                    else
                        species = "Unused" + i;
                }

                reorderedNames.Add(species);
            }

            return reorderedNames;
        }
    }
}
