using System;
using System.IO;
using Arnilsen.I18n;

public class Program
{
    public static void Main(string[] args)
    {
        LanguageCompiler compiler = new LanguageCompiler();
        byte[] bytes = compiler.Compile("Args.txt");
        File.WriteAllBytes("Args_.bytes", bytes);

        LanguageParser parser = new LanguageParserFile("Args_.bytes");
        parser.Parse();
        
        parser.LoadSection("");

        Console.WriteLine(parser.GetEntry("game/won", "Player", "100 coins"));
    } 
}

