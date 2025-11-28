using SimpleCalculator;

Console.WriteLine("Enter your calculation :");

string? input = Console.ReadLine();
bool isInputValid = CheckUserInput(input);

while (!isInputValid)
{
	isInputValid = CheckUserInput(input);
}

Console.WriteLine(CalculatorService.Evaluate(input!));




static bool CheckUserInput(string? userInput)
{
	if (string.IsNullOrWhiteSpace(userInput) || 
		userInput.ToCharArray().Any(c => !Char.IsNumber(c) && !c.IsMathematicSymbol()))
	{
		Console.WriteLine("Empty or invalid input.");
		return false;
	}
	return true;
}