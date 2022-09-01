using System;
using System.IO;
using Arnilsen.I18n;

public class Program
{
    public static void Main(string[] args)
    {
        LanguageCompiler compiler = new LanguageCompiler();
        byte[] bytes = compiler.Compile("Test.txt");
        File.WriteAllBytes("Test_.bytes", bytes);

        LanguageParser parser = new LanguageParserFile("Test_.bytes");
        parser.Parse();
        
        parser.LoadSection("");

        Console.WriteLine(parser.GetEntry("menu/leave"));
        Console.WriteLine(parser.GetEntry("menu/leave/sure"));

        Console.WriteLine(parser.GetEntry("entity/dog"));
        Console.WriteLine(parser.GetEntry("entity/dog"));
        Console.WriteLine(parser.GetEntry("entity/dog"));
    } 
}

