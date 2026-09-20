namespace Eval3Lib2
{
	using System;
	
	internal class Parser
	{
		//private Tokenizer mTokenizer;
		public Evaluator mEvaluator;
		public Parser(Evaluator evaluator)
		{
			//赋值，没用到
			mEvaluator = evaluator;
		}

		//
		//返回计算好的字符串
		//
		public String Parse(string str)
		{
			if (str == null)
			{
				throw new Exception("字符串是空值");
				return "null";
			}

			Tokenizer.NextToken();
			//Tokenizer.TokenizerSet1(str);
			Tokenizer.TokenizerSet(mEvaluator.mEnvironmentFunctionsList,str);
			//Tokenizer.
			//mTokenizer = new Tokenizer(this, str);//赋初值
			String sCalculation = Tokenizer.NextToken();//开始计算
			return sCalculation;
		}

		private static int ParamCompatibility(object value, Type type)
		{
			// This function returns a score 1 to 10 to this question
			// Can this value fit into this type ?
			if (value == null)
			{
				if (object.ReferenceEquals(type, typeof(object)))
					return 10;
				if (object.ReferenceEquals(type, typeof(string)))
					return 8;
				return 5;
			}
			else if (object.ReferenceEquals(type, value.GetType()))
			{
				return 10;
			}
			else
			{
				return 5;
			}
		}
	}
}