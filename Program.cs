using System;
using System.Reflection;
using System.IO;

namespace samptool
{
    public class Program
    {
        static Assembly Assembly = Assembly.GetExecutingAssembly();
        static string Name = Assembly.GetName().Name;
        static string NameWithExtension = Name + ".exe";

        public static void Main(string[] args)
        {
            // check if req. args can be present
            if (args.Length < 3)
            {
                Console.WriteLine("Error: Not enough arguments were specified.");
                Console.WriteLine();
                DisplayHelp();
                return;
            }

            // check if sound directory exists
            if (!CheckIfDirExists(args[0], "<sounddir>"))
                return;

            // try to load the sample data
            SampleDirectory samples;

            try
            {
                samples = new SampleDirectory(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Failed to load any sound file(s) in sound directory.");
                Console.WriteLine(e.Message);
                Console.ReadKey();
                return;
            }

            if (args[1] == "-x")
            {
                // try to extract the samples
                try
                {
                    samples.ExtractSamples(args[2]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Failed to extract samples.");
                    Console.WriteLine("Detailed error message:" + e.Message);
                    Console.ReadKey();
                    return;
                }
            }
            else if (args[1] == "-u")
            {
                if (!CheckIfDirExists(args[2], "<dir>"))
                    return;

                // get out path
                string outPath;
                bool useOwnPath = args.Length >= 4;
                if (useOwnPath)
                {
                    outPath = args[3];
                }
                else
                {
                    outPath = args[0];
                }

                // try to update the sample data
                // it will fail if no dsp files are present in the folder
                try
                {
                    samples.UpdateSamples(args[2]);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Failed to update sample files.");
                    Console.WriteLine("Detailed error message:" + e.Message);
                    Console.ReadKey();
                    return;
                }

                // update complete
                // try to save the file now
                try
                {
                    if (useOwnPath)
                    {
                        samples.Save(Directory.GetParent(outPath).FullName, Utilities.GetTopLevelDirectoryName(outPath));
                    }
                    else
                    {
                        samples.Save(outPath);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Failed to save the new sample data.");
                    Console.WriteLine("Detailed error message:" + e.Message);
                    Console.ReadKey();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Invalid mode specified: {0}\n", args[1]);
                DisplayHelp();
                return;
            }

            Console.WriteLine("Operation successfully completed. Press any key to continue.");
            Console.ReadKey();
        }

        static bool CheckIfDirExists(string dir, string dirArgName)
        {
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Specified {0} does not exist.\n", dirArgName);
                DisplayHelp();
                return false;
            }
            else
            {
                return true;
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Paper Mario Sample editor v0.1");
            Console.WriteLine("Created by TGE, partially based on the script created by Nisto. ");
            Console.WriteLine("Please give credit where is due.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} <sounddir> -<mode> <dir> [<optarg>]", NameWithExtension);
            Console.WriteLine();
            Console.WriteLine("Where:");
            Console.WriteLine("     <sounddir>  Path to the directory containing the samp and sdir files. (required)");
            Console.WriteLine("     <mode>      Tool mode. See below. (required)");
            Console.WriteLine("     <dir>       Path to directory to load from or write to depending on the mode. (required)");
            Console.WriteLine("     [<optarg>]  Arguments whose usage differs depending on the mode. (optional)");
            Console.WriteLine();
            Console.WriteLine("Modes are:");
            Console.WriteLine("     -x  Extracts the samples to <dir> in DSPADPCM format.");
            Console.WriteLine("     -u  Updates the sdir and samp files using the dsp files in the folder specified in <dir>");
            Console.WriteLine("         and saves the new files to the path specified by [<optargs>] if specified, otherwise");
            Console.WriteLine("         the files are saved to the <sounddir> and the folder name is used as the base file name.");
            Console.WriteLine();
            Console.WriteLine("Example usage:");
            Console.WriteLine("{0} \"D:\\Sound\" -x \"D:\\Sound\\Out\"", NameWithExtension);
            Console.WriteLine("{0} \"D:\\Sound\" -u \"D:\\Sound\\Dspfiles\" \"D:\\Sound\\pmario_new\"", NameWithExtension);
            Console.WriteLine();
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }
    }
}
