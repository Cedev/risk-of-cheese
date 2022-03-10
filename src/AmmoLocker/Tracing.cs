using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Linq.Expressions;
using System;
using System.Linq;
using System.Collections.Generic;

#if DEBUG
namespace CheesyBuffs
{

    public static class Tracing
    {

        public static TDelegate Trace<TDelegate>(ITrace tracer)
        {
            var methodInfo = typeof(TDelegate).GetMethod("Invoke");
            var delegateParams = methodInfo.GetParameters();

            var eParams = delegateParams.Select(x => Expression.Parameter(x.ParameterType, x.Name)).ToArray();
            var eParams1 = eParams.Skip(1).ToArray();

            var eTracer = Expression.Constant(tracer);

            var mEnter = typeof(ITrace).GetMethod("Enter");
            var mException = typeof(ITraceScope).GetMethod("Exception");
            var mReturn = typeof(ITraceScope).GetMethod("Return");
            var mDispose = typeof(IDisposable).GetMethod("Dispose");

            ParameterExpression eScope = Expression.Variable(typeof(ITraceScope), "scope");

            Expression innerBody = Expression.Invoke(eParams[0], eParams1);
            Expression body;
            if (methodInfo.ReturnType != typeof(void))
            {
                var eRet = Expression.Variable(methodInfo.ReturnType, "result");
                innerBody = Expression.Block(
                    Sanity.Vars(eRet),
                    Expression.Assign(eRet, innerBody),
                    Expression.Call(eScope, mReturn, eRet.ConvertTo<object>()),
                    eRet);
            }

            ParameterExpression eException = Expression.Parameter(typeof(Exception), "e");

            body = Expression.Block(
                Sanity.Vars(eScope),
                Expression.Assign(eScope, Expression.Call(eTracer, mEnter, eParams[0], eParams1.ToArrayExpression<object>())),
                Expression.TryCatchFinally(
                    innerBody,
                    Expression.Call(eScope, mDispose),
                    Expression.MakeCatchBlock(typeof(Exception), eException, Expression.Block(
                        Expression.Call(eScope, mException, eException),
                        Expression.Throw(eException, methodInfo.ReturnType)
                        ), Expression.Constant(true)))
                );

            var eLambda = Expression.Lambda<TDelegate>(body, eParams);
            return eLambda.Compile();
        }
    }

    public static class Sanity
    {
        public static ParameterExpression[] Vars(params ParameterExpression[] vars)
        {
            return vars;
        }

        public static Expression ToArrayExpression<T>(this IEnumerable<Expression> expressions)
        {
            return Expression.NewArrayInit(typeof(T), expressions.Select(ConvertTo<T>));
        }

        public static Expression ConvertTo<T>(this Expression expr)
        {
            return Expression.Convert(expr, typeof(T));
        }
    }


    public interface ITraceScope : IDisposable
    {
        public void Exception(Exception e);
        public void Return(object obj);
    }

    public interface ITrace
    {
        public ITraceScope Enter(object target, params object[] args);
    }

    public class TraceScope : ITraceScope
    {
        private readonly Action onExit;
        private readonly Func<object, object> formatter;
        private readonly string spaces;
        private readonly object target;
        private readonly object[] args;

        public TraceScope(Action onExit, Func<object, object> formatter, int indentation, object target, object[] args)
        {
            this.onExit = onExit;
            this.formatter = formatter;
            this.spaces = new string(' ', 2 * indentation);
            this.target = target;
            this.args = args;

            Log.LogDebug(string.Format("{0}Enter {1}({2})", spaces, target, string.Join(", ", args.Select(formatter))));
        }

        public void Dispose()
        {
            Log.LogDebug(string.Format("{0}Exit {1}({2})", spaces, target, string.Join(", ", args.Select(formatter))));
            this.onExit();
        }

        public void Exception(Exception e)
        {
            Log.LogDebug(e);
        }

        public void Return(object obj)
        {
            Log.LogDebug(string.Format("{0}Return: {1}", spaces, formatter(obj)));
        }


    }

    public class NullScope : ITraceScope
    {
        void ITraceScope.Exception(Exception e) { }
        void ITraceScope.Return(object obj) { }
        void IDisposable.Dispose() { }

        public static NullScope Instance = new NullScope();
    }

    public class Tracer : ITrace
    {
        public static Tracer Instance = new Tracer();
        public int indentation;
        public List<Func<object, bool>> Filters;
        public List<Func<object, object>> Formatters;

        public Tracer()
        {
            indentation = 0;
            Filters = new List<Func<object, bool>>();
            Formatters = new List<Func<object, object>>();
        }        

        public ITraceScope Enter(object target, params object[] args)
        {
            if (args.Any(x => Filters.Any(f => f(x))))
            {
                indentation++;
                return new TraceScope(() => { indentation--; }, FormatArg, indentation, target, args);
            } else
            {
                return NullScope.Instance; 
            }
        }

        public object FormatArg(object arg)
        {
            if (arg == null)
            {
                return "null";
            }
            foreach (var formatter in Formatters)
            {
                var result = formatter(arg);
                if (result != null)
                {
                    return result;
                }
            }
            return arg;
        }

        public void Format<T>(Func<T, object> formatter)
        {
            Formatters.Add(x => x is T ? formatter((T) x) : null);
        }

        public void Filter<T>(Func<T, bool> filter)
        {
            Filters.Add(x => x is T ? filter((T)x) : false);
        }
    }
}
#endif
