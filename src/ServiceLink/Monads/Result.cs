﻿using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ServiceLink.Monads
{
    [JsonConverter(typeof(ResultJsonConverter))]
    public abstract class Result<T> : IEquatable<Result<T>>
    {
        private Result()
        {
        }

        public bool Equals(Result<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return GuardedEquals(other);
        }

        public TR Match<TR>(Func<T, TR> success, Func<Exception, TR> error)
        {
            switch (this)
            {
                case Error er:
                    return error(er.Value);
                case Success s:
                    return success(s.Value);
            }
            throw new InvalidOperationException($"Unknown result type {GetType()}");
        }

        public void Match(Action<T> success, Action<Exception> error)
        {
            switch (this)
            {
                case Error er:
                    error(er.Value);
                    return;
                case Success s:
                    success(s.Value);
                    return;
            }
            throw new InvalidOperationException($"Unknown result type {GetType()}");
        }

        private bool GuardedEquals(Result<T> other)
        {
            switch (other)
            {
                case Success s:
                    return this is Success ts && Equals(s.Value, ts.Value);
                case Error e:
                    return this is Error te && Equals(e.Value, te.Value);
                default:
                    throw new InvalidOperationException("Unknown result subtype detected {other.GetType()}");
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return GuardedEquals((Result<T>) obj);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() * 29 + (this is Success s
                       ? s.Value?.GetHashCode() ?? 0
                       : ((Error) this).Value.GetHashCode());
        }

        public static bool operator ==(Result<T> left, Result<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Result<T> left, Result<T> right)
        {
            return !Equals(left, right);
        }

        public sealed class Success : Result<T>
        {
            public Success(T value)
            {
                Value = value;
            }

            public T Value { get; }

            public override string ToString()
            {
                return $"{nameof(Success)}({Value})";
            }
        }

        public sealed class Error : Result<T>
        {
            public Error([NotNull] Exception error)
            {
                Value = error ?? throw new ArgumentNullException(nameof(error));
            }

            [NotNull]
            public Exception Value { get; }

            public override string ToString()
            {
                return $"{nameof(Error)}({Value})";
            }
        }
    }

    public static class Result
    {
        public static Result<T> ToSuccess<T>(this T value)
        {
            return new Result<T>.Success(value);
        }

        public static Result<T> ToError<T>(this Exception error)
        {
            return new Result<T>.Error(error);
        }


        [Pure]
        public static Result<TResult> Select<TSource, TResult>(this Result<TSource> result,
            Func<TSource, TResult> mapper)
        {
            return result.Match(s => Try(s, mapper), e => e.ToError<TResult>());
        }


        [Pure]
        public static Result<TResult> SelectMany<TSource, TSelector, TResult>(this Result<TSource> result,
            Func<TSource, Result<TSelector>> selector,
            Func<TSource, TSelector, TResult> resultSelector)
        {
            if (result is Result<TSource>.Success s)
                try
                {
                    var r1 = selector(s.Value);
                    if (r1 is Result<TSelector>.Success s1)
                        try
                        {
                            return resultSelector(s.Value, s1.Value).ToSuccess();
                        }
                        catch (Exception ex1)
                        {
                            return ex1.ToError<TResult>();
                        }
                    return ((Result<TSelector>.Error) r1).Value.ToError<TResult>();
                }
                catch (Exception ex)
                {
                    return ex.ToError<TResult>();
                }
            return ((Result<TSource>.Error) result).Value.ToError<TResult>();
        }

        [Pure]
        public static Result<TResult> SelectMany<TSource, TResult>(this Result<TSource> result,
            Func<TSource, Result<TResult>> selector)
        {
            return SelectMany(result, selector, (_, p) => p);
        }

        [Pure]
        public static Result<T> Wrap<T>(T val)
        {
            return new Result<T>.Success(val);
        }

        [Pure]
        public static Result<T> WrapError<T>([NotNull] Exception ex)
        {
            return new Result<T>.Error(ex);
        }

        [Pure]
        public static Result<T> Try<T>(Func<T> op)
        {
            try
            {
                return op().ToSuccess();
            }
            catch (Exception ex)
            {
                return ex.ToError<T>();
            }
        }

        [Pure]
        public static Result<T> Try<T>(Func<Result<T>> op)
        {
            try
            {
                return op();
            }
            catch (Exception ex)
            {
                return ex.ToError<T>();
            }
        }


        [Pure]
        public static Result<T1> Try<T, T1>(this T value, Func<T, T1> map)
        {
            return Try(() => map(value));
        }


        [Pure]
        public static Result<TResult> Map<TSource, TResult>(this Result<TSource> result, Func<TSource, TResult> mapper)
        {
            return Select(result, mapper);
        }

        [Pure]
        public static Result<TResult> Bind<TSource, TResult>(this Result<TSource> result,
            Func<TSource, Result<TResult>> mapper)
        {
            return SelectMany(result, mapper);
        }


        public static T Unwrap<T>(this Result<T> result)
        {
            return result.Match(p => p, ex => throw ex);
        }

        [Pure]
        public static T Unwrap<T>(this Result<T> result, T @default)
        {
            return result.Match(p => p, _ => @default);
        }

        [Pure]
        public static T Unwrap<T>(this Result<T> result, Func<Exception, T> func)
        {
            return result.Match(p => p, func);
        }

        [Pure]
        public static Result<T> Correct<T, TE>(this Result<T> result, Func<TE, T> func)
            where TE : Exception
        {
            return result is Result<T>.Error err && err.Value is TE tex ? Try(() => func(tex)) : result;
        }

        [Pure]
        public static Result<T> MapError<T, TE>(this Result<T> result, Func<TE, Exception> func)
            where TE : Exception
        {
            return result is Result<T>.Error err && err.Value is TE tex ? Try(() => func(tex).ToError<T>()) : result;
        }

        [Pure]
        public static Result<T> MapOrCorrect<T, TE>(this Result<T> result, Func<TE, Result<T>> func)
            where TE : Exception
        {
            return result is Result<T>.Error err && err.Value is TE tex ? Try(() => func(tex)) : result;
        }

        [Pure]
        public static Result<T> OrElse<T>(Result<T> result, Func<Result<T>> alternative)
        {
            return result is Result<T>.Error ? Try(alternative) : result;
        }

        [Pure]
        public static Result<T> OrElse<T>(Result<T> result, Func<T> alternative)
        {
            return result is Result<T>.Error ? Try(alternative) : result;
        }

        [Pure]
        public static Result<T2> AndThen<T1, T2>(Result<T1> result, Func<T1, T2> func)
        {
            return result.Map(func);
        }

        [Pure]
        public static Result<T2> AndThen<T1, T2>(Result<T1> result, Func<T1, Result<T2>> func)
        {
            return result.Bind(func);
        }
    }
}