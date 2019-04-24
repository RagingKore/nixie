namespace Fixie.Assertions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static System.Environment;

    public static class AssertionExtensions
    {
        public static void ShouldBeGreaterThan(this int actual, int minimum)
        {
            if (actual <= minimum)
                throw new AssertException(ComparisonFailure(actual, minimum, ">"));
        }

        public static void ShouldBeGreaterThanOrEqualTo(this int actual, int minimum)
        {
            if (actual < minimum)
                throw new AssertException(ComparisonFailure(actual, minimum, ">="));
        }

        public static void ShouldBeGreaterThanOrEqualTo(this TimeSpan actual, TimeSpan minimum)
        {
            if (actual < minimum)
                throw new AssertException(ComparisonFailure(actual, minimum, ">="));
        }

        static string ComparisonFailure(object left, object right, string operation)
        {
            return $"Expected: {Format(left)} {operation} {Format(right)}{NewLine}but it was not";
        }

        public static void ShouldBeNull(this object actual)
        {
            actual.ShouldBe(null);
        }

        public static void ShouldBeType<T>(this object actual)
        {
            (actual?.GetType()).ShouldBe(typeof(T));
        }

        public static void ShouldBe<T>(this T actual, T expected)
        {
            if (!Assert.Equal(expected, actual))
                throw new ExpectedException(expected, actual);
        }

        public static void ShouldBe<T>(this IEnumerable<T> actual, params T[] expected)
        {
            actual.ToArray().ShouldBe(expected);
        }

        public static void ShouldBe<T>(this T actual, T expected, string userMessage)
        {
            if (!Assert.Equal(expected, actual))
                throw new ExpectedException(expected, actual, userMessage);
        }

        public static void ShouldNotBeNull<T>(this T @object) where T : class
        {
            @object.ShouldNotBe(null);
        }

        public static void ShouldNotBe<T>(this T actual, T expected)
        {
            if (Assert.Equal(expected, actual))
                throw new AssertException($"Unexpected: {Format(expected)}");
        }

        public static void ShouldBeEmpty<T>(this IEnumerable<T> collection)
        {
            if (collection.Any())
                throw new AssertException("Collection was not empty.");
        }

        static string Format(object value)
        {
            return value?.ToString() ?? "(null)";
        }

        public static TException ShouldThrow<TException>(this Action shouldThrow, string expectedMessage) where TException : Exception
        {
            bool threw = false;
            Exception exception = null;

            try
            {
                shouldThrow();
            }
            catch (Exception actual)
            {
                threw = true;
                actual.ShouldBeType<TException>();
                actual.Message.ShouldBe(expectedMessage);
                exception = actual;
            }

            threw.ShouldBe(true);
            return (TException)exception;
        }
    }
}