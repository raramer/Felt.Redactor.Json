using Newtonsoft.Json;
using System;
using Xunit;

namespace Felt.Redactor.Json.Tests
{
    public class JsonRedactorRedactTests
    {
        public const string BasicJsonExample =
        @"{
            ""a"": ""1"",
            ""b"": 2
        }";

        /// <summary>
        /// a: string
        /// b: number
        /// c: boolean
        /// d: object
        /// e: property on object
        /// f: object on object
        /// g: array of strings
        /// h: array of objects
        /// i: array of arrays
        /// j: null
        /// </summary>
        public const string ComplexJsonExample =
        @"{
            ""a"": ""A"",
            ""b"": 2,
            ""c"": true,
            ""d"": {
                ""e"": ""EEEEE"",
                ""f"": {
                    ""x"": 6
                },
                ""g"": [ ""GGGGGGG"", ""GGGGGGG"" ],
                ""h"": [
                    { ""y"": 8 },
                    { ""y"": 8 }
                ]
            },
            ""i"": [ [9, 9], [9, 9] ],
            ""j"": null
        }";

        public static object[][] FormattingIs_Data =
        {
            new object[] { JsonRedactorFormatting.Compressed, "{\"a\":\"[REDACTED]\",\"b\":2}" },
            new object[] { JsonRedactorFormatting.Indented, $"{{{Environment.NewLine}  \"a\": \"[REDACTED]\",{Environment.NewLine}  \"b\": 2{Environment.NewLine}}}" }
        };

        [Theory]
        [InlineData(ComplexTypeHandling.RedactValue, "{\"a\":\"A\",\"b\":2,\"c\":true,\"d\":\"[REDACTED]\",\"i\":\"[REDACTED]\",\"j\":null}")]
        [InlineData(ComplexTypeHandling.RedactDescendants, "{\"a\":\"A\",\"b\":2,\"c\":true,\"d\":{\"e\":\"[REDACTED]\",\"f\":{\"x\":\"[REDACTED]\"},\"g\":[\"[REDACTED]\",\"[REDACTED]\"],\"h\":[{\"y\":\"[REDACTED]\"},{\"y\":\"[REDACTED]\"}]},\"i\":[[\"[REDACTED]\",\"[REDACTED]\"],[\"[REDACTED]\",\"[REDACTED]\"]],\"j\":null}")]
        public void ComplexTypeHandlingIs(ComplexTypeHandling complexTypeHandling, string expectedResult)
        {
            var redactor = new JsonRedactor(new RedactorOptions
            {
                ComplexTypeHandling = complexTypeHandling,
                Redacts = new[] { "d", "i" }
            });

            var result = redactor.Redact(ComplexJsonExample);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData(nameof(FormattingIs_Data))]
        public void FormattingIs(JsonRedactorFormatting formatting, string expectedResult)
        {
            var redactor = new JsonRedactor(new JsonRedactorOptions
            {
                Redacts = new[] { "a" },
                Formatting = formatting
            });

            var result = redactor.Redact(BasicJsonExample);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(BasicJsonExample)]
        [InlineData(ComplexJsonExample)]
        public void IsExampleValidJson(string json)
        {
            var redactor = new JsonRedactor();
            Assert.True(redactor.TryRedact(json, out _));
        }

        [Theory]
        [InlineData(null, "{\"a\":null,\"b\":2}")] // null
        [InlineData("", "{\"a\":\"\",\"b\":2}")] // empty
        [InlineData(" ", "{\"a\":\" \",\"b\":2}")] // whitespace
        [InlineData("String", "{\"a\":\"String\",\"b\":2}")] // custom string
        [InlineData("********", "{\"a\":\"********\",\"b\":2}")] // asterisks
        [InlineData("X\bY", "{\"a\":\"X\\bY\",\"b\":2}")] // contains backspace
        [InlineData("X\nY", "{\"a\":\"X\\nY\",\"b\":2}")] // contains newline
        [InlineData("X\rY", "{\"a\":\"X\\rY\",\"b\":2}")] // contains carriage return
        [InlineData("X\tY", "{\"a\":\"X\\tY\",\"b\":2}")] // contains tab
        [InlineData("X\"Y", "{\"a\":\"X\\\"Y\",\"b\":2}")] // contains doublequote
        [InlineData("X\\Y", "{\"a\":\"X\\\\Y\",\"b\":2}")] // contains backslash
        public void MaskIs(string mask, string expectedResult)
        {
            var redactor = new JsonRedactor(new JsonRedactorOptions
            {
                Mask = mask,
                Redacts = new[] { "a" },
            });

            var result = redactor.Redact(BasicJsonExample);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(null)] // null
        [InlineData("")] // empty
        [InlineData(" ")] // whitespace
        [InlineData("abc")] // non-json string
        [InlineData("True")] // non-json boolean
        [InlineData("<xml></xml>")] // xml
        public void OnErrorRedactIsAll(string json)
        {
            var redactor = new JsonRedactor(new RedactorOptions
            {
                OnErrorRedact = OnErrorRedact.All
            });

            var result = redactor.Redact(json);

            Assert.Equal(RedactorOptions.DefaultMask, result);
        }

        [Theory]
        [InlineData(null)] // null
        [InlineData("")] // empty
        [InlineData(" ")] // whitespace
        [InlineData("abc")] // non-json string
        [InlineData("True")] // non-json boolean
        [InlineData("<xml></xml>")] // xml
        public void OnErrorRedactIsNone(string json)
        {
            var redactor = new JsonRedactor(new RedactorOptions
            {
                OnErrorRedact = OnErrorRedact.None
            });

            var result = redactor.Redact(json);

            Assert.Equal(json, result);
        }

        [Fact]
        public void RealWorldExample()
        {
            var json = JsonConvert.SerializeObject(SampleData.UserBillingHistory);
            var redacts = new[]
            {
                "password",
                "passwordHistory",
                "socialSecurityNumber",
            };
            var ifIsRedacts = new[]
            {
                new IfIsRedact { If = "type", Is = "check", Redact = "checkNumber" },
                new IfIsRedact { If = "type", Is = "creditCard", Redact = "creditCardData" },
            };
            var expectedValueRedactions = new[]
            {
                // using "" for string
                @"""P@ssw0rd5""", // password
                @"""P@ssw0rd1""", @"""P@ssw0rd2""", @"""P@ssw0rd3""", // passwordHistory
                @"1234567890", // socialSecurityNumber
                @"""2468""", // checkNumber
                @"""Visa""", @"""4111111111111111""", @"""04/25""", @"""258""", @"false", // creditCardData
            };

            var redactor = new JsonRedactor(new RedactorOptions
            {
                ComplexTypeHandling = ComplexTypeHandling.RedactDescendants,
                Redacts = redacts,
                IfIsRedacts = ifIsRedacts
            });

            var result = redactor.Redact(json);

            var jsonMask = $@"""{RedactorOptions.DefaultMask}""";
            var expectedResult = json;
            foreach (var expectedValueRedaction in expectedValueRedactions)
                expectedResult = expectedResult.Replace(expectedValueRedaction, jsonMask);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("false", "false")] // json boolean
        [InlineData("123", "123")] // json number
        [InlineData("\"abc\"", "\"abc\"")] // json string
        [InlineData(BasicJsonExample, "{\"a\":\"1\",\"b\":2}")] // json object
        [InlineData("[" + BasicJsonExample + "]", "[{\"a\":\"1\",\"b\":2}]")] // json array
        public void StringComparisonIsOrdinal(string json, string expectedResult)
        {
            var redactor = new JsonRedactor(new RedactorOptions
            {
                Redacts = new[] { "A" },
                StringComparison = StringComparison.Ordinal
            });

            var result = redactor.Redact(json);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("false", "false")] // json boolean
        [InlineData("123", "123")] // json number
        [InlineData("\"abc\"", "\"abc\"")] // json string
        [InlineData(BasicJsonExample, "{\"a\":\"[REDACTED]\",\"b\":2}")] // json object
        [InlineData("[" + BasicJsonExample + "]", "[{\"a\":\"[REDACTED]\",\"b\":2}]")] // json array
        public void StringComparisonIsOrdinalIgnoreCase(string json, string expectedResult)
        {
            var redactor = new JsonRedactor(new RedactorOptions
            {
                Redacts = new[] { "A" },
                StringComparison = StringComparison.OrdinalIgnoreCase
            });

            var result = redactor.Redact(json);

            Assert.Equal(expectedResult, result);
        }
    }
}