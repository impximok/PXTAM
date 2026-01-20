using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SnomiAssignmentReal.Validation
{
    public class PasswordComplexityAttribute : ValidationAttribute, IClientModelValidator
    {
        private const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$";

        public override bool IsValid(object? value) =>
            value is string s && Regex.IsMatch(s, Pattern);

        public override string FormatErrorMessage(string name) =>
            $"{name} must be at least 8 characters long and include " +
            "uppercase, lowercase, a digit, and one of the following symbols: @ $ ! % * ? &.";

        // Client-side validation
        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(
                context.Attributes,
                "data-val-passwordcomplexity",
                FormatErrorMessage(context.ModelMetadata.GetDisplayName())
            );
            MergeAttribute(
                context.Attributes,
                "data-val-passwordcomplexity-pattern",
                Pattern
            );
        }

        private bool MergeAttribute(IDictionary<string, string> attrs, string key, string value)
        {
            if (attrs.ContainsKey(key)) return false;
            attrs.Add(key, value);
            return true;
        }
    }
}
