namespace Eval3Lib2
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Reflection;
	using System.Text;
	static public class Tokenizer
	{
		static string mString;//传入的字符串原始数据
		static int mLen;//字符串长度
		static int mPos; //从0开始
		static string mCurChar;
		static ArrayList mParser;

		static readonly DataTable _jiSuan = new DataTable();

		/// <summary>
		/// 1.2的代码
		/// </summary>
		//public StringBuilder Value = new StringBuilder();

		static public void TokenizerSet(ArrayList Parser, string str)
		{
			mString = str;
			mLen = str.Length;
			mPos = 0;
			mParser = Parser;
		}

		//分拆 判断 计算
		//返回结果
		static public string NextToken()
		{
			string sValueLeft;
			int iNum = 0;//计算 （）；

			StringBuilder Value = new StringBuilder();

			//字符串，不是字符串跳过
			do
			{
				NextChar();

				switch (mCurChar)
				{
					//字符串判定a`z,A`Z
					case "a":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo1 = ParseIdentifier();
						if (jieGuo1 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo1);
						break;
					case "b":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo2 = ParseIdentifier();
						if (jieGuo2 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo2);
						break;
					case "c":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo3 = ParseIdentifier();
						if (jieGuo3 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo3); break;
					case "d":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo4 = ParseIdentifier();
						if (jieGuo4 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo4); break;
					case "e":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo5 = ParseIdentifier();
						if (jieGuo5 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo5); break;
					case "f":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo6 = ParseIdentifier();
						if (jieGuo6 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo6); break;
					case "g":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo7 = ParseIdentifier();
						if (jieGuo7 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo7); break;
					case "h":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo8 = ParseIdentifier();
						if (jieGuo8 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo8); break;
					case "i":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo9 = ParseIdentifier();
						if (jieGuo9 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo9); break;
					case "j":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo10 = ParseIdentifier();
						if (jieGuo10 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo10); break;
					case "k":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo11 = ParseIdentifier();
						if (jieGuo11 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo11); break;
					case "l":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo12 = ParseIdentifier();
						if (jieGuo12 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo12); break;
					case "m":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo13 = ParseIdentifier();
						if (jieGuo13 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo13); break;
					case "n":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo14 = ParseIdentifier();
						if (jieGuo14 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo14); break;
					case "o":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo15 = ParseIdentifier();
						if (jieGuo15 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo15); break;
					case "p":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo16 = ParseIdentifier();
						if (jieGuo16 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo16); break;
					case "q":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo17 = ParseIdentifier();
						if (jieGuo17 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo17); break;
					case "r":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo18 = ParseIdentifier();
						if (jieGuo18 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo18); break;
					case "s":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo19 = ParseIdentifier();
						if (jieGuo19 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo19); break;
					case "t":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo20 = ParseIdentifier();
						if (jieGuo20 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo20); break;
					case "u":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo21 = ParseIdentifier();
						if (jieGuo21 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo21); break;
					case "v":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo22 = ParseIdentifier();
						if (jieGuo22 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo22); break;
					case "w":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo23 = ParseIdentifier();
						if (jieGuo23 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo23); break;
					case "x":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo24 = ParseIdentifier();
						if (jieGuo24 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo24); break;
					case "y":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo25 = ParseIdentifier();
						if (jieGuo25 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo25); break;
					case "z":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo26 = ParseIdentifier();
						if (jieGuo26 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo26); break;
					case "A":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo27 = ParseIdentifier();
						if (jieGuo27 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo27); break;
					case "B":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo28 = ParseIdentifier();
						if (jieGuo28 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo28); break;
					case "C":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo29 = ParseIdentifier();
						if (jieGuo29 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo29); break;
					case "D":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo30 = ParseIdentifier();
						if (jieGuo30 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo30); break;
					case "E":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo31 = ParseIdentifier();
						if (jieGuo31 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo31); break;
					case "F":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo32 = ParseIdentifier();
						if (jieGuo32 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo32); break;
					case "G":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo33 = ParseIdentifier();
						if (jieGuo33 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo33); break;
					case "H":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo34 = ParseIdentifier();
						if (jieGuo34 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo34); break;
					case "I":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo35 = ParseIdentifier();
						if (jieGuo35 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo35); break;
					case "J":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo36 = ParseIdentifier();
						if (jieGuo36 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo36); break;
					case "K":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo37 = ParseIdentifier();
						if (jieGuo37 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo37); break;
					case "L":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo38 = ParseIdentifier();
						if (jieGuo38 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo38); break;
					case "M":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo39 = ParseIdentifier();
						if (jieGuo39 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo39); break;
					case "N":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo40 = ParseIdentifier();
						if (jieGuo40 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo40);
						break;
					case "O":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo41 = ParseIdentifier();
						if (jieGuo41 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo41);
						break;
					case "P":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo42 = ParseIdentifier();
						if (jieGuo42 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo42);
						break;
					case "Q":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo43 = ParseIdentifier();
						if (jieGuo43 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo43);
						break;
					case "R":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo44 = ParseIdentifier();
						if (jieGuo44 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo44);
						break;
					case "S":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo45 = ParseIdentifier();
						if (jieGuo45 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo45);
						break;
					case "T":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo46 = ParseIdentifier();
						if (jieGuo46 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo46);
						break;
					case "U":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo47 = ParseIdentifier();
						if (jieGuo47 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo47);
						break;
					case "V":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo48 = ParseIdentifier();
						if (jieGuo48 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo48);
						break;
					case "W":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo49 = ParseIdentifier();
						if (jieGuo49 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo49);
						break;
					case "X":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo50 = ParseIdentifier();
						if (jieGuo50 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo50);
						break;
					case "Y":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo51 = ParseIdentifier();
						if (jieGuo51 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo51);
						break;
					case "Z":
						sValueLeft = Value.ToString();
						Value.Clear();
						String jieGuo52 = ParseIdentifier();
						if (jieGuo52 == "false") { return mString; };
						Value.Append(sValueLeft);
						Value.Append(jieGuo52);
						break;
					case "(":
						iNum++;
						//ParseNumber();
						Value.Append(mCurChar);
						break;
					case ")":
						iNum--;
						//ParseNumber();
						Value.Append(mCurChar);
						if (iNum == -1)
						{
							throw new Exception("多了一个右括号 ')' 位置：" + mPos.ToString());
							//mParser.mEvaluator.sEval3ErrorPrompt = "Program error duo )";
							return mString;
						}
						//错误判定
						break;

					default:
						//ParseNumber();
						Value.Append(mCurChar);
						break;
				}
			} while (mPos < mLen);

			//防错 ,最后一位为符号
			if (mCurChar == "+" || mCurChar == "-" || mCurChar == "*" || mCurChar == "/" || mCurChar == "," || mCurChar == "%")
			{
				throw new Exception("最后一位是符号 位置：" + mPos.ToString());
				return mString;
			}

			//（ ）括号数量不对
			if(iNum!=0)
			{
				throw new Exception("表达式左括号和右括号不对等 位置：" + mPos.ToString());
			}
			//double sCal = Convert.ToDouble(value.ToString());

			String sCal;
			try
			{
				sCal = _jiSuan.Compute(Value.ToString(), "").ToString();
			}
			catch(SyntaxErrorException e)
			{
				throw new Exception(e.Message.ToString()+"位置：" + mPos.ToString());
			}
			

			return sCal;
			//String sCal = _jiSuan.Compute("(2+5)", "").ToString();
			//return sCal;

		}

		static void NextChar()
		{
			if (mPos < mLen)
			{
				mCurChar = mString.Substring(mPos, 1);
				mPos += 1;
			}
			else
			{
				mCurChar = "";
			}
		}
		

		//拆分出字符时调用，当前第一位一定是字母
		//反射
		static String ParseIdentifier()
		{
			String sValueLeft = null;//公式
									 //Double sValueRight=0;
			List<double> sValueRight = new List<double>();
			_ = new ArrayList();

			StringBuilder Value = new StringBuilder();

			//Value.Clear();
			//第一位
			Value.Append(mCurChar);

			bool bValue = true;

			do
			{
				NextChar();

				switch (mCurChar)
				{
					case "(":
						sValueLeft = Value.ToString();
						//Value.Clear();
						//Value.Append(mCurChar);
						ArrayList sParse = ParseString();
						if (sParse[sParse.Count - 1].ToString() == "false")
						{
							return "false";
						}
						else
						//if (sParse.Count > 1)
						{
							//遍历 自定义公式多参数
							for (int i = 0; i < sParse.Count; i++)
							{
								try
								{
									sValueRight.Add(Convert.ToDouble(sParse[i]));
								}
								catch(FormatException e)
								{
									throw new Exception(e.Message.ToString() + "位置：" + mPos.ToString());
								}
								
							}
							bValue = false;//改变状态，跳出循环		
							break;
						}
					//else
					//{
					//	svalueright.add( convert.todouble(sparse[0]));
					//	bvalue = false;//改变状态，跳出循环					
					//	break;
					//}

					case ")":
						//程序大概率错误；公式后面+（ 固定格式 默认不支持公式 "A(x(1,2)"
						throw new Exception("公式中暂不支持有'）'符号: 位置" + mPos.ToString());
						//mParser.mEvaluator.sEval3ErrorPrompt = "Program error duo )";
						return "false";
					case "a": Value.Append(mCurChar); break;
					case "b": Value.Append(mCurChar); break;
					case "c": Value.Append(mCurChar); break;
					case "d": Value.Append(mCurChar); break;
					case "e": Value.Append(mCurChar); break;
					case "f": Value.Append(mCurChar); break;
					case "g": Value.Append(mCurChar); break;
					case "h": Value.Append(mCurChar); break;
					case "i": Value.Append(mCurChar); break;
					case "j": Value.Append(mCurChar); break;
					case "k": Value.Append(mCurChar); break;
					case "l": Value.Append(mCurChar); break;
					case "m": Value.Append(mCurChar); break;
					case "n": Value.Append(mCurChar); break;
					case "o": Value.Append(mCurChar); break;
					case "p": Value.Append(mCurChar); break;
					case "q": Value.Append(mCurChar); break;
					case "r": Value.Append(mCurChar); break;
					case "s": Value.Append(mCurChar); break;
					case "t": Value.Append(mCurChar); break;
					case "u": Value.Append(mCurChar); break;
					case "v": Value.Append(mCurChar); break;
					case "w": Value.Append(mCurChar); break;
					case "x": Value.Append(mCurChar); break;
					case "y": Value.Append(mCurChar); break;
					case "z": Value.Append(mCurChar); break;
					case "A": Value.Append(mCurChar); break;
					case "B": Value.Append(mCurChar); break;
					case "C": Value.Append(mCurChar); break;
					case "D": Value.Append(mCurChar); break;
					case "E": Value.Append(mCurChar); break;
					case "F": Value.Append(mCurChar); break;
					case "G": Value.Append(mCurChar); break;
					case "H": Value.Append(mCurChar); break;
					case "I": Value.Append(mCurChar); break;
					case "J": Value.Append(mCurChar); break;
					case "K": Value.Append(mCurChar); break;
					case "L": Value.Append(mCurChar); break;
					case "M": Value.Append(mCurChar); break;
					case "N": Value.Append(mCurChar); break;
					case "O": Value.Append(mCurChar); break;
					case "P": Value.Append(mCurChar); break;
					case "Q": Value.Append(mCurChar); break;
					case "R": Value.Append(mCurChar); break;
					case "S": Value.Append(mCurChar); break;
					case "T": Value.Append(mCurChar); break;
					case "U": Value.Append(mCurChar); break;
					case "V": Value.Append(mCurChar); break;
					case "W": Value.Append(mCurChar); break;
					case "X": Value.Append(mCurChar); break;
					case "Y": Value.Append(mCurChar); break;
					case "Z": Value.Append(mCurChar); break;
					case "":
						//mParser.mEvaluator.sEval3ErrorPrompt = "Program error string";
						//证明全是字符串,这并不一定代表错误
						return "false";
					default:
						Value.Append(mCurChar);
						break;
				}
			} while (bValue);

			if (mPos == mLen)
			{
				//程序错误 
			}
			if (sValueLeft == null)
			{
				//mParser.mEvaluator.sEval3ErrorPrompt = "Program error sValueLeft==null";
				throw new Exception("公式格式书写有歧义 : 位置" + mPos.ToString());
				//return "false";
			}
			//遍历 反射 
			//判断公式 防止“Abs  ”模式出现,判断最后一位是不是空格
			//sValueLeft
			while (sValueLeft.Substring((sValueLeft.Length - 1), 1) == " ")
			{
				sValueLeft = sValueLeft.Substring((0), sValueLeft.Length - 1);
			}

			//反射结果
			String sEndReslut = ReflectionParse(sValueLeft, sValueRight);

			return sEndReslut;
		}

		//参数：公式，<值>
		static String ReflectionParse(String sValueLeft, List<double> sValueRight)
		{
			String sResult = "false";
			for (int i = 0; i < mParser.Count; i++)
			{
				Type obj_d = mParser[i].GetType();

				Type[] tType = new Type[sValueRight.Count];
				for (int m = 0; m < sValueRight.Count; m++)
				{
					tType[m] = typeof(double);
				}
				MethodInfo method = obj_d.GetMethod(sValueLeft, tType); // 获取方法信息
				if (method != null)
				{
					//1.3
					sResult = Reflection(method, sValueRight, obj_d);
				}
			}
			
			if(sResult=="false")
			{
				throw new Exception("自定义公式不正确，请检查公式和参数: 位置" + mPos.ToString());
			}
			return sResult;
		}

		//参数：MethodInfo方法信息，<值>，方法公式
		public static String Reflection(MethodInfo method, List<double> sValueRight, Type obj_d)
		{

			Object[] oObj = new object[sValueRight.Count];

			for (int n = 0; n < sValueRight.Count; n++)
			{
				oObj[n] = sValueRight[n];
			}
			object[] parameters = oObj;

			String sResult;
			try
			{
				sResult = (method.Invoke(obj_d, parameters)).ToString();
			
			}
			catch(ArgumentOutOfRangeException e)
			{
				throw new Exception(e.Message.ToString()+": 位置" + mPos.ToString());
			}
			catch(TargetInvocationException e)
			{
				throw new Exception(e.Message.ToString() + ": 位置" + mPos.ToString());
			}
			
			return sResult;
		}

		//公式后面的 有（ 号调用 
		static ArrayList ParseString()
		{

			String sValueLeft;
			String sValueRight;
			ArrayList sValueRightList = new ArrayList();
			int iNumDouHao = 0;//计算有多少逗号
			int sNumNote = 1;//计算“（”符号数量,第一位 mPos在上一个方法已经++

			StringBuilder Value = new StringBuilder();
			Value.Append(mCurChar);

			do
			{
				NextChar();

				switch (mCurChar)
				{
					case "(":
						sNumNote++;
						Value.Append(mCurChar);
						break;
					case ")":
						sNumNote--;
						Value.Append(mCurChar);
						try
						{
							Char cJudge = Value[Value.Length - 2];
							if (cJudge == '+' || cJudge == '-' || cJudge == '*' || cJudge == '/' || cJudge == '%')
							{
								sValueRightList.Add("false");
								//mParser.mEvaluator.sEval3ErrorPrompt = "Program error ）";
								throw new Exception("公式末尾有多余符号: 位置" + mPos.ToString());
								//return sValueRightList;
							}
						}
						catch(IndexOutOfRangeException e)
						{
							throw new Exception(e.Message.ToString()+": 位置" + mPos.ToString());
						}
						
						break;
					case ",":
						////////////////!!!////////////////
						if (iNumDouHao == 0)
						{
							Value.Remove(0, 1);
							String sCal1;
							try
							{
								sCal1 = _jiSuan.Compute(Value.ToString(), "").ToString();
							}
							catch (SyntaxErrorException e)
							{
								throw new Exception(e.Message.ToString() + ": 位置" + mPos.ToString());
							}
							
							Value.Clear();
							sValueRightList.Add(sCal1);
							iNumDouHao++;
						}
						else
						{
							String sCal1;
							try
							{
								sCal1 = _jiSuan.Compute(Value.ToString(), "").ToString();
							}
							catch (SyntaxErrorException e)
							{
								throw new Exception(e.Message.ToString() + ": 位置" + mPos.ToString());
							}

							
							Value.Clear();
							sValueRightList.Add(sCal1);
							iNumDouHao++;
						}
						break;
					case "a":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "b":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "c":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "d":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "e":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "f":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "g":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "h":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "i":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "j":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "k":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "l":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "m":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "n":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "o":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "p":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "q":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "r":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "s":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "t":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "u":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "v":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "w":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "x":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "y":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "z":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "A":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "B":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "C":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "D":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "E":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "F":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "G":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "H":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "I":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "J":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "K":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "L":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "M":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "N":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "O":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "P":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "Q":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "R":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "S":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "T":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "U":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "V":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "W":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "X":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "Y":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "Z":
						sValueLeft = Value.ToString();
						sValueRight = ParseIdentifier();
						if (sValueRight == "false") { sValueRightList.Add("false"); return sValueRightList; };
						Value.Clear();
						Value.Append(sValueLeft);
						Value.Append(sValueRight);
						break;
					case "":
						sValueRightList.Add("false");
						throw new Exception("公式缺少 ）: 位置" + mPos.ToString());
					//return sValueRightList;
					default:
						Value.Append(mCurChar);
						break;
				}
			}
			//传进来的第一位 一定是（
			while (!(sNumNote == 0));


			//double dJieGuo = Convert.ToDouble(value.ToString());
			//一个逗号都没有,有多个逗号
			if (iNumDouHao == 0)
			{
				String sCal;
				try
				{
					sCal = _jiSuan.Compute(Value.ToString(), "").ToString();
				}
				catch (SyntaxErrorException e)
				{
					throw new Exception(e.Message.ToString() + ": 位置" + mPos.ToString());
				}

				Value.Clear();
				sValueRightList.Add(sCal);
				return sValueRightList;
			}
			else
			{
				//最后一位
				Value.Remove(Value.Length - 1, 1);
				String sCal1;
				try
				{
					sCal1 = _jiSuan.Compute(Value.ToString(), "").ToString();
				}
				catch(SyntaxErrorException e)
				{
					throw new Exception(e.Message.ToString()+": 位置" + mPos.ToString());
				}
				
				Value.Clear();
				sValueRightList.Add(sCal1);
				return sValueRightList;
			}
		}


	}
}

