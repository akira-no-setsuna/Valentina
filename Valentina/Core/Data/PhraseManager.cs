using System;
using System.IO;
using System.Text.Json;

namespace Valentina.Core.Data;

public class PhraseManager
{
    private string _configBasePath;
    public PhrasesConfig Config { get; private set; } = new();

    public void Start()
    {
        try
        {
            _configBasePath = Path.Combine(AppContext.BaseDirectory, "Content", "Configs");
            var json = LoadConfig("phrases.json");
            
            if (!string.IsNullOrEmpty(json))
                Config = JsonSerializer.Deserialize<PhrasesConfig>(json) ?? new PhrasesConfig();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[PhraseManager] Error: {e.Message}");
        }
    }

    private string LoadConfig(string fileName)
    {
        var fullPath = Path.Combine(_configBasePath, fileName);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
    }
}