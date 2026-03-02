namespace EmailAgent.Worker.Models;

public class IntentResult
{
    public FintechEmailIntent Intent { get; set; }
    public string Label => Intent.ToString();
    public int Score { get; set; }
    public double Confidence { get; set; }
}