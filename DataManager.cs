using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MileageServer
{
    public static class DataManager
    {
        // gets the current list of entries and formats it as a table
        public static string getCurrentEntries(string username)
        {
            string returner = "ID\tAMT\tCOMMENT\r\n";
            string[] file;
            try
            {
                file = System.IO.File.ReadAllLines("Data\\" + username + ".current");
            }
            catch (Exception)
            {
                return "Failed to read data file :(\r\nMaybe you don't have any entries yet?\r\n";
            }
            for (int i = 0; i < file.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // this would be the first part of an entry
                    returner += (i/2).ToString() + "\t" + file[i] + "\t";
                }
                else
                {
                    // this would be the second part of an entry (the comment)
                    returner += file[i] + "\r\n";
                }
            }
            return returner;
        }

        // returns a string with the current mileage and expense totals
        public static string getCurrentTotals(string username)
        {
            string returner = "\r\n";
            string[] file;
            try
            {
                file = System.IO.File.ReadAllLines("Data\\" + username + ".current");
            }
            catch (Exception)
            {
                return "Failed to read data file :(\r\nMaybe you don't have any entries yet?\r\n";
            }
            int mlz = 0;
            int exp = 0;
            for (int i = 0; i < file.Length; i++)
            {
                if (i % 2 == 0)
                {
                    if (file[i].StartsWith("$"))
                        exp += Int16.Parse(file[i].TrimStart('$'));
                    else
                        mlz += Int16.Parse(file[i]);
                }
            }
            returner += "$" + exp.ToString() + ", " + mlz + " mi.\r\n\r\n";
            return returner;
        }

        // gets the archived entries formatted as a table
        public static string getArchive(string username)
        {
            string returner = "TOTALS\t\tCOMMENT\r\n";
            string[] file;
            try
            {
                file = System.IO.File.ReadAllLines("Data\\" + username + ".archive");
            }
            catch (Exception)
            {
                return "Failed to read data file :(\r\nMaybe you don't have any entries yet?\r\n";
            }
            for (int i = 0; i < file.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // this would be the first part of an entry
                    returner += file[i] + "\t";
                }
                else
                {
                    // this would be the second part of an entry (the comment)
                    returner += file[i] + "\r\n";
                }
            }
            return returner;
        }

        // deletes a mileage or expense entry
        public static bool deleteEntry(string username, int entryID)
        {
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines("Data\\" + username + ".current");
                System.IO.StreamWriter file = new System.IO.StreamWriter("Data\\" + username + ".current", false);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i != entryID * 2 && i != (entryID * 2) + 1)
                    {
                        file.WriteLine(lines[i]);
                    }
                }
                file.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static bool endCurrentPeriod(string username, string name)
        {
            try
            {
                string clean = "";
                string[] lines;

                lines = System.IO.File.ReadAllLines("Data\\" + username + ".current");
                int mlz = 0;
                int exp = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (lines[i].StartsWith("$"))
                            exp += Int16.Parse(lines[i].TrimStart('$'));
                        else
                            mlz += Int16.Parse(lines[i]);
                    }
                }
                clean += "$" + exp + ", " + mlz + " mi.";

                System.IO.StreamWriter file = new System.IO.StreamWriter("Data\\" + username + ".archive", true);
                file.WriteLine(clean);
                file.WriteLine(name);
                file.Close();
                file = new System.IO.StreamWriter("Data\\" + username + ".current", false);
                file.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
