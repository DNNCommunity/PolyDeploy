﻿using Cantarus.Libraries.Encryption;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace DeployClient
{
    class Program
    {
        private static bool IsSilent = false;

        enum ExitCode : int
        {
            Success = 0,
            Error = 1,
            NoModulesFound = 2,
            UserExit = 3,
            InstallFailure = 4
        }

        static void Main(string[] args)
        {
            try
            {
                foreach (string arg in args)
                {
                    if (arg.ToLower().Contains("--silent"))
                    {
                        IsSilent = true;
                    }
                }

                // Output start.
                WriteLine("*** Polly Deployment Client ***");
                WriteLine();

                // Get the properties we need.
                string targetUri = Properties.Settings.Default.TargetUri;
                string apiKey = Properties.Settings.Default.APIKey;
                string encryptionKey = Properties.Settings.Default.EncryptionKey;

                // Do we have a target uri.
                if (targetUri.Equals(string.Empty))
                {
                    throw new Exception("No target uri has been set.");
                }

                // Do we have an api key?
                if (apiKey.Equals(string.Empty))
                {
                    throw new Exception("No api key has been set.");
                }

                // Do we have an encryption key?
                if (encryptionKey.Equals(string.Empty))
                {
                    throw new Exception("No encryption key has been set.");
                }

                // Output identifying module archives.
                WriteLine("Identifying module archives...");

                // Read zip files in current directory.
                string currentDirectory = Directory.GetCurrentDirectory();
                List<string> zipFiles = new List<string>(Directory.GetFiles(currentDirectory, "*.zip"));

                // Is there something to do?
                if (zipFiles.Count <= 0)
                {
                    // No, exit.
                    WriteLine("No module archives found.");
                    WriteLine("Exiting.");
                    ReadLine();
                    Environment.Exit((int)ExitCode.NoModulesFound);
                }

                // Inform user of modules found.
                WriteLine(string.Format("Found {0} module archives in {1}:", zipFiles.Count, currentDirectory));

                foreach (string zipFile in zipFiles)
                {
                    WriteLine(string.Format("\t{0}. {1}", zipFiles.IndexOf(zipFile) + 1, Path.GetFileName(zipFile)));
                }
                WriteLine();

                // Prompt to continue.
                WriteLine("Would you like to continue? (y/n)");

                // Continue?
                if (!Confirm())
                {
                    // No, exit.
                    WriteLine("Exiting.");
                    Environment.Exit((int)ExitCode.UserExit);
                }
                WriteLine();

                // Inform user of encryption.
                WriteLine("Starting encryption...");

                List<KeyValuePair<string, Stream>> encryptedStreams = new List<KeyValuePair<string, Stream>>();

                foreach (string zipFile in zipFiles)
                {
                    using (FileStream fs = new FileStream(zipFile, FileMode.Open))
                    {
                        encryptedStreams.Add(new KeyValuePair<string, Stream>(Path.GetFileName(zipFile), Crypto.Encrypt(fs, Properties.Settings.Default.EncryptionKey)));
                    }
                    WriteLine(string.Format("\tEncrypting {0}", Path.GetFileName(zipFile)));
                }
                WriteLine();

                Dictionary<string, dynamic> results = API.CIInstall(encryptedStreams).Result;

                ArrayList installed = results.ContainsKey("Installed") ? results["Installed"] : null;
                ArrayList failed = results.ContainsKey("Failed") ? results["Failed"] : null;

                // Any failures?
                if (failed.Count > 0)
                {
                    WriteLine(string.Format("{0}/{1} module archives failed to install.", failed.Count, encryptedStreams.Count));
                    ReadLine();
                    Environment.Exit((int)ExitCode.InstallFailure);
                }

                // Output result
                WriteLine(string.Format("{0}/{1} module archives installed successfully.", installed.Count, encryptedStreams.Count));
                ReadLine();

                foreach (KeyValuePair<string, Stream> keyValuePair in encryptedStreams)
                {
                    keyValuePair.Value.Dispose();
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex.Message);
                ReadLine();
                Environment.Exit((int)ExitCode.Error);
            }
        }

        private static void WriteLine(string message = "")
        {
            if(IsSilent)
            {
                return;
            }

            Console.WriteLine(message);
        }

        private static string ReadLine()
        {
            if (IsSilent)
            {
                return null;
            }

            return Console.ReadLine();
        }

        private static bool Confirm()
        {
            if (IsSilent)
            {
                return true;
            }

            char ch = Console.ReadKey(true).KeyChar;

            if (ch.Equals('y') || ch.Equals('Y'))
            {
                return true;
            }

            return false;
        }
    }
}