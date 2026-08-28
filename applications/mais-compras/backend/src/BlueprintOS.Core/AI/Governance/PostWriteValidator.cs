#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Applies a <see cref="PostWriteValidationRule"/> by comparing what was expected after a write against what
/// was actually re-read from the resource. It compares only the rule's declared fields, matched by the rule's
/// declared business keys — so an unrelated column changed by a trigger does not fail the validation, while a
/// record that never appeared, or appeared with the wrong value, does.
/// </summary>
public static class PostWriteValidator
{
    public static PostWriteValidationReport Validate(
        PostWriteValidationRule rule,
        IReadOnlyList<RecoveryDataSet> expected,
        IReadOnlyList<RecoveryDataSet> actual,
        DateTimeOffset validatedAt)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var mismatches = new List<string>();
        var validated = 0;
        var withErrors = 0;

        foreach (var expectedSet in expected.Where(set => string.Equals(set.Resource, rule.Resource, StringComparison.OrdinalIgnoreCase)))
        {
            var actualRecords = actual
                .Where(set => string.Equals(set.Resource, expectedSet.Resource, StringComparison.OrdinalIgnoreCase))
                .SelectMany(set => set.Records)
                .ToList();

            foreach (var expectedRecord in expectedSet.Records)
            {
                validated++;
                var key = BuildKey(rule, expectedRecord);
                var actualRecord = actualRecords.FirstOrDefault(record => KeyMatches(rule, record, expectedRecord));
                if (actualRecord is null)
                {
                    withErrors++;
                    mismatches.Add($"{expectedSet.Resource}[{key}]: registro nao encontrado apos a escrita.");
                    continue;
                }

                var recordHasError = false;
                foreach (var field in rule.FieldsToCompare)
                {
                    if (!expectedRecord.TryGetValue(field, out var expectedValue)) continue;
                    actualRecord.TryGetValue(field, out var actualValue);
                    if (!string.Equals(Normalize(expectedValue), Normalize(actualValue), StringComparison.OrdinalIgnoreCase))
                    {
                        recordHasError = true;
                        mismatches.Add($"{expectedSet.Resource}[{key}].{field}: esperado '{expectedValue ?? "<null>"}', encontrado '{actualValue ?? "<null>"}'.");
                    }
                }

                if (recordHasError) withErrors++;
            }
        }

        // A validation that compared nothing has proven nothing, so it is not a pass.
        var passed = validated > 0 && withErrors == 0;
        if (validated == 0) mismatches.Add($"{rule.Resource}: nenhum registro esperado para comparar; a escrita nao pode ser considerada validada.");

        return new PostWriteValidationReport(rule.RuleId, passed, validated, withErrors, mismatches, validatedAt);
    }

    private static bool KeyMatches(PostWriteValidationRule rule, IReadOnlyDictionary<string, string?> candidate, IReadOnlyDictionary<string, string?> expected)
    {
        var comparableKeys = rule.BusinessKeyFields.Where(expected.ContainsKey).ToList();
        if (comparableKeys.Count == 0) return false;
        return comparableKeys.All(field =>
            candidate.TryGetValue(field, out var candidateValue)
            && string.Equals(Normalize(candidateValue), Normalize(expected[field]), StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildKey(PostWriteValidationRule rule, IReadOnlyDictionary<string, string?> record) =>
        string.Join(",", rule.BusinessKeyFields.Where(record.ContainsKey).Select(field => $"{field}={record[field]}"));

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
