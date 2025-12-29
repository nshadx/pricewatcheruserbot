namespace pricewatcheruserbot.UserInput;

public interface IUserInput
{
    Task<string?> RequestAndWait(
        string description,
        string property
    );
}