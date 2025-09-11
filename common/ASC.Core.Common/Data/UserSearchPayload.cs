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

using System.Text.Json.Serialization;

namespace ASC.Core.Data;

/// <summary>
/// Represents a single search condition for user filtering
/// </summary>
public class SearchCondition
{
    /// <summary>
    /// The field name to search on (e.g., "FirstName", "Email", "Status")
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// The comparison operator (e.g., "EQUALS", "STARTS_WITH", "GREATER_THAN")
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// The value to compare against
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

/// <summary>
/// Represents a logical group of search conditions with AND/OR operations
/// </summary>
public class LogicalGroup
{
    /// <summary>
    /// The logical operator for combining conditions and groups ("AND" or "OR")
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "AND";

    /// <summary>
    /// List of search conditions in this group
    /// </summary>
    [JsonPropertyName("conditions")]
    public List<SearchCondition> Conditions { get; set; } = new();

    /// <summary>
    /// List of nested logical groups
    /// </summary>
    [JsonPropertyName("groups")]
    public List<LogicalGroup> Groups { get; set; } = new();
}

/// <summary>
/// Root payload for advanced user search requests
/// </summary>
public class UserSearchPayload
{
    /// <summary>
    /// The root logical group containing all search criteria
    /// </summary>
    [JsonPropertyName("rootGroup")]
    public LogicalGroup RootGroup { get; set; } = new();
}
