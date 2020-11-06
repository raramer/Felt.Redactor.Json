namespace Felt.Redactor.Json
{
    public sealed class JsonRedactorOptions : RedactorOptions
    {
        public JsonRedactorFormatting Formatting { get; set; } = JsonRedactorFormatting.Compressed;
    }
}