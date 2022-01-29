namespace IZEncoder.Common.AvisynthFilter
{
    public interface IAvisynthParamValidator
    {
        string Validator { get; set; }
        string Validate(object input);
    }
}