namespace Nancy.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Nancy.Extensions;

    /// <summary>
    /// Default implementation of a route pattern matcher.
    /// </summary>
    public class DefaultRoutePatternMatcher : IRoutePatternMatcher
    {
        private readonly ConcurrentDictionary<string, Regex> matcherCache = new ConcurrentDictionary<string, Regex>();

        /// <summary>
        /// Attempts to match a requested path with a route pattern.
        /// </summary>
        /// <param name="requestedPath">The path that was requested.</param>
        /// <param name="routePath">The route pattern that the requested path should be attempted to be matched with.</param>
        /// <param name="context">The <see cref="NancyContext"/> instance for the current request.</param>
        /// <returns>An <see cref="IRoutePatternMatchResult"/> instance, containing the outcome of the match.</returns>
        public IRoutePatternMatchResult Match(string requestedPath, string routePath, NancyContext context)
        {
            var routePathPattern = 
                this.matcherCache.GetOrAdd(routePath, s => BuildRegexMatcher(routePath));

            requestedPath = 
                TrimTrailingSlashFromRequestedPath(requestedPath);

            var matches = routePathPattern
                .Match(requestedPath)
                .Groups.Cast<Group>()
                .Where(x => x.Success)
                .ToList();

            return new RoutePatternMatchResult(
                matches.Any(),
                GetParameters(routePathPattern, matches),
                context);
        }

        public IRoutePatternMatchResult Match(string requestedPath, string routePath, IEnumerable<string> segments, NancyContext context)
        {
            var routePathPattern =
                this.matcherCache.GetOrAdd(routePath, s => BuildRegexMatcher(segments));

            requestedPath =
                TrimTrailingSlashFromRequestedPath(requestedPath);

            var matches = routePathPattern
                .Match(requestedPath)
                .Groups.Cast<Group>()
                .Where(x => x.Success)
                .ToList();

            return new RoutePatternMatchResult(
                matches.Any(),
                GetParameters(routePathPattern, matches),
                context);
        }

        private static string TrimTrailingSlashFromRequestedPath(string requestedPath)
        {
            if (!requestedPath.Equals("/"))
            {
                requestedPath = requestedPath.TrimEnd('/');
            }

            return requestedPath;
        }

        private static Regex BuildRegexMatcher(string path)
        {
            var segments =
                GetSegments(path);

            var parameterizedSegments =
                GetParameterizedSegments(segments);

            var pattern =
                string.Concat(@"^/", string.Join("/", parameterizedSegments), @"$");

            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private static Regex BuildRegexMatcher(IEnumerable<string> segments)
        {
            var parameterizedSegments =
                GetParameterizedSegments(segments);

            var pattern =
                string.Concat(@"^/", string.Join("/", parameterizedSegments), @"$");

            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        
        private static DynamicDictionary GetParameters(Regex regex, IList<Group> matches)
        {
            dynamic data = new DynamicDictionary();

            for (var i = 1; i <= matches.Count() - 1; i++)
            {
                data[regex.GroupNameFromNumber(i)] = matches[i].Value;
            }

            return data;
        }

        private static IEnumerable<string> GetParameterizedSegments(IEnumerable<string> segments)
        {
            foreach (var segment in segments)
            {
                var current = segment;

                if (current.IsParameterized())
                {
                    current = ParameterizeSegment(segment);
                }
                else
                {
                    current = (!IsRegexSegment(current)) ? Regex.Escape(current) : current;
                }

                yield return current;
            }
        }

        //private static IEnumerable<string> GetSegments(string path)
        //{
        //    var currentSegment = string.Empty;
        //    var openingParenthesesCount = 0;

        //    for (var index = 0; index < path.Length; index++)
        //    {
        //        var token =
        //            path[index];

        //        if (token.Equals('('))
        //        {
        //            openingParenthesesCount++;
        //        }

        //        if (token.Equals(')'))
        //        {
        //            openingParenthesesCount--;
        //        }

        //        if (!token.Equals('/') || openingParenthesesCount > 0)
        //        {
        //            currentSegment += token;
        //        }

        //        if ((token.Equals('/') || index == path.Length - 1) && currentSegment.Length > 0 && openingParenthesesCount == 0)
        //        {
        //            yield return currentSegment;
        //            currentSegment = string.Empty;
        //        }
        //    }
        //}

        private const char EscapeCharacter = '~'; // just picked something random

        private const string Separator = "/";

        private const string EscapedSeparator = "~/";

        private static IEnumerable<string> GetSegments(string path)
        {
            var segmentStartIndex = path.StartsWith(Separator) ? Separator.Length : 0;
            var currentIndex = segmentStartIndex;

            var unscapedString = path.Replace(EscapedSeparator, Separator);
            var unescapedStartIndex = segmentStartIndex;
            var unescapedIndex = currentIndex;

            while (currentIndex < path.Length)
            {
                var oldIndex = currentIndex;
                currentIndex = path.IndexOf(Separator, currentIndex, StringComparison.Ordinal);
                unescapedIndex += currentIndex - oldIndex;

                // If we've found an escaped separator then ignore it and move on
                if (currentIndex > 0 && path[currentIndex - 1] == EscapeCharacter)
                {
                    unescapedIndex += Separator.Length - 1;
                    currentIndex += Separator.Length;
                    continue;
                }

                // Not found any more separators, bail out
                if (currentIndex == -1)
                {
                    break;
                }

                yield return unscapedString.Substring(unescapedStartIndex, unescapedIndex - unescapedStartIndex);

                currentIndex += Separator.Length;
                segmentStartIndex = currentIndex;

                unescapedIndex += Separator.Length;
                unescapedStartIndex = unescapedIndex;
            }

            // Return the last part of the string
            if (segmentStartIndex < path.Length)
            {
                yield return unscapedString.Substring(unescapedStartIndex);
            }
        }

        private static bool IsRegexSegment(string segment)
        {
            return segment.StartsWith("(");
        }

        private static string ParameterizeSegment(string segment)
        {
            segment = segment.Replace(".", @"\.");

            foreach (var name in segment.GetParameterNames())
            {
                var replacement =
                    string.Format(CultureInfo.InvariantCulture, @"(?<{0}>.+?)", name);

                segment = segment.Replace(
                    string.Concat("{", name, "}"),
                    replacement);
            }

            return segment;
        }
    }

    public interface IRoutePatternSplitter
    {
        IEnumerable<string> Split(string path);
    }

    public class DefaultRoutePatternSplitter : IRoutePatternSplitter
    {
        private const char EscapeCharacter = '~'; // just picked something random

        private const string Separator = "/";

        private const string EscapedSeparator = "~/";

        public IEnumerable<string> Split(string path)
        {
            var segmentStartIndex = path.StartsWith(Separator) ? Separator.Length : 0;
            var currentIndex = segmentStartIndex;

            var unscapedString = path.Replace(EscapedSeparator, Separator);
            var unescapedStartIndex = segmentStartIndex;
            var unescapedIndex = currentIndex;

            while (currentIndex < path.Length)
            {
                var oldIndex = currentIndex;
                currentIndex = path.IndexOf(Separator, currentIndex, StringComparison.Ordinal);
                unescapedIndex += currentIndex - oldIndex;

                // If we've found an escaped separator then ignore it and move on
                if (currentIndex > 0 && path[currentIndex - 1] == EscapeCharacter)
                {
                    unescapedIndex += Separator.Length - 1;
                    currentIndex += Separator.Length;
                    continue;
                }

                // Not found any more separators, bail out
                if (currentIndex == -1)
                {
                    break;
                }

                yield return unscapedString.Substring(unescapedStartIndex, unescapedIndex - unescapedStartIndex);

                currentIndex += Separator.Length;
                segmentStartIndex = currentIndex;

                unescapedIndex += Separator.Length;
                unescapedStartIndex = unescapedIndex;
            }

            // Return the last part of the string
            if (segmentStartIndex < path.Length)
            {
                yield return unscapedString.Substring(unescapedStartIndex);
            }
        }
    }
}
