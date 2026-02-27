namespace EmailAgent.Worker.Models;

public class IntentRule
{
    public string Label { get; set; }
    public List<string> Keywords { get; set; }
    public int Weight { get; set; }
    
}