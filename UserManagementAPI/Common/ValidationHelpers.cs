
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public static class ValidationHelpers
{
    public static (bool IsValid, Dictionary<string, string[]> Errors) Validate<T>(T model)
    {
        var context = new ValidationContext(model!);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model!, context, results, validateAllProperties: true);
        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => new { member, error = r.ErrorMessage ?? "Invalid value." }))
            .GroupBy(x => x.member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.error).ToArray());
        return (isValid, errors);
    }
}

