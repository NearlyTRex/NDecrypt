﻿using System;
using System.IO;
using NDecrypt.Core;
using NDecrypt.N3DS;
using NDecrypt.Nitro;

namespace NDecrypt
{
    class Program
    {
        /// <summary>
        /// Type of the detected file
        /// </summary>
        private enum FileType
        {
            NULL,
            NDS,
            NDSi,
            iQueDS,
            N3DS,
            iQue3DS,
            N3DSCIA,
        }

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                DisplayHelp("Not enough arguments");
                return;
            }

            bool encrypt;
            if (args[0] == "decrypt" || args[0] == "d")
            {
                encrypt = false;
            }
            else if (args[0] == "encrypt" || args[0] == "e")
            {
                encrypt = true;
            }
            else
            {
                DisplayHelp($"Invalid operation: {args[0]}");
                return;
            }

            bool development = false,
                force = false,
                outputHashes = false,
                useAesKeysTxt = false;
            string? keyfile = null;
            int start = 1;
            for (; start < args.Length; start++)
            {
                if (args[start] == "-a" || args[start] == "--aes-keys"
                    || args[start] == "-c" || args[start] == "--citra")
                {
                    useAesKeysTxt = true;
                }
                else if (args[start] == "-dev" || args[start] == "--development")
                {
                    development = true;
                }
                else if (args[start] == "-f" || args[start] == "--force")
                {
                    force = true;
                }
                else if (args[start] == "-h" || args[start] == "--hash")
                {
                    outputHashes = true;
                }
                else if (args[start] == "-k" || args[start] == "--keyfile")
                {
                    if (start == args.Length - 1)
                        Console.WriteLine("Invalid keyfile path: no additional arguments found!");

                    start++;
                    string tempPath = args[start];
                    if (string.IsNullOrWhiteSpace(tempPath))
                        Console.WriteLine($"Invalid keyfile path: null or empty path found!");

                    tempPath = Path.GetFullPath(tempPath);
                    if (!File.Exists(tempPath))
                        Console.WriteLine($"Invalid keyfile path: file {tempPath} not found!");
                    else
                        keyfile = tempPath;
                }
                else
                {
                    break;
                }
            }

            // Derive the keyfile path based on the runtime folder if not already set
            keyfile = DeriveKeyFile(keyfile, useAesKeysTxt);

            // If we are using a Citra keyfile, there are no development keys
            if (development && useAesKeysTxt)
            {
                Console.WriteLine("AES keyfiles don't contain development keys; disabling the option...");
                development = false;
            }

            // Initialize the decrypt args, if possible
            var decryptArgs = new DecryptArgs(keyfile, useAesKeysTxt);

