namespace pricewatcheruserbot.UserInput;

public class ConsoleUserInput : IUserInput
{
    public Task<string?> RequestAndWait(
        string description,
        string property
    )
    {
        Console.Write("Enter "); Console.Write(description); Console.Write(": ");
        
        var result = Console.ReadLine();
        Console.WriteLine();
        
        return Task.FromResult(result);
    }

    public Task<bool> YesNoWait(string description)
    {
        Console.Write("Enter "); Console.Write(description); Console.Write(" (y/n): ");

        var result = Console.ReadLine();
        Console.WriteLine();
        
        return Task.FromResult(result == "y" || result == "yes");
    }
}