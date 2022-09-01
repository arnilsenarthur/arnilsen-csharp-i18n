using System;
using System.IO;
using Arnilsen.I18n;

public class Program
{
    public static void Main(string[] args)
    {
        LanguageCompiler compiler = new LanguageCompiler();
        byte[] bytes = compiler.Compile("MediumExample.txt");
        File.WriteAllBytes("MediumExample_.bytes", bytes);

        LanguageParser parser = new LanguageParserFile("MediumExample_.bytes");
        parser.Parse();
        
        parser.LoadSection("");

        Console.WriteLine(parser.GetEntry("menu/leave"));
        Console.WriteLine(parser.GetEntry("menu/leave/sure"));

        Console.WriteLine(parser.GetEntry("entity/dog"));
        Console.WriteLine(parser.GetEntry("entity/dog"));
        Console.WriteLine(parser.GetEntry("entity/dog"));
    } 
}