            for (int i = start; i < args.Length; i++)
            {
                if (File.Exists(args[i]))
                {
                    var tool = DeriveTool(args[i], development, decryptArgs);
                    if (tool == null)
                        continue;

                    Console.WriteLine(args[i]);
                    ProcessPath(args[i], tool, encrypt, force);
                    if (outputHashes) WriteHashes(args[i]);
                }
                else if (Directory.Exists(args[i]))
                {
                    foreach (string file in Directory.EnumerateFiles(args[i], "*", SearchOption.AllDirectories))
                    {
                        var tool = DeriveTool(file, development, decryptArgs);
                        if (tool == null)
                            continue;

                        Console.WriteLine(file);
                        ProcessPath(file, tool, encrypt, force);
                        if (outputHashes) WriteHashes(file);
                    }
                }
                else
                {
                    Console.WriteLine($"{args[i]} is not a file or folder. Please check your spelling and formatting and try again.");
                }
            }
        }

        /// <summary>
        /// Display a basic help text
        /// </summary>
        /// <param name="path">Path to the file to process</param>
        /// <param name="tool">Processing tool to use on the file path</param>
        /// <param name="encrypt">Indicates if the file should be encrypted or decrypted</param>
        /// <param name="force">Indicates if the operation should be forced</param>
        private static void ProcessPath(string path, ITool tool, bool encrypt, bool force)
        {
            if (encrypt && !tool.EncryptFile(path, force))
                Console.WriteLine("Encryption failed!");
            else if (!encrypt && !tool.DecryptFile(path, force))
                Console.WriteLine("Decryption failed!");
        }

        /// <summary>
        /// Display a basic help text
        /// </summary>
        /// <param name="err">Additional error text to display, can be null to ignore</param>
        private static void DisplayHelp(string? err = null)
        {
            if (!string.IsNullOrWhiteSpace(err))
                Console.WriteLine($"Error: {err}");

            Console.WriteLine(@"Usage: NDecrypt <operation> [flags] <path> ...

Possible values for <operation>:
e, encrypt - Encrypt the input files
d, decrypt - Decrypt the input files

Possible values for [flags] (one or more can be used):
-a, --aes-keys        Enable using aes_keys.txt instead of keys.bin
-dev, --development   Enable using development keys, if available
-f, --force           Force operation by avoiding sanity checks
-h, --hash            Output size and hashes to a companion file
-k, --keyfile <path>  Path to keys.bin or aes_keys.txt

<path> can be any file or folder that contains uncompressed items.
More than one path can be specified at a time.");
        }

        /// <summary>
        /// Derive the full path to the keyfile, if possible
        /// </summary>
        private static string? DeriveKeyFile(string? keyfile, bool useCitraKeyFile)
        {
            // If a path is passed in
            if (!string.IsNullOrEmpty(keyfile))
            {
                keyfile = Path.GetFullPath(keyfile);
                if (File.Exists(keyfile))
                    return keyfile;
            }

            // Derive the keyfile path based on the runtime folder if not already set
            using var processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            string applicationDirectory = Path.GetDirectoryName(processModule?.FileName) ?? string.Empty;

            // Citra has a unique keyfile format
            if (useCitraKeyFile)
                keyfile = Path.Combine(applicationDirectory, "aes_keys.txt");
            else
                keyfile = Path.Combine(applicationDirectory, "keys.bin");

            // Only return the path if the file exists
            return File.Exists(keyfile) ? keyfile : null;
        }

        /// <summary>
        /// Derive the encryption tool to be used for the given file
        /// </summary>
        /// <param name="filename">Filename to derive the tool from</param>
        /// <param name="development">Indicates if development images are expected</param>
        /// <param name="decryptArgs">Arguments to pass to the tools on creation</param>
        /// <returns></returns>
        private static ITool? DeriveTool(string filename, bool development, DecryptArgs decryptArgs)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"{filename} does not exist! Skipping...");
                return null;
            }

            FileType type = DetermineFileType(filename);
            switch (type)
            {
                case FileType.NDS:
                    Console.WriteLine("File recognized as Nintendo DS");
                    return new DSTool();
                case FileType.NDSi:
                    Console.WriteLine("File recognized as Nintendo DS");
                    return new DSTool();
                case FileType.iQueDS:
                    Console.WriteLine("File recognized as iQue DS");
                    return new DSTool();
                case FileType.N3DS:
                    Console.WriteLine("File recognized as Nintendo 3DS");
                    return new ThreeDSTool(development, decryptArgs);
                case FileType.N3DSCIA:
                    Console.WriteLine("File recognized as Nintendo 3DS CIA [CAUTION: NOT WORKING CURRENTLY]");
                    return new CIATool(development, decryptArgs);
                case FileType.NULL:
                default:
                    Console.WriteLine($"Unrecognized file format for {filename}. Expected *.nds, *.nds.enc, *.srl, *.dsi, *.3ds");
                    return null;
            }
        }

        /// <summary>
        /// Determine the file type from the filename extension
        /// </summary>
        /// <param name="filename">Filename to derive the type from</param>
        /// <returns>FileType value, if possible</returns>
        private static FileType DetermineFileType(string filename)
        {
            if (filename.EndsWith(".nds", StringComparison.OrdinalIgnoreCase)        // Standard carts
                || filename.EndsWith(".nds.enc", StringComparison.OrdinalIgnoreCase) // carts/images with secure area encrypted
                || filename.EndsWith(".srl", StringComparison.OrdinalIgnoreCase))    // Development carts/images
                return FileType.NDS;

            else if (filename.EndsWith(".dsi", StringComparison.OrdinalIgnoreCase))
                return FileType.NDSi;

            else if (filename.EndsWith(".ids", StringComparison.OrdinalIgnoreCase))
                return FileType.iQueDS;

            else if (filename.EndsWith(".3ds", StringComparison.OrdinalIgnoreCase))
                return FileType.N3DS;

            else if (filename.EndsWith(".cia", StringComparison.OrdinalIgnoreCase))
                return FileType.N3DSCIA;

            return FileType.NULL;
        }

        /// <summary>
        /// Write out the hashes of a file to a named file
        /// </summary>
        /// <param name="filename">Filename to get hashes for/param>
        private static void WriteHashes(string filename)
        {
            // If the file doesn't exist, don't try anything
            if (!File.Exists(filename))
                return;

            // Get the hash string from the file
            string? hashString = HashingHelper.GetInfo(filename);
            if (hashString == null)
                return;

            // Open the output file and write the hashes
            using (var fs = File.Create(Path.GetFullPath(filename) + ".hash"))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine(hashString);
            }
        }
    }
}
