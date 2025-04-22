// Copyright (c) Hugues Valois. All rights reserved.
// Licensed under the MIT license. See LICENSE in the project root for license information.

namespace Woohoo.ChecksumCalculator.Cli;

using Woohoo.Security.Cryptography;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Must pass file path to calculate hashes for.");
            return;
        }

        var filePath = args[0];
        var fileInfo = new FileInfo(filePath);
        var hashNames = new string[] { "CRC32", "MD5", "SHA1" };
        var calculator = new HashCalculator();
        var result = calculator.Calculate(hashNames, filePath);
        Console.WriteLine($"Name: {Path.GetFileName(filePath)}");
        Console.WriteLine($"Size: {fileInfo.Length}");
        foreach (var hash in result.Checksums)
        {
            var hashVal = HashCalculator.HexToString(hash.Value);
            Console.WriteLine($"{hash.Key}: {hashVal}");
        }
    }
}
