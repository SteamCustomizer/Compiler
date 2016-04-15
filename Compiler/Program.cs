using System;
using System.IO;

using CommandLineParser;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;

using FreeImageAPI;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLineParser.CommandLineParser parser = new CommandLineParser.CommandLineParser();

            SwitchArgument debugFlag = new SwitchArgument('d', "debug", "Enable debug mode", false);
            FileArgument definitionFile = new FileArgument('f', "file", "Path to skin definition file");
            definitionFile.Optional = false;
            DirectoryArgument steamDirectory = new DirectoryArgument('s', "steam", "Path to Steam directory");
            steamDirectory.Optional = false;
            DirectoryArgument baseDirectory = new DirectoryArgument('b', "base", "Path to directory containing skin bases. Defaults to %STEAM_FOLDER%/skins/");
            SwitchArgument nobackupFlag = new SwitchArgument('n', "nobackup", "Backup old skin folder before writing new one", false);
            SwitchArgument activateSkinFlag = new SwitchArgument('a', "activate", "Activate skin after compilation", false);

            //dumbResponse = Array.Exists(args, el => el == "--dumb") || Array.Exists(args, el => el == "-q");

            parser.Arguments.Add(debugFlag);
            parser.Arguments.Add(definitionFile);
            parser.Arguments.Add(steamDirectory);
            parser.Arguments.Add(baseDirectory);
            parser.Arguments.Add(nobackupFlag);
            parser.Arguments.Add(activateSkinFlag);

#if !DEBUG
            try
            {
#endif
                parser.ParseCommandLine(args);
#if !DEBUG
                try
                {
#endif
                    Core.backupEnabled = !nobackupFlag.Value;
                    Core.debugMode = debugFlag.Value;
                    Core.activateSkin = activateSkinFlag.Value;
                    Core.Compile(definitionFile.Value, steamDirectory.Value, baseDirectory.Value);
#if !DEBUG
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Environment.Exit(2);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }
#endif
#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}
