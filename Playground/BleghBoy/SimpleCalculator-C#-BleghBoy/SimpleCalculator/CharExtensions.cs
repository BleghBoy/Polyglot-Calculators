namespace SimpleCalculator
{
	public static class CharExtensions
	{
		public static readonly char[] Symbols =
		[
			'+',
			'-',
			'*',
			'/',
			'(',
			')'
		];

		public static bool IsMathematicSymbol(this char c)
		{
			return Symbols.Contains(c);
		}
	}
}