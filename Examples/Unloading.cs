using System;
using System.IO;
using Arnilsen.I18n;

public class Program
{
    public static void Main(string[] args)
    {
        LanguageCompiler compiler = new LanguageCompiler();
        byte[] bytes = compiler.Compile("Unloading.txt");
        File.WriteAllBytes("Unloading_.bytes", bytes);

        LanguageParser parser = new LanguageParserFile("Unloading_.bytes");
        parser.Parse();
        
        parser.LoadSection("ui");

        Console.WriteLine(parser.GetEntry("ui/main_menu/play"));
        Console.WriteLine(parser.GetEntry("ui/inventory/title"));
    
        parser.UnloadSection("ui", "ui/main_menu");

        Console.WriteLine(parser.GetEntry("ui/main_menu/play"));
        Console.WriteLine(parser.GetEntry("ui/inventory/title"));
    } 
}

