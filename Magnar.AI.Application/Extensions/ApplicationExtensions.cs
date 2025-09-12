using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace Magnar.AI.Application.Extensions;

public static class ApplicationExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString());
        if (field is null)
        {
            return string.Empty;
        }

        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        return string.Empty;
    }

    public static ExcelPackage GenerateExcel<T>(this IEnumerable<T> data, IEnumerable<ExcelColumnDto> columns, string title)
        where T : class
    {
        ExcelPackage.LicenseContext = LicenseContext.Commercial;
        ExcelPackage package = new();
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(title);

        List<ExcelColumnDto> columnsList = columns.ToList();
        IList<T> dataList = data as IList<T> ?? data.ToList();

        // Cache property info for each column
        Dictionary<string, PropertyInfo> propertyCache = [];
        foreach (ExcelColumnDto? column in columnsList)
        {
            PropertyInfo? propertyInfo = typeof(T).GetProperty(column.Header);
            if (propertyInfo is not null)
            {
                propertyCache[column.Header] = propertyInfo;
            }
        }

        // Add headers with formatting
        for (int i = 0; i < columnsList.Count; i++)
        {
            ExcelRange cell = worksheet.Cells[1, i + 1];
            cell.Value = columnsList[i].DisplayName;
            cell.Style.Font.Bold = true; // Make headers bold
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray); // Header background color
            worksheet.Column(i + 1).Width = columnsList[i].Width; // Set column width
        }

        // Add data rows
        Parallel.For(0, dataList.Count, i =>
        {
            T row = dataList[i];
            for (int j = 0; j < columnsList.Count; j++)
            {
                string columnKey = columnsList[j].Header; // Use Header from ExcelColumnsDto
                if (propertyCache.TryGetValue(columnKey, out PropertyInfo? propertyInfo))
                {
                    string? value = propertyInfo.GetValue(row)?.ToString(); // Get property value
                    worksheet.Cells[i + 2, j + 1].Value = value ?? string.Empty;
                }
                else
                {
                    worksheet.Cells[i + 2, j + 1].Value = string.Empty; // Property not found
                }
            }
        });

        // Apply border to the entire table
        ExcelRange tableRange = worksheet.Cells[1, 1, dataList.Count + 1, columnsList.Count];
        tableRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
        tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

        return package;
    }

    /// <summary>
    /// Replace tokens in email body with values in dictionary.
    /// </summary>
    /// <param name="message">Message including tokens.</param>
    /// <param name="tokens">Dictionary of token keys with replacements.</param>
    /// <returns>Replaced tokens body string.</returns>
    public static string ReplaceEmailTokens(this string message, Dictionary<string, string> tokens)
    {
        if (tokens is null)
        {
            return message;
        }

        StringBuilder content = new(message);
        foreach (KeyValuePair<string, string> k in tokens)
        {
            content = content.Replace($"[[{k.Key}]]", k.Value);
        }

        return content.ToString();
    }

    public static Result ToApplicationResult(this IdentityResult result)
    {
        IEnumerable<Error> applicationErrors = result.Errors.Select(x =>
        {
            return new Error(x.Description);
        });

        return new Result(result.Succeeded, applicationErrors);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        if (expr1 is null)
        {
            return expr2;
        }

        if (expr2 is null)
        {
            return expr1;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T));

        Expression<Func<T, bool>> combined = Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter)),
            parameter);

        return combined;
    }
}
