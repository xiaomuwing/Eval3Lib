//
//Project：计算组件
//Author：scs
//Date:20200107
//Description:多位（double）、嵌套自定义公式、混合等式,支持+ - * / % （ ）
//Version:1.0
//
//modification: 1.Tokenizer.cs,iNum值域范围设置错误;2.bug：公式后面+空格报错
//author:scs
//date:20200107
//Version:1.1
//
//modification: 1.自定义公式可以传多位参数
//author:scs
//date:20200119
//Version:1.2
//
//modification: 
//1.添加各类报错吧,应用方法Throw new Exception(string ex)
//2.value改成内部变量
//3.反射区打包，增加：打包方法（），反射计算（） 
//author:scs
//date:20200216
//Version:1.3
//
//modification: 1.Tokenizer做成静态类；2.报错提示修改，增加位置提示；
//author:scs
//date:20200117
//Version:1.4
///
//modification: 1.报错提示修改，BUG3处，增加括号提示判断1处；错误呈现形式修改1处；
//author:scs
//date:20200117
//Version:1.4.1
//
//modification: 增加错误判断的范围
//author:scs
//date:20200117
//Version:1.4.2
//
//modification: 修改bug1处；  像 元数据bug(1/2/3)类公式需要慎重,除不尽的情况
//author:scs
//date:20200117
//Version:1.4.3
//

namespace Eval3Lib2
{
	using System;
	using System.Collections;
	public class Evaluator
	{
		internal ArrayList mEnvironmentFunctionsList;//反射数组
		public String sEval3ErrorPrompt;//错误提示
		public Evaluator()
		{
			mEnvironmentFunctionsList = new ArrayList();
		}
		public void AddEnvironmentFunctions(object obj)
		{
			if (obj == null)
				return;
			if (!mEnvironmentFunctionsList.Contains(obj))
			{
				//数组融入
				mEnvironmentFunctionsList.Add(obj);
			}
		}
		public void RemoveEnvironmentFunctions(object obj)
		{
			if (mEnvironmentFunctionsList.Contains(obj))
			{
				mEnvironmentFunctionsList.Remove(obj);
			}
		}
		public String Parse(string str)
		{
			return new Parser(this).Parse(str);
		}
		public string ConvertToString(object value)
		{
			if (value is string)
			{
				return (string)value;
			}
			else if (value == null)
			{
				return string.Empty;
			}
			else if (value is System.DateTime)
			{
				System.DateTime d = (System.DateTime)value;
				if (d.TimeOfDay.TotalMilliseconds > 0)
				{
					return d.ToString();
				}
				else
				{
					return d.ToShortDateString();
				}
			}
			else if (value is decimal)
			{
				decimal d = (decimal)value;
				if ((d % 1) != 0)
				{
					return d.ToString("#,##0.00");
				}
				else
				{
					return d.ToString("#,##0");
				}
			}
			else if (value is double)
			{
				double d = (double)value;
				if ((d % 1) != 0)
				{
					return d.ToString("#,##0.00");
				}
				else
				{
					return d.ToString("#,##0");
				}
			}
			else if (value is object)
			{
				return value.ToString();
			}
			return string.Empty;//函数“ConvertToString”不会在所有代码路径上都返回值。当使用结果时，可能会在运行时发生 null 引用异常。
		}
	}
}