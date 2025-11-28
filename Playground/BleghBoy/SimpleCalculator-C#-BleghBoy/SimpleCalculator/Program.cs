using SimpleCalculator;

Console.WriteLine("Enter your calculation :");

string? input = Console.ReadLine();

CheckUserInput(input);






static void CheckUserInput(string? userInput)
{
	if (string.IsNullOrWhiteSpace(userInput))
	{
		Console.WriteLine("No input provided.");
		return;
	}

	if (userInput.ToCharArray().Any(c => !Char.IsNumber(c) && !c.IsMathematicSymbol()))
	{
		Console.WriteLine("Invalid characters in input.");
		return;
	}
}