namespace Trident.Abstractions.Profiles;

public record Todo(bool Completed, string Text)
{
    public bool Completed { get; set; } = Completed;
    public string Text { get; set; } = Text;
}