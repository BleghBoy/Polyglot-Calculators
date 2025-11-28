namespace SimpleCalculator
{
	public static class CalculatorService
	{
		// Evaluate an arithmetic expression: +, -, *, /, ^, parentheses, unary minus
		public static double Evaluate(string expression)
		{
			if (expression is null) throw new ArgumentNullException(nameof(expression));
			var span = expression.AsSpan();

			List<Token> output = new(span.Length / 2); // roughly
			Stack<Token> ops = new();

			Token? prev = null;
			Tokenizer tokenizer = new(span);

			while (tokenizer.TryNext(out var tok))
			{
				switch (tok.Kind)
				{
					case TokenKind.Number:
						output.Add(tok);
						break;

					case TokenKind.Operator:
						// Handle unary minus: treat leading '-' or after '(' or another operator as unary
						if (tok.Op == Operator.Minus && IsUnary(prev))
						{
							// Represent unary minus as a special operator with highest precedence
							tok = Token.UnaryMinus();
						}

						while (ops.Count > 0 && ops.Peek().Kind == TokenKind.Operator)
						{
							var top = ops.Peek();
							if (ShouldPopOperator(top, tok))
							{
								output.Add(ops.Pop());
							}
							else break;
						}
						ops.Push(tok);
						break;

					case TokenKind.LeftParen:
						ops.Push(tok);
						break;

					case TokenKind.RightParen:
						while (ops.Count > 0 && ops.Peek().Kind != TokenKind.LeftParen)
						{
							output.Add(ops.Pop());
						}
						if (ops.Count == 0) throw new FormatException("Mismatched parentheses.");
						ops.Pop(); // discard '('
						break;

					default:
						throw new FormatException("Invalid token.");
				}

				prev = tok;
			}

			while (ops.Count > 0)
			{
				var top = ops.Pop();
				if (top.Kind is TokenKind.LeftParen or TokenKind.RightParen)
					throw new FormatException("Mismatched parentheses.");
				output.Add(top);
			}

			return EvaluateRpn(output);
		}

		private static bool IsUnary(Token? prev)
		{
			// Unary if at start, or after an operator or after '('
			if (prev is null) 
			{
				return true;
			}

			return prev.Value.Kind switch
			{
				TokenKind.Operator => true,
				TokenKind.LeftParen => true,
				_ => false
			};
		}

		private static bool ShouldPopOperator(Token top, Token incoming)
		{
			// Pop while top has higher precedence, or equal precedence and incoming is left-associative
			var topPrec = Precedence(top.Op);
			var inPrec = Precedence(incoming.Op);

			if (top.Kind == TokenKind.Operator && top.Op == Operator.UnaryMinus)
			{
				// Unary minus always pops first (it binds strongest)
				return true;
			}

			if (topPrec > inPrec) return true;
			if (topPrec == inPrec && IsLeftAssociative(incoming.Op)) return true;
			return false;
		}

		private static int Precedence(Operator op) => op switch
		{
			Operator.UnaryMinus => 4,
			Operator.Power => 3,
			Operator.Multiply or Operator.Divide => 2,
			Operator.Plus or Operator.Minus => 1,
			_ => 0
		};

		private static bool IsLeftAssociative(Operator op) => op switch
		{
			Operator.Power => false, // right-associative
			Operator.UnaryMinus => false,
			_ => true
		};

		private static double EvaluateRpn(List<Token> rpn)
		{
			var stack = new Stack<double>();
			foreach (var t in rpn)
			{
				switch (t.Kind)
				{
					case TokenKind.Number:
						stack.Push(t.Number);
						break;

					case TokenKind.Operator:
						if (t.Op == Operator.UnaryMinus)
						{
							if (stack.Count < 1) throw new FormatException("Invalid expression.");
							stack.Push(-stack.Pop());
							break;
						}
						if (stack.Count < 2) throw new FormatException("Invalid expression.");
						var b = stack.Pop();
						var a = stack.Pop();
						stack.Push(t.Op switch
						{
							Operator.Plus => a + b,
							Operator.Minus => a - b,
							Operator.Multiply => a * b,
							Operator.Divide => a / b,
							Operator.Power => Math.Pow(a, b),
							_ => throw new FormatException("Unknown operator.")
						});
						break;

					default:
						throw new FormatException("Invalid RPN token.");
				}
			}
			if (stack.Count != 1) throw new FormatException("Invalid expression.");
			return stack.Pop();
		}

		private enum TokenKind { Number, Operator, LeftParen, RightParen }

		private enum Operator { Plus, Minus, Multiply, Divide, Power, UnaryMinus }

		private readonly struct Token
		{
			public TokenKind Kind { get; }
			public double Number { get; }
			public Operator Op { get; }

			private Token(TokenKind kind, double number, Operator op)
			{
				Kind = kind;
				Number = number;
				Op = op;
			}

			public static Token NumberToken(double value) => new(TokenKind.Number, value, default);
			public static Token OperatorToken(Operator op) => new(TokenKind.Operator, default, op);
			public static Token LeftParen() => new(TokenKind.LeftParen, default, default);
			public static Token RightParen() => new(TokenKind.RightParen, default, default);
			public static Token UnaryMinus() => OperatorToken(Operator.UnaryMinus);
		}

		private ref struct Tokenizer
		{
			private ReadOnlySpan<char> _span;
			private int _i;

			public Tokenizer(ReadOnlySpan<char> span)
			{
				_span = span;
				_i = 0;
			}

			public bool TryNext(out Token token)
			{
				SkipWhitespace();

				if (_i >= _span.Length)
				{
					token = default;
					return false;
				}

				char c = _span[_i];

				// Numbers: support integers, decimals, and leading dot ".5"
				if (char.IsDigit(c) || (c == '.' && _i + 1 < _span.Length && char.IsDigit(_span[_i + 1])))
				{
					int start = _i;
					bool hasDot = c == '.';
					_i++;

					while (_i < _span.Length)
					{
						char n = _span[_i];
						if (char.IsDigit(n))
						{
							_i++;
						}
						else if (n == '.' && !hasDot)
						{
							hasDot = true;
							_i++;
						}
						else
						{
							break;
						}
					}

					double value = ParseDouble(_span.Slice(start, _i - start));
					token = Token.NumberToken(value);
					return true;
				}

				// Operators and parentheses
				switch (c)
				{
					case '+':
						_i++;
						token = Token.OperatorToken(Operator.Plus);
						return true;
					case '-':
						_i++;
						token = Token.OperatorToken(Operator.Minus);
						return true;
					case '*':
						_i++;
						token = Token.OperatorToken(Operator.Multiply);
						return true;
					case '/':
						_i++;
						token = Token.OperatorToken(Operator.Divide);
						return true;
					case '^':
						_i++;
						token = Token.OperatorToken(Operator.Power);
						return true;
					case '(':
						_i++;
						token = Token.LeftParen();
						return true;
					case ')':
						_i++;
						token = Token.RightParen();
						return true;
					default:
						throw new FormatException($"Unexpected character '{c}' at position {_i}.");
				}
			}

			private void SkipWhitespace()
			{
				while (_i < _span.Length && char.IsWhiteSpace(_span[_i])) _i++;
			}

			private static double ParseDouble(ReadOnlySpan<char> s)
			{
				// Avoid allocating substrings; use span parsing
				if (!double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
					throw new FormatException("Invalid number literal.");
				return d;
			}
		}
	}
}