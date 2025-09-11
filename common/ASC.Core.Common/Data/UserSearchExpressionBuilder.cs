// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Linq.Expressions;
using System.Reflection;
using ASC.Core.Users;
using ASC.Core.Common.EF;
using ASC.Core.Data;

namespace ASC.Core.Data;

/// <summary>
/// Helper class for building dynamic LINQ expressions for advanced user search
/// </summary>
public static class UserSearchExpressionBuilder
{
    private static readonly Dictionary<string, PropertyInfo> UserProperties = typeof(User)
        .GetProperties()
        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> FieldMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "IsActive", "ActivationStatus" } // IsActive is computed from ActivationStatus
    };

    /// <summary>
    /// Builds a LINQ expression from UserSearchPayload
    /// </summary>
    public static Expression<Func<User, bool>> BuildExpression(UserSearchPayload searchPayload)
    {
        if (searchPayload?.RootGroup == null)
        {
            return u => true;
        }

        var parameter = Expression.Parameter(typeof(User), "u");
        var body = BuildLogicalGroupExpression(searchPayload.RootGroup, parameter);
        
        return Expression.Lambda<Func<User, bool>>(body, parameter);
    }

    private static Expression BuildLogicalGroupExpression(LogicalGroup group, ParameterExpression parameter)
    {
        var expressions = new List<Expression>();

        // Process conditions
        foreach (var condition in group.Conditions)
        {
            var conditionExpression = BuildConditionExpression(condition, parameter);
            if (conditionExpression != null)
            {
                expressions.Add(conditionExpression);
            }
        }

        // Process nested groups
        foreach (var nestedGroup in group.Groups)
        {
            var nestedExpression = BuildLogicalGroupExpression(nestedGroup, parameter);
            if (nestedExpression != null)
            {
                expressions.Add(nestedExpression);
            }
        }

        if (expressions.Count == 0)
        {
            return Expression.Constant(true);
        }

        if (expressions.Count == 1)
        {
            return expressions[0];
        }

        // Combine expressions with AND or OR
        var result = expressions[0];
        for (int i = 1; i < expressions.Count; i++)
        {
            if (string.Equals(group.Operator, "OR", StringComparison.OrdinalIgnoreCase))
            {
                result = Expression.OrElse(result, expressions[i]);
            }
            else // Default to AND
            {
                result = Expression.AndAlso(result, expressions[i]);
            }
        }

        return result;
    }

    private static Expression BuildConditionExpression(SearchCondition condition, ParameterExpression parameter)
    {
        if (string.IsNullOrEmpty(condition.Field) || condition.Value == null)
        {
            return null;
        }

        // Map field name if needed
        var fieldName = FieldMappings.TryGetValue(condition.Field, out var mappedField) ? mappedField : condition.Field;

        // Handle special cases
        if (string.Equals(condition.Field, "IsActive", StringComparison.OrdinalIgnoreCase))
        {
            return BuildIsActiveExpression(condition, parameter);
        }

        if (!UserProperties.TryGetValue(fieldName, out var property))
        {
            return null; // Unknown field
        }

        var propertyAccess = Expression.Property(parameter, property);
        var value = ConvertValue(condition.Value, property.PropertyType);

        if (value == null)
        {
            return null; // Invalid value conversion
        }

        return BuildComparisonExpression(propertyAccess, condition.Operator, value, property.PropertyType);
    }

    private static Expression BuildIsActiveExpression(SearchCondition condition, ParameterExpression parameter)
    {
        // IsActive is computed as ActivationStatus.HasFlag(EmployeeActivationStatus.Activated)
        var activationStatusProperty = Expression.Property(parameter, "ActivationStatus");
        var activatedFlag = Expression.Constant(EmployeeActivationStatus.Activated);
        var hasFlagMethod = typeof(Enum).GetMethod("HasFlag");
        var hasFlagCall = Expression.Call(activationStatusProperty, hasFlagMethod, activatedFlag);

        if (condition.Value is bool expectedActive)
        {
            return expectedActive ? hasFlagCall : Expression.Not(hasFlagCall);
        }

        return null;
    }

    private static Expression BuildComparisonExpression(Expression property, string operatorType, object value, Type propertyType)
    {
        var constantValue = Expression.Constant(value, propertyType);

        return operatorType?.ToUpperInvariant() switch
        {
            "EQUALS" => Expression.Equal(property, constantValue),
            "NOT_EQUALS" => Expression.NotEqual(property, constantValue),
            "GREATER_THAN" => Expression.GreaterThan(property, constantValue),
            "GREATER_THAN_OR_EQUAL" => Expression.GreaterThanOrEqual(property, constantValue),
            "LESS_THAN" => Expression.LessThan(property, constantValue),
            "LESS_THAN_OR_EQUAL" => Expression.LessThanOrEqual(property, constantValue),
            "STARTS_WITH" => BuildStringMethodExpression(property, "StartsWith", value.ToString()),
            "ENDS_WITH" => BuildStringMethodExpression(property, "EndsWith", value.ToString()),
            "CONTAINS" => BuildStringMethodExpression(property, "Contains", value.ToString()),
            "NOT_CONTAINS" => Expression.Not(BuildStringMethodExpression(property, "Contains", value.ToString())),
            _ => Expression.Equal(property, constantValue) // Default to equals
        };
    }

    private static Expression BuildStringMethodExpression(Expression property, string methodName, string value)
    {
        // Handle nullable strings
        var stringValue = Expression.Constant(value, typeof(string));
        var method = typeof(string).GetMethod(methodName, new[] { typeof(string) });
        
        // Check for null before calling string method
        var nullCheck = Expression.NotEqual(property, Expression.Constant(null, property.Type));
        var methodCall = Expression.Call(property, method, stringValue);
        
        return Expression.AndAlso(nullCheck, methodCall);
    }

    private static object ConvertValue(object value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Handle enums
            if (underlyingType.IsEnum)
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(underlyingType, stringValue, true);
                }
                return Enum.ToObject(underlyingType, value);
            }

            // Handle DateTime
            if (underlyingType == typeof(DateTime))
            {
                if (value is string dateString)
                {
                    return DateTime.Parse(dateString);
                }
            }

            // Handle Guid
            if (underlyingType == typeof(Guid))
            {
                if (value is string guidString)
                {
                    return Guid.Parse(guidString);
                }
            }

            // Handle boolean
            if (underlyingType == typeof(bool))
            {
                if (value is string boolString)
                {
                    return bool.Parse(boolString);
                }
            }

            // Use Convert.ChangeType for other types
            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            return null; // Invalid conversion
        }
    }
}
