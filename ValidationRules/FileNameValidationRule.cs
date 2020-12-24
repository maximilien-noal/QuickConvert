namespace QuickConvert.ValidationRules
{
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;

    public class FileNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string filename)
            {
                if (string.IsNullOrEmpty(filename))
                {
                    return new ValidationResult(false, "Value cannot be converted to string.");
                }
                if (Path.GetInvalidFileNameChars().Any(x => filename.Contains(x)))
                {
                    return new ValidationResult(false, $"Caractères invalides: {Path.GetInvalidPathChars().Where(x => filename.Contains(x)).Select(x => $" {x}")}");
                }
            }
            else
            {
                return new ValidationResult(true, "Value cannot be converted to string."); ;
            }
            return new ValidationResult(true, "Filename is valid."); ;
        }
    }
}