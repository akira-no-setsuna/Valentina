using System.Collections.Generic;

namespace Valentina.Core.Data;

public class PhrasesConfig
{
    public List<string> IntroPhrases { get; set; } = new();
    public List<string> PredictionPhrases { get; set; } = new();
    public List<string> AngryPhrases { get; set; } = new();
    public List<string> OffendedPhrases { get; set; } = new();
}