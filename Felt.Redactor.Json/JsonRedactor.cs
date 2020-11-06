using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Felt.Redactor.Json
{
    public sealed class JsonRedactor : RedactorBase, IRedact
    {
        private readonly Formatting _formatting = Formatting.None;
        private readonly Func<string, string> AdjustWhiteSpace = i => i;

        public JsonRedactor() : base(new RedactorOptions())
        {
        }

        public JsonRedactor(params string[] redacts) : base(new RedactorOptions { Redacts = redacts })
        {
        }

        public JsonRedactor(params IfIsRedact[] ifIsRedacts) : base(new RedactorOptions { IfIsRedacts = ifIsRedacts })
        {
        }

        public JsonRedactor(string[] redacts, IfIsRedact[] ifIsRedacts) : base(new RedactorOptions { Redacts = redacts, IfIsRedacts = ifIsRedacts })
        {
        }

        public JsonRedactor(RedactorOptions options) : base(options)
        {
        }

        public JsonRedactor(JsonRedactorOptions options) : base(options)
        {
            switch (options.Formatting)
            {
                case JsonRedactorFormatting.Compressed:
                    _formatting = Formatting.None;
                    break;

                case JsonRedactorFormatting.Indented:
                    _formatting = Formatting.Indented;
                    break;

                case JsonRedactorFormatting.WhiteSpaced:
                    _formatting = Formatting.Indented;
                    AdjustWhiteSpace = i => Regex.Replace(i, @"\s+", " ", RegexOptions.Multiline);
                    break;

                default:
                    throw new InvalidOperationException("Invalid " + nameof(JsonRedactorFormatting));
            }
        }

        public override string Redact(string json)
        {
            return TryRedact(json, out var redactedJson, out _) ? redactedJson
                : _options.OnErrorRedact == OnErrorRedact.None ? json
                : _options.Mask;
        }

        public override bool TryRedact(string json, out string redactedJson)
        {
            return TryRedact(json, out redactedJson, out _);
        }

        public override bool TryRedact(string json, out string redactedJson, out string errorMessage)
        {
            try
            {
                var jToken = JToken.Parse(json);
                RedactJToken(jToken, false, null);
                redactedJson = AdjustWhiteSpace(jToken.ToString(_formatting));
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                redactedJson = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        protected override string SanitizeMask(string mask)
        {
            return mask;
        }

        private void RedactJObject(JObject jObject, bool redacting)
        {
            var properties = jObject.Properties();
            foreach (var next in properties)
            {
                var property = next;

                var redactProperty = redacting // redacting parent
                    || _options.Redacts.Any(r => r.Equals(property.Name, _options.StringComparison)) // redacting by name
                    || _options.IfIsRedacts.Any(iir => iir.Redact.Equals(property.Name, _options.StringComparison) // redact if other property has a specific value
                                                    && properties.Any(p => iir.If.Equals(p.Name, _options.StringComparison)
                                                                        && p.Value.Type == JTokenType.String
                                                                        && iir.Is.Equals(p.Value.Value<string>(), _options.StringComparison)));

                RedactJToken(property.Value, redactProperty, () => property.Value = _options.Mask);
            }
        }

        private void RedactJToken(JToken jtoken, bool redacting, Action redactAction)
        {
            switch (jtoken.Type)
            {
                case JTokenType.Object:
                    if (redacting && _options.ComplexTypeHandling == ComplexTypeHandling.RedactValue)
                        goto default;

                    RedactJObject(jtoken as JObject, redacting);
                    break;

                case JTokenType.Array:
                    if (redacting && _options.ComplexTypeHandling == ComplexTypeHandling.RedactValue)
                        goto default;

                    var array = jtoken as JArray;
                    for (var i = 0; i < array.Count; i++)
                    {
                        var item = array[i];
                        var index = i;
                        RedactJToken(item, redacting, () => array[index] = _options.Mask);
                    }
                    break;

                default:
                    if (redacting)
                    {
                        redactAction();
                    }
                    break;
            }
        }
    }
}